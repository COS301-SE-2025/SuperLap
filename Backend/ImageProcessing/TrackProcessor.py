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
    
def processTrack(img_path, out_dir = "output"):
    processor = TrackProcessor()

    try:
        print(f"Loading Image: {img_path}")
        processor.loadImg(img_path)
        
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