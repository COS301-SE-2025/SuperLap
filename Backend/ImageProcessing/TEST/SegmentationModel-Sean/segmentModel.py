import os
import numpy as np
import cv2
import matplotlib.pyplot as plt
import tensorflow as tf
from PIL import Image
from scipy import ndimage
from skimage.morphology import skeletonize, remove_small_objects
from skimage.measure import label, regionprops
import json

class SlidingWindowTrackDetector:
    """
    Detect race tracks by processing small 128x128 patches and assembling results
    """
    
    def __init__(self, lane_model_path, patch_size=128, stride=64):
        self.patch_size = patch_size
        self.stride = stride
        self.model = None
        self.load_lane_model(lane_model_path)
        
    def load_lane_model(self, model_path):
        """Load the pre-trained lane detection model"""
        try:
            self.model = tf.keras.models.load_model(model_path, compile=False)
            print(f"Loaded lane detection model from {model_path}")
            print(f"Model input shape: {self.model.input_shape}")
            print(f"Model output shape: {self.model.output_shape}")
        except Exception as e:
            print(f"Error loading model: {e}")
            
    def preprocess_patch(self, patch):
        """Preprocess a single patch for the lane model"""
        # Ensure patch is correct size
        if patch.shape[:2] != (self.patch_size, self.patch_size):
            patch = cv2.resize(patch, (self.patch_size, self.patch_size))
        
        # Convert to RGB if needed
        if len(patch.shape) == 3 and patch.shape[2] == 3:
            patch = cv2.cvtColor(patch, cv2.COLOR_BGR2RGB)
        
        # Normalize
        patch = patch.astype(np.float32) / 255.0
        
        # Apply histogram equalization like in original model
        if len(patch.shape) == 3:
            patch_yuv = cv2.cvtColor((patch * 255).astype(np.uint8), cv2.COLOR_RGB2YUV)
            patch_yuv[:,:,0] = cv2.equalizeHist(patch_yuv[:,:,0])
            patch = cv2.cvtColor(patch_yuv, cv2.COLOR_YUV2RGB).astype(np.float32) / 255.0
        
        return patch
    
    def extract_patches(self, image):
        """
        Extract overlapping patches from large image
        Returns patches and their positions
        """
        h, w = image.shape[:2]
        patches = []
        positions = []
        
        y = 0
        while y + self.patch_size <= h:
            x = 0
            while x + self.patch_size <= w:
                # Extract patch
                patch = image[y:y+self.patch_size, x:x+self.patch_size]
                
                # Preprocess patch
                processed_patch = self.preprocess_patch(patch)
                
                patches.append(processed_patch)
                positions.append((x, y))
                
                x += self.stride
                
            y += self.stride
            
        # Handle remaining areas if image doesn't divide evenly
        # Right edge
        if w % self.stride != 0:
            x = w - self.patch_size
            y = 0
            while y + self.patch_size <= h:
                patch = image[y:y+self.patch_size, x:x+self.patch_size]
                processed_patch = self.preprocess_patch(patch)
                patches.append(processed_patch)
                positions.append((x, y))
                y += self.stride
        
        # Bottom edge
        if h % self.stride != 0:
            y = h - self.patch_size
            x = 0
            while x + self.patch_size <= w:
                patch = image[y:y+self.patch_size, x:x+self.patch_size]
                processed_patch = self.preprocess_patch(patch)
                patches.append(processed_patch)
                positions.append((x, y))
                x += self.stride
        
        # Bottom-right corner
        if h % self.stride != 0 and w % self.stride != 0:
            x = w - self.patch_size
            y = h - self.patch_size
            patch = image[y:y+self.patch_size, x:x+self.patch_size]
            processed_patch = self.preprocess_patch(patch)
            patches.append(processed_patch)
            positions.append((x, y))
        
        return np.array(patches), positions
    
    def predict_patches(self, patches, batch_size=16, confidence_threshold=0.3):
        if self.model is None:
            raise ValueError("Model not loaded!")
        
        predictions = []
        
        for i in range(0, len(patches), batch_size):
            batch = patches[i:i+batch_size]
            batch_predictions = self.model.predict(batch, verbose=0)
            
            # Ensure shape is (h, w) per patch
            if batch_predictions.shape[-1] == 1:
                batch_predictions = np.squeeze(batch_predictions, axis=-1)
            
            batch_predictions = (batch_predictions > confidence_threshold).astype(np.float32)
            predictions.extend(batch_predictions)
        
        return np.array(predictions)

    
    def assemble_predictions(self, predictions, positions, original_shape, blend_mode='max'):
        """
        Assemble patch predictions into full-size prediction map
        """
        h, w = original_shape[:2]
        
        if len(predictions[0].shape) == 3:
            assembled = np.zeros((h, w, predictions[0].shape[2]), dtype=np.float32)
            weight_map = np.zeros((h, w, predictions[0].shape[2]), dtype=np.float32)
        else:
            assembled = np.zeros((h, w), dtype=np.float32)
            weight_map = np.zeros((h, w), dtype=np.float32)
        
        # Create distance-based weights for blending overlaps
        patch_weights = self.create_patch_weights(self.patch_size)
        
        for pred, (x, y) in zip(predictions, positions):
            # if len(pred.shape) == 3:
            #     pred = pred[:, :, 0]  # Take first channel
            
            if blend_mode == 'weighted':
                # Weighted blending based on distance from patch center
                assembled[y:y+self.patch_size, x:x+self.patch_size] += pred * patch_weights
                weight_map[y:y+self.patch_size, x:x+self.patch_size] += patch_weights
            elif blend_mode == 'max':
                # Take maximum value (good for binary predictions)
                current = assembled[y:y+self.patch_size, x:x+self.patch_size]
                assembled[y:y+self.patch_size, x:x+self.patch_size] = np.maximum(current, pred)
                weight_map[y:y+self.patch_size, x:x+self.patch_size] = 1
            else:  # 'average'
                assembled[y:y+self.patch_size, x:x+self.patch_size] += pred
                weight_map[y:y+self.patch_size, x:x+self.patch_size] += 1
        
        # Normalize by weight map (avoid division by zero)
        if blend_mode != 'max':
            weight_map = np.maximum(weight_map, 1e-7)
            assembled = assembled / weight_map
        
        return assembled
    
    def create_patch_weights(self, patch_size):
        """
        Create distance-based weights for patch blending
        Center pixels get higher weights
        """
        center = patch_size // 2
        y, x = np.ogrid[:patch_size, :patch_size]
        
        # Distance from center
        dist_from_center = np.sqrt((x - center)**2 + (y - center)**2)
        max_dist = np.sqrt(2) * center
        
        # Weight decreases with distance from center
        weights = 1.0 - (dist_from_center / max_dist)
        weights = np.maximum(weights, 0.1)  # Minimum weight
        
        return weights
    
    def post_process_track_map(self, track_map, min_area=500, fill_holes=True):
        """
        Clean up the assembled track prediction
        """
        # Binarize
        binary_map = (track_map > 0.5).astype(np.uint8)
        
        if fill_holes:
            # Fill holes in track segments
            binary_map = ndimage.binary_fill_holes(binary_map).astype(np.uint8)
        
        # Remove small components
        if min_area > 0:
            labeled_map = label(binary_map)
            binary_map = remove_small_objects(labeled_map, min_size=min_area).astype(np.uint8)
            binary_map = (binary_map > 0).astype(np.uint8)
        
        # Morphological operations to connect nearby segments
        kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
        binary_map = cv2.morphologyEx(binary_map, cv2.MORPH_CLOSE, kernel)
        
        # Smooth edges
        kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
        binary_map = cv2.morphologyEx(binary_map, cv2.MORPH_OPEN, kernel)
        
        return binary_map
    
    def extract_track_skeleton(self, track_map):
        """
        Extract track centerline from cleaned track map
        """
        # Skeletonize the track
        skeleton = skeletonize(track_map > 0)
        
        # Clean skeleton
        skeleton_clean = self.clean_skeleton(skeleton)
        
        return skeleton_clean.astype(np.uint8)
    
    def clean_skeleton(self, skeleton, min_branch_length=15):
        """
        Remove small branches from skeleton
        """
        skeleton = skeleton.astype(np.uint8)
        
        # Iteratively remove short branches
        cleaned = skeleton.copy()
        changed = True
        iterations = 0
        max_iterations = 10
        
        while changed and iterations < max_iterations:
            changed = False
            iterations += 1
            
            # Find endpoints (pixels with exactly 1 neighbor)
            kernel = np.ones((3, 3), dtype=np.uint8)
            kernel[1, 1] = 0
            neighbors = cv2.filter2D(cleaned, -1, kernel)
            
            endpoints = (cleaned == 1) & (neighbors == 1)
            endpoint_coords = np.where(endpoints)
            
            for y, x in zip(endpoint_coords[0], endpoint_coords[1]):
                # Trace branch length
                branch_length = self.trace_branch_from_endpoint(cleaned, x, y, min_branch_length)
                
                if branch_length < min_branch_length:
                    # Remove this short branch
                    self.remove_branch_from_endpoint(cleaned, x, y, branch_length)
                    changed = True
        
        return cleaned
    
    def trace_branch_from_endpoint(self, skeleton, start_x, start_y, max_length):
        """
        Trace branch length from an endpoint
        """
        visited = set()
        current = (start_x, start_y)
        length = 0
        
        while length < max_length:
            if current in visited:
                break
            
            visited.add(current)
            x, y = current
            
            if skeleton[y, x] == 0:
                break
            
            # Find neighboring skeleton pixels
            neighbors = []
            for dx in [-1, 0, 1]:
                for dy in [-1, 0, 1]:
                    if dx == 0 and dy == 0:
                        continue
                    
                    nx, ny = x + dx, y + dy
                    if (0 <= nx < skeleton.shape[1] and 0 <= ny < skeleton.shape[0] and
                        skeleton[ny, nx] == 1 and (nx, ny) not in visited):
                        neighbors.append((nx, ny))
            
            if len(neighbors) != 1:
                # Junction or dead end
                break
            
            current = neighbors[0]
            length += 1
        
        return length
    
    def remove_branch_from_endpoint(self, skeleton, start_x, start_y, length):
        """
        Remove a branch of specified length from endpoint
        """
        current = (start_x, start_y)
        removed = 0
        
        while removed <= length:
            x, y = current
            skeleton[y, x] = 0
            removed += 1
            
            # Find next pixel in branch
            neighbors = []
            for dx in [-1, 0, 1]:
                for dy in [-1, 0, 1]:
                    if dx == 0 and dy == 0:
                        continue
                    
                    nx, ny = x + dx, y + dy
                    if (0 <= nx < skeleton.shape[1] and 0 <= ny < skeleton.shape[0] and
                        skeleton[ny, nx] == 1):
                        neighbors.append((nx, ny))
            
            if len(neighbors) != 1:
                break
            
            current = neighbors[0]
    
    def detect_full_track(self, image_path, output_dir="track_detection_output", visualize=True):
        """
        Main function to detect complete track from large image
        """
        if not os.path.exists(output_dir):
            os.makedirs(output_dir)
        
        # Load image
        print(f"Loading image: {image_path}")
        image = cv2.imread(image_path)
        if image is None:
            raise ValueError(f"Could not load image: {image_path}")
        
        original_shape = image.shape
        print(f"Image shape: {original_shape}")
        
        # Extract patches
        print("Extracting patches...")
        patches, positions = self.extract_patches(image)
        print(f"Extracted {len(patches)} patches")
        
        # Predict on patches
        print("Running lane detection on patches...")
        predictions = self.predict_patches(patches, batch_size=16)
        
        # Assemble predictions
        print("Assembling predictions...")
        assembled_map = self.assemble_predictions(predictions, positions, original_shape, blend_mode='max')
        
        # Post-process
        print("Post-processing track map...")
        cleaned_map = self.post_process_track_map(assembled_map, min_area=1000)
        
        # Extract skeleton
        print("Extracting track centerline...")
        skeleton = self.extract_track_skeleton(cleaned_map)
        
        # Save results
        results = {
            'raw_prediction': assembled_map,
            'cleaned_track': cleaned_map,
            'skeleton': skeleton,
            'image_shape': original_shape,
            'patch_info': {
                'patch_size': self.patch_size,
                'stride': self.stride,
                'num_patches': len(patches)
            }
        }
        
        # Save images
        cv2.imwrite(os.path.join(output_dir, 'raw_prediction.png'), (assembled_map * 255).astype(np.uint8))
        cv2.imwrite(os.path.join(output_dir, 'cleaned_track.png'), (cleaned_map * 255).astype(np.uint8))
        cv2.imwrite(os.path.join(output_dir, 'track_skeleton.png'), (skeleton * 255).astype(np.uint8))
        
        if visualize:
            self.visualize_results(image, assembled_map, cleaned_map, skeleton, output_dir)
        
        print(f"Track detection completed! Results saved to: {output_dir}")
        return results
    
    def visualize_results(self, original_image, raw_prediction, cleaned_track, skeleton, output_dir):
        """
        Create comprehensive visualization of results
        """
        fig, axes = plt.subplots(2, 3, figsize=(20, 12))
        
        # Original image
        axes[0, 0].imshow(cv2.cvtColor(original_image, cv2.COLOR_BGR2RGB))
        axes[0, 0].set_title('Original Image', fontsize=14)
        axes[0, 0].axis('off')
        
        # Raw prediction
        axes[0, 1].imshow(raw_prediction, cmap='hot')
        axes[0, 1].set_title('Raw Prediction (Assembled)', fontsize=14)
        axes[0, 1].axis('off')
        
        # Cleaned track
        axes[0, 2].imshow(cleaned_track, cmap='gray')
        axes[0, 2].set_title('Cleaned Track Map', fontsize=14)
        axes[0, 2].axis('off')
        
        # Track skeleton
        axes[1, 0].imshow(skeleton, cmap='hot')
        axes[1, 0].set_title('Track Centerline', fontsize=14)
        axes[1, 0].axis('off')
        
        # Overlay on original
        overlay = cv2.cvtColor(original_image, cv2.COLOR_BGR2RGB).copy()
        overlay_alpha = 0.6
        
        # Add track overlay
        track_color = np.zeros_like(overlay)
        track_color[cleaned_track > 0] = [255, 0, 0]  # Red for track
        overlay = cv2.addWeighted(overlay, 1-overlay_alpha, track_color, overlay_alpha, 0)
        
        axes[1, 1].imshow(overlay)
        axes[1, 1].set_title('Track Overlay on Original', fontsize=14)
        axes[1, 1].axis('off')
        
        # Skeleton overlay
        skeleton_overlay = cv2.cvtColor(original_image, cv2.COLOR_BGR2RGB).copy()
        skeleton_color = np.zeros_like(skeleton_overlay)
        skeleton_color[skeleton > 0] = [0, 255, 0]  # Green for centerline
        skeleton_overlay = cv2.addWeighted(skeleton_overlay, 0.7, skeleton_color, 0.3, 0)
        
        axes[1, 2].imshow(skeleton_overlay)
        axes[1, 2].set_title('Centerline Overlay', fontsize=14)
        axes[1, 2].axis('off')
        
        plt.tight_layout()
        plt.savefig(os.path.join(output_dir, 'complete_visualization.png'), dpi=200, bbox_inches='tight')
        plt.show()
        
        print(f"Visualization saved to: {os.path.join(output_dir, 'complete_visualization.png')}")

def main():
    """
    Example usage of sliding window track detector
    """
    # Path to your trained lane detection model
    lane_model_path = "LaneDetectionModel.keras"
    
    if not os.path.exists(lane_model_path):
        print(f"Lane detection model not found: {lane_model_path}")
        print("Please train the lane detection model first.")
        return
    
    # Initialize detector
    print("Initializing Sliding Window Track Detector...")
    detector = SlidingWindowTrackDetector(
        lane_model_path=lane_model_path,
        patch_size=128,  # Same size as your trained model
        stride=64        # 50% overlap between patches
    )
    
    # Test image path
    test_image_path = "test_track_image.jpg"  # Replace with your track image
    
    if os.path.exists(test_image_path):
        print(f"Processing track image: {test_image_path}")
        
        # Detect track
        results = detector.detect_full_track(
            image_path=test_image_path,
            output_dir="sliding_window_output",
            visualize=True
        )
        
        # Print statistics
        cleaned_track = results['cleaned_track']
        skeleton = results['skeleton']
        
        track_pixels = np.sum(cleaned_track > 0)
        skeleton_pixels = np.sum(skeleton > 0)
        total_pixels = cleaned_track.shape[0] * cleaned_track.shape[1]
        
        print(f"\nTrack Detection Statistics:")
        print(f"- Track pixels detected: {track_pixels:,}")
        print(f"- Centerline pixels: {skeleton_pixels:,}")
        print(f"- Track coverage: {track_pixels/total_pixels*100:.2f}%")
        print(f"- Number of patches processed: {results['patch_info']['num_patches']}")
        
    else:
        print(f"Test image not found: {test_image_path}")
        print("Please provide a race track image to test the detector.")

if __name__ == "__main__":
    main()