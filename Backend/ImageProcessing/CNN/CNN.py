import sys
import cv2 as cv
import numpy as np
from scipy.interpolate import interp1d
import json
import os
from PIL import Image
import matplotlib.pyplot as plt
from tensorflow.keras.models import load_model
from scipy import ndimage
from skimage import morphology, measure
from skimage.morphology import skeletonize, dilation, erosion

NUM_EDGE_POINTS = 4550  # Fixed number of points for each boundary

class CNNTrackProcessor:
    def __init__(self, model_path="MapSegmentationGenerator.keras"):
        self.model = load_model(model_path, compile=True)
        self.original_image = None
        self.track_mask = None
        self.track_boundaries = None

    def resampleContour(self, contour, target_points=NUM_EDGE_POINTS):
        """Resample contour to fixed number of points - same as original"""
        if len(contour) < 2:
            return []

        contour = contour.squeeze()
        if contour.ndim != 2:
            contour = contour.reshape(-1, 2)

        x = contour[:, 0]
        y = contour[:, 1]
        dist = np.sqrt(np.diff(x) ** 2 + np.diff(y) ** 2)
        dist = np.concatenate([[0], np.cumsum(dist)])

        if dist[-1] == 0:
            return [[float(x[i]), float(y[i])] for i in range(len(x))]

        interp_dist = np.linspace(0, dist[-1], target_points)
        interp_func_x = interp1d(dist, x, kind="linear")
        interp_func_y = interp1d(dist, y, kind="linear")
        x_interp = interp_func_x(interp_dist)
        y_interp = interp_func_y(interp_dist)

        return [[float(x), float(y)] for x, y in zip(x_interp, y_interp)]

    def should_ignore_tile(self, tile_array, black_threshold=1.0, black_color=(0, 0, 0)):
        """Check if a tile should be ignored based on black color percentage."""
        if len(tile_array.shape) != 3 or tile_array.shape[2] != 3:
            return False
        
        black_mask = np.all(tile_array == black_color, axis=-1)
        black_percentage = np.sum(black_mask) / (tile_array.shape[0] * tile_array.shape[1])
        
        return black_percentage >= black_threshold

    def tile_image_with_offset(self, image, tile_size=(128, 128), offset=(0, 0), black_threshold=0.85):
        """Splits an image into tiles with a given offset, filtering out predominantly black tiles."""
        if isinstance(image, np.ndarray):
            image = Image.fromarray(image)

        width, height = image.size
        tiles = []
        tile_coordinates = []
        tile_ignore_mask = []

        for y in range(offset[1], height, tile_size[1]):
            for x in range(offset[0], width, tile_size[0]):
                x_end = min(x + tile_size[0], width)
                y_end = min(y + tile_size[1], height)
                
                if (x_end - x) < tile_size[0] // 2 or (y_end - y) < tile_size[1] // 2:
                    continue
                
                tile = image.crop((x, y, x_end, y_end))

                if tile.size != tile_size:
                    new_tile = Image.new("RGB", tile_size, (0, 0, 0))
                    new_tile.paste(tile, (0, 0))
                    tile = new_tile

                tile_array = np.array(tile)
                ignore_tile = self.should_ignore_tile(tile_array, black_threshold)
                
                if not ignore_tile:
                    tiles.append(tile_array)
                    tile_coordinates.append((x, y))
                else:
                    tile_coordinates.append((x, y))
                
                tile_ignore_mask.append(ignore_tile)
        
        return tiles, tile_coordinates, tile_ignore_mask

    def preprocess_tile(self, tile):
        """Preprocess tile to match training data format"""
        tile = tile.astype(np.float32) / 255.0
        return tile

    def predict_on_tiles(self, tiles, ignore_mask):
        """Run prediction on tiles, handling ignored tiles"""
        predictions = []
        tile_index = 0

        for i, ignore in enumerate(ignore_mask):
            if ignore:
                blank_prediction = np.zeros((128, 128, 1), dtype=np.float32)
                predictions.append(blank_prediction)
            else:
                processed_tile = self.preprocess_tile(tiles[tile_index])
                prediction = self.model.predict(np.expand_dims(processed_tile, axis=0), verbose=0)
                predictions.append(prediction[0])
                tile_index += 1
        
        return predictions

    def stitch_tiles_with_weights(self, tile_predictions, original_size, tile_coordinates, tile_size=(128, 128)):
        """Stitches tiles back with proper weight accumulation for overlapping areas."""
        width, height = original_size
        stitched = np.zeros((height, width), dtype=np.float32)
        weight_map = np.zeros((height, width), dtype=np.float32)

        for (x, y), prediction in zip(tile_coordinates, tile_predictions):
            if prediction.ndim == 3:
                mask = prediction[:, :, 0]
            else:
                mask = prediction
            
            h = min(tile_size[1], height - y)
            w = min(tile_size[0], width - x)
            
            stitched[y:y+h, x:x+w] += mask[:h, :w]
            weight_map[y:y+h, x:x+w] += 1.0

        weight_map[weight_map == 0] = 1.0
        stitched = stitched / weight_map
        
        return stitched

    def generate_track_mask(self, image_path, tile_size=(128, 128)):
        """Generate track mask using CNN with overlapping tiles and gap filling"""
        # Load original image
        original_image = Image.open(image_path).convert("RGB")
        original_size = original_image.size
        self.original_image = np.array(original_image)
        
        # Multi-pass prediction for better coverage
        # Pass 1: Regular grid
        tiles1, coords1, ignore1 = self.tile_image_with_offset(original_image, tile_size, offset=(0, 0))
        predictions1 = self.predict_on_tiles(tiles1, ignore1)
        mask1 = self.stitch_tiles_with_weights(predictions1, original_size, coords1, tile_size)
        
        # Pass 2: Offset grid
        offset = (tile_size[0]//2, tile_size[1]//2)
        tiles2, coords2, ignore2 = self.tile_image_with_offset(original_image, tile_size, offset=offset)
        predictions2 = self.predict_on_tiles(tiles2, ignore2)
        mask2 = self.stitch_tiles_with_weights(predictions2, original_size, coords2, tile_size)
        
        # Merge passes
        weight1 = (mask1 > 0).astype(np.float32)
        weight2 = (mask2 > 0).astype(np.float32)
        total_weight = weight1 + weight2
        total_weight[total_weight == 0] = 1
        merged_mask = (mask1 * weight1 + mask2 * weight2) / total_weight
        
        # Convert to binary mask
        binary_mask = (merged_mask > 0.3).astype(np.uint8)
        
        # Clean up the mask
        kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (3, 3))
        binary_mask = cv.morphologyEx(binary_mask, cv.MORPH_CLOSE, kernel, iterations=2)
        binary_mask = cv.morphologyEx(binary_mask, cv.MORPH_OPEN, kernel, iterations=1)
        
        self.track_mask = binary_mask * 255  # Convert to 0-255 range
        return self.track_mask
      
      
      
    def detectBoundaries(self, mask):
      """Detect track boundaries from the CNN-generated mask, fixing gaps/forks but keeping inner loop."""

     # Close gaps + remove forks
      kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (15, 15))
      closed = cv.morphologyEx(mask, cv.MORPH_CLOSE, kernel, iterations=0)

      # Find contours with hierarchy (needed to detect holes)
      contours, hierarchy = cv.findContours(closed, cv.RETR_CCOMP, cv.CHAIN_APPROX_SIMPLE)
      if not contours or hierarchy is None:
          return None

      # Pick the largest contour as outer
      areas = [cv.contourArea(c) for c in contours]
      largest_idx = int(np.argmax(areas))
      outer_boundary = contours[largest_idx]

      # Look for children (holes) of that contour
      inner_boundary = None
      for i, h in enumerate(hierarchy[0]):
          parent = h[3]  # parent index (-1 = no parent)
          if parent == largest_idx:
              # child of the outer contour → candidate inner boundary
              if inner_boundary is None or cv.contourArea(contours[i]) > cv.contourArea(inner_boundary):
                  inner_boundary = contours[i]

      # Fallback: if no inner boundary found, approximate with erosion
      if inner_boundary is None:
          kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (20, 20))
          eroded = cv.erode(closed, kernel, iterations=3)
          inner_contours, _ = cv.findContours(eroded, cv.RETR_EXTERNAL, cv.CHAIN_APPROX_SIMPLE)
          if inner_contours:
              inner_boundary = max(inner_contours, key=cv.contourArea)

      self.track_boundaries = {"outer": outer_boundary, "inner": inner_boundary}
      return self.track_boundaries

    def processImageForCSharp(self, img_path):
        """Main processing function that matches the original interface"""
        try:
            # Generate track mask using CNN
            mask = self.generate_track_mask(img_path)
            if mask is None:
                return {
                    "success": False,
                    "outer_boundary": [],
                    "inner_boundary": [],
                    "error": "Failed to generate track mask"
                }

            # Detect boundaries from the mask
            boundaries = self.detectBoundaries(mask)
            if boundaries is None:
                return {
                    "success": False,
                    "outer_boundary": [],
                    "inner_boundary": [],
                    "error": "Failed to detect track boundaries"
                }

            # Resample contours to fixed point count
            outer_coords = []
            inner_coords = []

            if boundaries["outer"] is not None:
                outer_coords = self.resampleContour(boundaries["outer"], NUM_EDGE_POINTS)

            if boundaries["inner"] is not None:
                inner_coords = self.resampleContour(boundaries["inner"], NUM_EDGE_POINTS)

            return {
                "success": True,
                "outer_boundary": outer_coords,
                "inner_boundary": inner_coords,
                "error": None
            }

        except Exception as e:
            return {
                "success": False,
                "outer_boundary": [],
                "inner_boundary": [],
                "error": str(e)
            }

def write_result_to_file(result, output_file):
    """Write result to file instead of printing to stdout"""
    try:
        with open(output_file, 'w') as f:
            json.dump(result, f, separators=(',', ':'))
        return True
    except Exception as e:
        print(f"Error writing to file: {e}", file=sys.stderr)
        return False

def main():
    if len(sys.argv) < 2:
        result = {
            "success": False,
            "outer_boundary": [],
            "inner_boundary": [],
            "error": "No image path provided"
        }
        if len(sys.argv) >= 3:
            write_result_to_file(result, sys.argv[2])
        else:
            print(json.dumps(result, separators=(',', ':')))
        sys.exit(1)

    img_path = sys.argv[1]
    output_file = sys.argv[2] if len(sys.argv) >= 3 else None
    
    # Use CNN processor instead of traditional processor
    processor = CNNTrackProcessor()
    result = processor.processImageForCSharp(img_path)
    
    if output_file:
        # Write to file
        if write_result_to_file(result, output_file):
            sys.exit(0 if result["success"] else 1)
        else:
            sys.exit(2)  # File write error
    else:
        # Fallback to stdout (for backwards compatibility)
        print(json.dumps(result, separators=(',', ':')))
        sys.exit(0 if result["success"] else 1)

if __name__ == "__main__":
    main()    