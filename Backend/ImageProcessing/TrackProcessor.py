from pathlib import Path
import cv2 as cv
import numpy as np
import matplotlib.pyplot as plt
import json
import os
import argparse
import glob
import struct
from scipy import ndimage
from scipy.interpolate import interp1d, splprep, splev
from skimage.morphology import skeletonize

class TrackProcessor:
    def __init__(self):
        #initialize track values
        self.original_image = None
        self.processed_image = None
        self.track_mask = None
        self.track_boundaries = None
        self.centerline = None
        self.centerline_smoothed = None

    def loadImg(self, img_path):
        # Load and convert the image
        self.original_image = cv.imread(img_path)
        if self.original_image is None:
            raise ValueError(f"Could not load image from {img_path}")
        print(f"Image loaded successfully.")
        return self.original_image
    
    def processImg(self, img, show_debug=True):
        # Process the track for easier edge detection
        hsv = cv.cvtColor(img, cv.COLOR_BGR2HSV)

        # Find black track
        lower_black = np.array([0,0,0])
        upper_black = np.array([180,255,150])
        dark_mask = cv.inRange(hsv, lower_black, upper_black)

        # bilateral filter to reduce noise and preserve edges
        bi_lat_filter = cv.bilateralFilter(dark_mask, 10, 130, 75)

        # Matrix
        kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (2,2))

        # Cleaning image using morphological operations which removes small noise and fills small holes
        opening = cv.morphologyEx(bi_lat_filter, cv.MORPH_OPEN, kernel, iterations=1)
        closing = cv.morphologyEx(opening, cv.MORPH_CLOSE, kernel, iterations=1)

        # Otsu's threshold selection to better define track
        _, thresh = cv.threshold(closing, 0, 255, cv.THRESH_BINARY + cv.THRESH_OTSU)

        if show_debug:
            #cv.imshow("dark_mask", dark_mask)
            #cv.imshow("Filtered", bi_lat_filter)
            #cv.imshow("cleaned", closing)
            cv.imshow("Otsu", thresh)

        self.processed_image = thresh
        self.track_mask = opening
        return {
            'processed_image': thresh,
            #'track_mask': dark_mask,
            #'greyscale': greyscale,
            #'filtered': bi_lat_filter,
            #'closing': closing,
            #'opening': opening
        }

    def detectBoundaries(self, img, show_debug=True):
        # Find track boundaries using color and edge detection
        cannyEdges = cv.Canny(img, 85, 170)

        contours, _ = cv.findContours(cannyEdges, cv.RETR_TREE, cv.CHAIN_APPROX_SIMPLE)
        
        if not contours or len(contours) < 3:
            print("Not enough contours found to detect boundaries")
            return None
        
        contours = sorted(contours, key=cv.contourArea, reverse=True)
        outer_boundary = contours[0]
        inner_boundary = contours[2]

        self.track_boundaries = {
            'outer': outer_boundary,
            'inner': inner_boundary
        }
        
        if show_debug:

            print("Number of contours found: " + str(len(contours)))
            print("Number of datapoints for outer boundary: ", str(len(outer_boundary)))
            print("Number of datapoints for inner boundary: ", str(len(inner_boundary)))

            contour_img = self.original_image.copy()
            cv.drawContours(contour_img, [outer_boundary], -1, (255,0,0), thickness=2)
            cv.drawContours(contour_img, [inner_boundary], -1, (0,0,255), thickness=2)
            #cv.drawContours(contour_img, outer_contours, -1, (0,255,0), thickness=1)
            #cv.drawContours(contour_img, outer_contours, -1, (0,255,255), thickness=1)
            cv.imshow("Contours", contour_img)

        return self.track_boundaries
    
    def extractCenterline(self, method='skeleton', smooth=True, show_debug=True):
        if self.track_mask is None:
            print("Track mask not found")
            return None
        
        track_bin = (self.track_mask > 0).astype(np.uint8)

        #if method == 'skeleton':
        skeleton = skeletonize(track_bin).astype(np.uint8) * 255
        centerline_points = self.skeletonToPoints(skeleton)

        #elif method == 'distance_transform':
            #dist_transform = cv.distanceTransform(track_bin, cv.DIST_L2, 5)


        #elif method == 'medial_axis':
            #dist_transform = cv.distanceTransform(track_bin, cv.DIST_L2, 5)

        #else:
            #print(f"{method} not recognised, defaulting to geometric")


        if not centerline_points:
            print(f"No centerline points found with method: {method}")
            return None
        
        ordered_points = self.orderPoints(centerline_points)
        smoothed_points = self.smoothCenterline(ordered_points) if smooth and len(ordered_points) > 10 else ordered_points

        self.centerline = ordered_points
        self.centerline_smoothed = smoothed_points

        if show_debug:
            cv.imshow("skeleton", skeleton)

        return {
            'centerline_raw': ordered_points,
            'centerline_smoothed': smoothed_points,
            'skeleton_image': skeleton if method == 'skeleton' else None
        }
    
    def skeletonToPoints(self, skeleton):
        points = []
        y_coords, x_coords = np.where(skeleton > 0)
        for x, y in zip(x_coords, y_coords):
            points.append((int(x), int(y)))
        return points
    
    def orderPoints(self, points):
        if len(points) < 2:
            return points
        
        points_arr = np.array(points)
        ordered = [points_arr[0]]
        remaining = points_arr[1:].tolist()
        current = np.array(points_arr[0])

        while len(remaining) > 0:
            distances = [np.linalg.norm(current - np.array(p)) for p in remaining]
            nearest = np.argmin(distances)
            nearest_pnt = remaining.pop(nearest)
            ordered.append(nearest_pnt)
            current = np.array(nearest_pnt)
        
        return ordered
    
    def smoothCenterline(self, points, factor=0.1):
        if len(points) < 4:
            return points
        
        points_arr = np.array(points)
        x_coords = points_arr[:, 0]
        y_coords = points_arr[:, 1]

        t = np.linspace(0, 1, len(points))

        try:
            interp_x = interp1d(t, x_coords, kind='cubic', fill_value='extrapolate')
            interp_y = interp1d(t, y_coords, kind='cubic', fill_value='extrapolate')

            t_smooth = np.linspace(0, 1, len(points) * 2)
            x_smooth = interp_x(t_smooth)
            y_smooth = interp_y(t_smooth)

            smoothed_points = [(int(x), int(y)) for x, y in zip(x_smooth, y_smooth)]
            return smoothed_points

        except Exception as e:
            print(f"Smoothing failed: {e}. Returning original points")
            return points

    
    def visualizeCenterline(self, use_smoothed=True):
        if self.original_image is None:
            return None
        
        overlay = self.original_image.copy()
        points = self.centerline_smoothed if use_smoothed else self.centerline

        for i in range(1, len(points)):
            cv.line(overlay, points[i - 1], points[i], (0,255,255), thickness=2)

        return overlay
    
    def saveCenterlineToBin(self, filepath, use_smoothed=True):
        points = self.centerline_smoothed if use_smoothed else self.centerline
        if not points:
            print(f"No centerline data to save")
            return False
        
        with open(filepath, 'wb') as f:
            f.write(struct.pack('<I', len(points)))
            for x, y in points:
                f.write(struct.pack('<ff', float(x), float(y)))

        print(f"Centerline saved to {filepath}")
        return True

    def readEdgesFromBin(self, bin_path):
        # Read edge coordinates from binary file
        edges = {'outer_boundary': [], 'inner_boundary': []}

        with open(bin_path, 'rb') as f:
            for key in ['outer_boundary', 'inner_boundary']:
                length_bytes = f.read(4)
                if not length_bytes:
                    break
                num_points = struct.unpack('<I', length_bytes)[0]

                points = []
                for _ in range(num_points):
                    coords = f.read(8)  # 2 floats = 8 bytes
                    x, y = struct.unpack('<ff', coords)
                    points.append((int(round(x)), int(round(y))))
                edges[key] = points

        return edges

    def drawEdgesFromBin(self, bin_path, output_path=None, canvas_size=None):
        # Draw edges from binary file onto a blank canvas
        if canvas_size is None:
            if self.original_image is not None:
                h, w = self.original_image.shape[:2]
                canvas_size = (w, h)
            else:
                canvas_size = (1524, 1024)
        
        if output_path is None:
            output_dir = os.path.dirname(bin_path)
            output_path = os.path.join(output_dir, 'edgeVisualization.png')

        edge_data = self.readEdgesFromBin(bin_path)
        
        outer = edge_data['outer_boundary']
        inner = edge_data['inner_boundary']

        # Create blank white canvas
        image = np.ones((canvas_size[1], canvas_size[0], 3), dtype=np.uint8) * 255

        def draw_contour(points, color):
            for i in range(1, len(points)):
                cv.line(image, points[i - 1], points[i], color, thickness=2)
            if len(points) > 2:
                cv.line(image, points[-1], points[0], color, thickness=2)  # close loop

        draw_contour(outer, (0, 255, 0))  # green
        draw_contour(inner, (0, 0, 255))  # red

        # Save image
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        cv.imwrite(output_path, image)
        print(f"Edge visualization saved to: {output_path}")
        
        return output_path
    
    def saveProcessedImages(self, results, output_dir, base_filename):
        saved_files = []

        os.makedirs(output_dir, exist_ok=True)

        for name, image in results.items():
            if isinstance(image, np.ndarray) and image.ndim in [2, 3]:
                filename = f"{base_filename}_{name}.png"
                filepath = os.path.join(output_dir, filename)
                cv.imwrite(filepath, image)
                saved_files.append(filepath)
                print(f"Saved: {filepath}")

        if self.original_image is not None:
            original_path = os.path.join(output_dir, f"{base_filename}_original.png")
            cv.imwrite(original_path, self.original_image)
            saved_files.append(original_path)
            print(f"Saved: {original_path}")

        return saved_files
    
    def processImageForCSharp(self, img_path):
        """
        Process a track image and return boundary coordinates in a format suitable for C# integration.
        
        Args:
            img_path (str): Path to the track image file
            
        Returns:
            dict: Dictionary containing success status, boundary coordinates, and error information
        """
        try:
            # Load the image
            img = self.loadImg(img_path)
            if img is None:
                return {
                    'success': False,
                    'outer_boundary': [],
                    'inner_boundary': [],
                    'error': 'Failed to load image'
                }
            
            # Process the image
            processed_result = self.processImg(img, show_debug=False)
            if processed_result is None:
                return {
                    'success': False,
                    'outer_boundary': [],
                    'inner_boundary': [],
                    'error': 'Failed to process image'
                }
            
            # Detect boundaries
            boundaries = self.detectBoundaries(processed_result['processed_image'], show_debug=False)
            if boundaries is None:
                return {
                    'success': False,
                    'outer_boundary': [],
                    'inner_boundary': [],
                    'error': 'Failed to detect track boundaries'
                }
            
            # Extract boundary coordinates and convert to simple lists
            outer_coords = []
            inner_coords = []
            
            if boundaries['outer'] is not None:
                # Convert OpenCV contour to list of [x, y] coordinates
                outer_coords = boundaries['outer'].reshape(-1, 2).tolist()
            
            if boundaries['inner'] is not None:
                # Convert OpenCV contour to list of [x, y] coordinates  
                inner_coords = boundaries['inner'].reshape(-1, 2).tolist()
            
            return {
                'success': True,
                'outer_boundary': outer_coords,
                'inner_boundary': inner_coords,
                'error': None
            }
            
        except Exception as e:
            return {
                'success': False,
                'outer_boundary': [],
                'inner_boundary': [],
                'error': str(e)
            }

# Standalone function for direct C# integration
def process_track_for_csharp(img_path):
    """
    Standalone function to process a track image for C# integration.
    
    Args:
        img_path (str): Path to the track image file
        
    Returns:
        dict: Dictionary containing success status, boundary coordinates, and error information
    """
    processor = TrackProcessor()
    return processor.processImageForCSharp(img_path)

def processTrack(img_path, output_base_dir="processedTracks", show_debug=True, centerline_method='skeleton', extract_centerline=False):
    processor = TrackProcessor()
    base_filename = Path(img_path).stem
    output_dir = os.path.join(output_base_dir, base_filename)
    os.makedirs(output_dir, exist_ok=True)

    try:
        print(f"Loading Image: {img_path}")
        processor.loadImg(img_path)
        
        print(f"Processing Image: {base_filename}")
        results = processor.processImg(processor.original_image, show_debug)

        print("Generating boundaries...")
        boundaries = processor.detectBoundaries(results['processed_image'], show_debug)

        if boundaries:
            
            if extract_centerline:
                print(f"Extracting centerline using {centerline_method} method...")
                centerline_results = processor.extractCenterline(method=centerline_method, show_debug=show_debug)

                if centerline_results:
                    results.update(centerline_results)

                    centerline_bin_path = os.path.join(output_dir, f'{base_filename}_centerline.bin')
                    processor.saveCenterlineToBin(centerline_bin_path)
            else:
                print("Skipping centerline extraction")

            def contour_to_list(contour):
                return contour.squeeze().tolist() if contour.ndim == 3 else contour.tolist()

            edge_data = {
                'outer_boundary': contour_to_list(boundaries['outer']),
                'inner_boundary': contour_to_list(boundaries['inner'])
            }

            # Save binary edge coordinates
            edge_bin_path = os.path.join(output_dir, f'{base_filename}_edge_coords.bin')
            os.makedirs(output_dir, exist_ok=True)
            with open(edge_bin_path, 'wb') as f:
                for key in ['outer_boundary', 'inner_boundary']:
                    points = edge_data[key]
                    f.write(struct.pack('<I', len(points)))  # Write number of points
                    for x, y in points:
                        f.write(struct.pack('<ff', float(x), float(y)))  # Write each point as float32
            print(f"Edge coordinates saved to: {edge_bin_path}")

            # Draw edges visualization if debug is enabled
            if show_debug:
                centerline_img = None
                if extract_centerline and processor.centerline:
                    centerline_img = processor.visualizeCenterline()
                if centerline_img is not None:
                    results['centerline_visualization'] = centerline_img

                edge_viz_path = processor.drawEdgesFromBin(edge_bin_path)
                results['edge_visualization'] = cv.imread(edge_viz_path)

        saved_files = processor.saveProcessedImages(results, output_dir, base_filename)

        summary = {
            'original_image': img_path,
            'output_directory': output_dir,
            'processed_files': saved_files,
            'processing_successful': True,
            'centerline_extracted': extract_centerline and processor.centerline is not None,
            'centerline_points': len(processor.centerline) if processor.centerline else 0,

        }

        summary_path = os.path.join(output_dir, f"{base_filename}_summary.json")
        with open(summary_path, 'w') as f:
            json.dump(summary, f, indent=2)

        print(f"Processing complete. Files saved to: {output_dir}")

        if show_debug:
            cv.waitKey(0)
            cv.destroyAllWindows()

        return summary

    except Exception as e:
        print(f"Error processing track image {img_path}: {str(e)}")
        return None
    
def processAllTracks(input_dir='trackImages', output_base_dir='processedTracks', show_debug=True, centerline_method='skeleton', extract_centerline=False):
    extensions = ['*.png', '*.jpg', '*.bmp', '*.tiff']

    image_files = []
    for ext in extensions:
        pattern = os.path.join(input_dir, ext)
        image_files.extend(glob.glob(pattern))

    if not image_files:
        print(f"No image files found in {input_dir}")
        return []
    
    print(f"Found {len(image_files)} image(s) to process")

    results = []
    for img_path in image_files:
        print(f"\n{'='*50}")
        result = processTrack(img_path, output_base_dir, show_debug, centerline_method, extract_centerline)
        if result:
            results.append(result)

    return results


def main():
    parser = argparse.ArgumentParser(description="Process racetrack images for ML algorithm")
    parser.add_argument('--input', '-i', default='trackImages', help='Input directory containing track images (Default: trackImages)')
    parser.add_argument('--output', '-o', default='processedTracks', help='Output base directory (Default: processedTracks)')
    parser.add_argument('--file', '-f', type=str, help='Process a single specific file instead of all files in the input directory')
    parser.add_argument('--debug', '-d', action='store_true', help='Show debug images during processing and generate edge visualization')
    parser.add_argument('--extract-centerline', '-e', action='store_true', help='Extract centerline data (disabled by default)')
    parser.add_argument('--centerline-method', '-c', choices=['skeleton', 'distance_transform', 'medial_axis'], default='skeleton', help='Method for centerline extraction (Default: skeleton)')

    args = parser.parse_args()

    os.makedirs(args.output, exist_ok=True)

    if args.file:
        if os.path.exists(args.file):
            result = processTrack(args.file, args.output, args.debug)
            if result:
                print(f"\nSingle file processing complete")
                if args.extract_centerline and result['centerline_extracted']:
                    print(f"Centerline extracted with {result['centerline_points']} points")
                elif args.extract_centerline:
                    print("Centerline extraction failed")
                else:
                    print("Centerline extraction skipped")
            else:
                print(f"\nFailed to process {args.file}")
        else:
            print(f"File not found: {args.file}")
    else:
        results = processAllTracks(args.input, args.output, args.debug, args.centerline_method, args.extract_centerline)

        print(f"\n{'='*50}")
        print(f"PROCESSING SUMMARY")
        print(f"\n{'='*50}")
        print(f"Total files processed: {len(results)}")
        print(f"Output directory: {args.output}")
        print(f"Centerline extraction: {'Enabled' if args.extract_centerline else 'Disabled'}")
        if args.extract_centerline:
            print(f"Centerline method used: {args.centerline_method}")

        if results:
            print(f"\nProcessed tracks:")
            centerline_success = 0

            for result in results:
                trackName = Path(result['original_image']).stem
                status_info = []

                if args.extract_centerline and result['centerline_extracted']:
                    centerline_success += 1
                    status_info.append(f"{result['centerline_points']} centerline points")
                elif args.extract_centerline:
                    status_info.append("Centerline extraction failed")

                status_str = ", ".join(status_info) if status_info else "basic processing only"
                print(f" - {trackName}: {len(result['processed_files'])} files generated ({status_str})")

            if args.extract_centerline:
                print(f"\nCenterline extraction success rate: {centerline_success}/{len(results)} tracks")

                
if __name__ == "__main__":
    main()