import cv2 as cv
import numpy as np
import matplotlib.pyplot as plt
import json
import os

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
    
    def processImg(self, img):
        # Process the track for easier edge detection
        greyscale = cv.cvtColor(img, cv.COLOR_BGR2GRAY)
        # bilateral filter to reduce noise and preserve edges
        biLatfilter = cv.bilateralFilter(greyscale, 9, 75, 75)

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
        _, thresh = cv.threshold(biLatfilter, 0, 255, cv.THRESH_BINARY + cv.THRESH_OTSU)

        #!!! to be moved to the visualization method
        cv.imshow("Greyscale", greyscale)
        cv.imshow("Filtered", biLatfilter)
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
        return thresh

    def detectBoundaries(self, processedImg):
        # Find track boundaries using color and edge detection
        cannyEdges = cv.Canny(processedImg, 100, 700)

        #!!! to be moved to the visualization method
        rgb = cv.cvtColor(self.original_image, cv.COLOR_BGR2RGB)
        _, axs = plt.subplots(1, 2, figsize=(7,4))
        axs[0].imshow(rgb), axs[0].set_title('Original')
        axs[1].imshow(cannyEdges), axs[1].set_title('Edges')
        for ax in axs:
            ax.set_xticks([]), ax.set_yticks([])
        plt.tight_layout()
        plt.show()

        contours, _ = cv.findContours(cannyEdges, cv.RETR_TREE, cv.CHAIN_APPROX_SIMPLE)
        contours = sorted(contours, key=cv.contourArea, reverse=True)

        print("Number of contours found: "+str(len(contours)))

        outerBoundary = contours[0]
        innerBoundary = contours[2]

        self.track_boundaries = {
            'outer': outerBoundary,
            'inner': innerBoundary
        }

        print("Number of datapoints for outer boundary: ",str(len(outerBoundary)))
        print("Number of datapoints for inner boundary: ",str(len(innerBoundary)))

        #!!! to be moved to visualization method
        contImg = cv.imread(img_path, cv.IMREAD_COLOR)
        cv.drawContours(contImg, outerBoundary, -1, (255,0,0), thickness=2)
        cv.drawContours(contImg, innerBoundary, -1, (0,0,255), thickness=2)
        #cv.drawContours(contImg, contours, -1, 255, thickness=2)
        cv.imshow("Contours", contImg)
        
        return self.track_boundaries
    
def processTrack(img_path, out_dir = "output"):
    processor = TrackProcessor()

    try:
        print(f"Loading Image: {img_path}")
        processor.loadImg(img_path)
        
        print("Processing Image...")
        processor.processImg(processor.original_image)

        
        cv.waitKey(0)
        cv.destroyAllWindows

    except Exception as e:
        print(f"Error processing track image: {str(e)}")
        return None

if __name__ == "__main__":
    img_path = "trackImages/test.png"  # Replace with image path
    output_dir = "trackOutput"  # Replace with output directory
    if not os.path.exists(output_dir):
        os.makedirs(output_dir)

    track_features = processTrack(img_path, output_dir)

    if track_features:
        print(f"Processed track features saved to {output_dir}")
        print("Track features extracted summary:")