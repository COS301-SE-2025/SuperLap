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

    def loadImg(self, img_path):
        # Load and convert the image
        self.original_image = cv.imread(img_path)
        if self.original_image is None:
            raise ValueError(f"Could not load image from {img_path}")
        print(f"Image loaded successfully.")
        return self.original_image
    
    def processImg(self, img, show_debug=True):
        # Process the track for easier edge detection
        greyscale = cv.cvtColor(img, cv.COLOR_BGR2GRAY)
        # bilateral filter to reduce noise and preserve edges
        bi_lat_filter = cv.bilateralFilter(greyscale, 9, 75, 75)

        hsv = cv.cvtColor(img, cv.COLOR_BGR2HSV)

        # Find black track
        lower_black = np.array([0,0,0])
        upper_black = np.array([360,255,110])
        # Threshold the hsv img to get only black
        dark_mask = cv.inRange(hsv, lower_black, upper_black)

        # Matrix
        kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (7,7))

        # Cleaning image using morphological operations which removes small noise and fills small holes
        closing = cv.morphologyEx(dark_mask, cv.MORPH_CLOSE, kernel, iterations=2)
        opening = cv.morphologyEx(closing, cv.MORPH_OPEN, kernel, iterations=1)

        # Otsu's threshold selection to better define track
        _, thresh = cv.threshold(bi_lat_filter, 0, 255, cv.THRESH_BINARY + cv.THRESH_OTSU)

        if show_debug:
            cv.imshow("Greyscale", greyscale)
            cv.imshow("Filtered", bi_lat_filter)
            cv.imshow("dark_mask", dark_mask)
            cv.imshow("cleaned opening", opening)
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
        cannyEdges = cv.Canny(img, 100, 700)

        contours, _ = cv.findContours(cannyEdges, cv.RETR_TREE, cv.CHAIN_APPROX_SIMPLE)
        contours = sorted(contours, key=cv.contourArea, reverse=True)

        if len(contours) >= 3:
            outerBoundary = contours[0]
            innerBoundary = contours[2]

            self.track_boundaries = {
                'outer': outerBoundary,
                'inner': innerBoundary
            }

            if show_debug:
                rgb = cv.cvtColor(self.original_image, cv.COLOR_BGR2RGB)
                _, axs = plt.subplots(1, 2, figsize=(7,4))
                axs[0].imshow(rgb), axs[0].set_title('Original')
                axs[1].imshow(cannyEdges), axs[1].set_title('Edges')
                for ax in axs:
                    ax.set_xticks([]), ax.set_yticks([])
                plt.tight_layout()
                plt.show()

                print("Number of contours found: " + str(len(contours)))
                print("Number of datapoints for outer boundary: ", str(len(outerBoundary)))
                print("Number of datapoints for inner boundary: ", str(len(innerBoundary)))

                contour_img = self.original_image.copy()
                cv.drawContours(contour_img, [outerBoundary], -1, (255,0,0), thickness=2)
                cv.drawContours(contour_img, [innerBoundary], -1, (0,0,255), thickness=2)
                cv.imshow("Contours", contour_img)
        
        else:
            print("Not enough contours found to detect boundaries.")

        return self.track_boundaries
    
    def extractCenterline(self, method='skeleton', show_debug=True):
        if self.track_mask is None:
            print("Track mask not found")
            return None
        
        track_bin = (self.track_mask > 0).astype(np.uint8)

        if method == 'skeleton':
            skeleton = skeletonize(track_bin).astype(np.uint8) * 255
            centerline_points = self.skeletonToPoints(skeleton)

        elif method == 'distance_transform':
            dist_transform = cv.distanceTransform(track_bin, cv.DIST_L2, 5)


        elif method == 'medial_axis':
            dist_transform = cv.distanceTransform(track_bin, cv.DIST_L2, 5)

        else:
            print(f"{method} not recognised, defaulting to skeleton")


        if show_debug:
            self.visualizeCenterline(skeleton if method == 'skeleton' else None)

        return {
            'centerline_raw'
        }
    
    def skeletonToPoints(self, skeleton):
        points = []
        y_coords, x_coords = np.where(skeleton > 0)
        for x, y in zip(x_coords, y_coords):
            points.append((int(x), int(y)))
        return points
    
    def visualizeCenterline(self, skeleton=None):
        if self.original_image is None:
            return

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
            if image is not None:
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
    
def processTrack(img_path, output_base_dir="processedTracks", show_debug=True, centerline_method='skeleton'):
    processor = TrackProcessor()

    try:
        print(f"Loading Image: {img_path}")
        processor.loadImg(img_path)

        base_filename = Path(img_path).stem
        output_dir = os.path.join(output_base_dir, base_filename)
        
        print(f"Processing Image: {base_filename}")
        results = processor.processImg(processor.original_image, show_debug)

        print("Generating boundaries...")
        boundaries = processor.detectBoundaries(results['processed_image'], show_debug)

        if boundaries:
            print(f"Extracting centerline using {centerline_method} method...")
            centerline_results = processor.extractCenterline(method=centerline_method, show_debug=show_debug)

            if centerline_results:
                results.update(centerline_results)
                centerline_files = processor.saveCenterlineData(output_dir, base_filename)

            def contour_to_list(contour):
                return contour.squeeze().tolist() if contour.ndim == 3 else contour.tolist()

            edge_data = {
                'outer_boundary': contour_to_list(boundaries['outer']),
                'inner_boundary': contour_to_list(boundaries['inner'])
            }

            # Save binary edge coordinates
            edge_bin_path = os.path.join(output_dir, 'edgeCoordinates.bin')
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
                edge_viz_path = processor.drawEdgesFromBin(edge_bin_path)
                results['edge_visualization'] = cv.imread(edge_viz_path)

        saved_files = processor.saveProcessedImages(results, output_dir, base_filename)

        summary = {
            'original_image': img_path,
            'output_directory': output_dir,
            'processed_files': saved_files,
            'processing_successful': True,
            'centerline_extracted': processor.centerline is not None,
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
    
def processAllTracks(input_dir='trackImages', output_base_dir='processedTracks', show_debug=True):
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
        result = processTrack(img_path, output_base_dir, show_debug)
        if result:
            results.append(result)

    return results


def main():
    parser = argparse.ArgumentParser(description="Process racetrack images for ML algorithm")
    parser.add_argument('--input', '-i', default='trackImages', help='Input directory containing track images (Default: trackImages)')
    parser.add_argument('--output', '-o', default='processedTracks', help='Output base directory (Default: processedTracks)')
    parser.add_argument('--file', '-f', type=str, help='Process a single specific file instead of all files in the input directory')
    parser.add_argument('--debug', '-d', action='store_true', help='Show debug images during processing and generate edge visualization')
    parser.add_argument('--centerline-method', '-c', choices=['skeleton', 'distance_transform', 'medial_axis'], default='skeleton', help='Method for centerline extraction (Default: skeleton)')

    args = parser.parse_args()

    os.makedirs(args.output, exist_ok=True)

    if args.file:
        if os.path.exists(args.file):
            result = processTrack(args.file, args.output, args.debug)
            if result:
                print(f"\nSingle file processing complete")
                if result['centerline_extracted']:
                    print(f"Centerline extracted with {result['centerline_points']} points")
            else:
                print(f"\nFailed to process {args.file}")
        else:
            print(f"File not found: {args.file}")
    else:
        results = processAllTracks(args.input, args.output, args.debug, args.centerline_method)

        print(f"\n{'='*50}")
        print(f"PROCESSING SUMMARY")
        print(f"\n{'='*50}")
        print(f"Total files processed: {len(results)}")
        print(f"Output directory: {args.output}")
        print(f"Centerline method used: {args.centerline_method}")

        if results:
            print(f"\nProcessed tracks:")
            centerline_success = 0

            for result in results:
                trackName = Path(result['original_image']).stem
                status_info = []

                if result['centerline_extracted']:
                    centerline_success += 1
                    status_info.append(f"{result['centerline_points']} centerline points")

                status_str = ", ".join(status_info) if status_info else "basic processing only"
                print(f" - {trackName}: {len(result['processed_files'])} files generated ({status_str})")

            print(f"\nCenterline extraction success rate: {centerline_success}/{len(results)} tracks")

                
if __name__ == "__main__":
    main()