from pathlib import Path
import cv2 as cv
import numpy as np
import matplotlib.pyplot as plt
import json
import os
import argparse
import glob

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
        #kernel = np.ones((3,3), np.uint8)
        kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (7,7))

        # Cleaning image using morphological operations which removes small noise and fills small holes
        closing = cv.morphologyEx(dark_mask, cv.MORPH_CLOSE, kernel, iterations=2)
        opening = cv.morphologyEx(dark_mask, cv.MORPH_OPEN, kernel, iterations=1)

        #background = cv.dilate(closing, kernel, iterations=1)
        #distTransform = cv.distanceTransform(closing, cv.DIST_L2, 0)
        #_, foreground = cv.threshold(distTransform, 0.002 * distTransform.max(), 255, 0)

        # Otsu's threshold selection to better define track
        _, thresh = cv.threshold(bi_lat_filter, 0, 255, cv.THRESH_BINARY + cv.THRESH_OTSU)

        if show_debug:
            cv.imshow("Greyscale", greyscale)
            cv.imshow("Filtered", bi_lat_filter)
            #cv.imwrite("old_mask.png", dark_mask)
            cv.imshow("dark_mask", dark_mask)
            cv.imshow("Otsu", thresh)
            #cv.imshow("closing", closing)
            #cv.imshow("opening", opening)
            #cv.imshow("cleaned", cleaned)
            #cv.imshow("background", background)
            #cv.imshow("foreground", foreground)

        self.processed_image = thresh
        self.track_mask = dark_mask
        return {
            'processed_image': thresh,
            'track_mask': dark_mask,
            'greyscale': greyscale,
            'filtered': bi_lat_filter,
            'closing': closing,
            'opening': opening
        }

    def detectBoundaries(self, img, show_debug=True):
        # Find track boundaries using color and edge detection
        cannyEdges = cv.Canny(img, 100, 700)

        contours, _ = cv.findContours(cannyEdges, cv.RETR_TREE, cv.CHAIN_APPROX_SIMPLE)
        contours = sorted(contours, key=cv.contourArea, reverse=True)

        if show_debug:
            rgb = cv.cvtColor(self.original_image, cv.COLOR_BGR2RGB)
            _, axs = plt.subplots(1, 2, figsize=(7,4))
            axs[0].imshow(rgb), axs[0].set_title('Original')
            axs[1].imshow(cannyEdges), axs[1].set_title('Edges')
            for ax in axs:
                ax.set_xticks([]), ax.set_yticks([])
            plt.tight_layout()
            plt.show()

            print("Number of contours found: "+str(len(contours)))

            outerBoundary = contours[0]
            innerBoundary = contours[2]

            self.track_boundaries = {
                'outer': outerBoundary,
                'inner': innerBoundary
            }

            print("Number of datapoints for outer boundary: ",str(len(outerBoundary)))
            print("Number of datapoints for inner boundary: ",str(len(innerBoundary)))

            contour_img = cv.imread(self.original_image, cv.IMREAD_COLOR)
            cv.drawContours(contour_img, outerBoundary, -1, (255,0,0), thickness=2)
            cv.drawContours(contour_img, innerBoundary, -1, (0,0,255), thickness=2)
            #cv.drawContours(contImg, contours, -1, 255, thickness=2)
            cv.imshow("Contours", contour_img)
        
        return self.track_boundaries
    
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
    
def processTrack(img_path, output_base_dir="processedTracks", show_debug=True):
    processor = TrackProcessor()

    try:
        print(f"Loading Image: {img_path}")
        processor.loadImg(img_path)

        base_filename = Path(img_path).stem
        output_dir = os.path.join(output_base_dir, base_filename)
        
        print(f"Processing Image: {base_filename}")
        results = processor.processImg(processor.original_image, show_debug)

        saved_files = processor.saveProcessedImages(results, output_dir, base_filename)

        summary = {
            'original_image': img_path,
            'output_directory': output_dir,
            'processed_files': saved_files,
            'processing_successful': True
        }

        summary_path = os.path.join(output_dir, f"{base_filename}_summary.json")
        with open(summary_path, 'w') as f:
            json.dump(summary, f, indent=2)

        print(f"Processing complete. Files saved to: {output_dir}")

        if show_debug:
            cv.waitKey(0)
            cv.destroyAllWindows

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
    parser = argparse.ArgumentParser(description="Process tracetrack images for ML algorithm")
    parser.add_argument('--input', '-i', default='trackImages', help='Input directory containing track images (Default: trackImages)')
    parser.add_argument('--output', '-o', default='processedTracks', help='Output base directory (Default: processedTracks)')
    parser.add_argument('--file', '-f', type=str, help='Process a single specific file instead of all files in the input directory')
    parser.add_argument('--debug', '-d', action='store_true', help='Show debug images during processing')

    args = parser.parse_args()

    os.makedirs(args.output, exist_ok=True)

    if args.file:
        if os.path.exists(args.file):
            result = processTrack(args.file, args.output, args.debug)
            if result:
                print(f"\nSingle file processing complete")
            else:
                print(f"\nFailed to process {args.file}")
        else:
            print(f"File not found: {args.file}")
    else:
        results = processAllTracks(args.input, args.output, args.debug)

        print(f"\n{'='*50}")
        print(f"PROCESSING SUMMARY")
        print(f"\n{'='*50}")
        print(f"Total files processed: {len(results)}")
        print(f"Output directory: {args.output}")

        if results:
            print(f"\nProcessed tracks:")
            for result in results:
                trackName = Path(result['original_image']).stem
                print(f" - {trackName}: {len(result['processed_files'])} files generated")
                
if __name__ == "__main__":
    main()