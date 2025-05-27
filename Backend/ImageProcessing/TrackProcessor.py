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

        # Convert img to greyscale
        greyscale = cv.cvtColor(img, cv.COLOR_BGR2GRAY)
        # bilateral filter to reduce noise and preserve edges
        biLatfilter = cv.bilateralFilter(greyscale, 9, 75, 75)

        # Convert to hsv for better colour detection
        hsv = cv.cvtColor(img, cv.COLOR_BGR2HSV)

        # Find black track
        lower_black = np.array([0,0,0])
        upper_black = np.array([360,255,110])
        # Threshold the hsv img to get only black
        dark_mask = cv.inRange(hsv, lower_black, upper_black)

        # Matrix
        #kernel = np.ones((3,3), np.uint8)
        kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (7,7))

        closing = cv.morphologyEx(dark_mask, cv.MORPH_CLOSE, kernel, iterations=2)
        opening = cv.morphologyEx(dark_mask, cv.MORPH_OPEN, kernel, iterations=1)

        # Cleaning image using morphological operations which removes small noise and fills small holes
        #closing = cv.morphologyEx(thresh, cv.MORPH_CLOSE, kernel)
        #opening = cv.morphologyEx(thresh, cv.MORPH_OPEN, kernel)
        #cleaned = cv.morphologyEx(opening, cv.MORPH_CLOSE, kernel)

        #background = cv.dilate(closing, kernel, iterations=1)

        #distTransform = cv.distanceTransform(closing, cv.DIST_L2, 0)
        #_, foreground = cv.threshold(distTransform, 0.002 * distTransform.max(), 255, 0)

        # Otsu's threshold selection to better define track
        _, thresh = cv.threshold(biLatfilter, 0, 255, cv.THRESH_BINARY + cv.THRESH_OTSU)

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