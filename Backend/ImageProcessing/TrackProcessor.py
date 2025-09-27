import sys
import cv2 as cv
import numpy as np
from scipy.interpolate import interp1d
import json
import os

NUM_EDGE_POINTS = 4550  # Fixed number of points for each boundary

class TrackProcessor:
    def __init__(self):
        self.original_image = None
        self.processed_image = None
        self.track_mask = None
        self.track_boundaries = None

    def resampleContour(self, contour, target_points=NUM_EDGE_POINTS):
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

    def loadImg(self, img_path):
        self.original_image = cv.imread(img_path)
        if self.original_image is None:
            raise ValueError(f"Could not load image from {img_path}")
        return self.original_image

    def processImg(self, img):
        hsv = cv.cvtColor(img, cv.COLOR_BGR2HSV)

        # Find black track
        lower_black = np.array([0, 0, 0])
        upper_black = np.array([180, 255, 150])
        dark_mask = cv.inRange(hsv, lower_black, upper_black)

        # bilateral filter to reduce noise and preserve edges
        bi_lat_filter = cv.bilateralFilter(dark_mask, 10, 130, 75)

        kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (2, 2))
        opening = cv.morphologyEx(bi_lat_filter, cv.MORPH_OPEN, kernel, iterations=1)
        closing = cv.morphologyEx(opening, cv.MORPH_CLOSE, kernel, iterations=1)

        # Otsu's thresholding
        _, thresh = cv.threshold(closing, 0, 255, cv.THRESH_BINARY + cv.THRESH_OTSU)

        self.processed_image = thresh
        self.track_mask = opening
        return {"processed_image": thresh}

    def detectBoundaries(self, img):
        cannyEdges = cv.Canny(img, 85, 170)
        contours, _ = cv.findContours(cannyEdges, cv.RETR_TREE, cv.CHAIN_APPROX_SIMPLE)

        if not contours or len(contours) < 3:
            return None

        contours = sorted(contours, key=cv.contourArea, reverse=True)
        outer_boundary = contours[0]
        inner_boundary = contours[2]

        self.track_boundaries = {"outer": outer_boundary, "inner": inner_boundary}
        return self.track_boundaries

    def processImageForCSharp(self, img_path):
        try:
            img = self.loadImg(img_path)
            if img is None:
                return {
                    "success": False,
                    "outer_boundary": [],
                    "inner_boundary": [],
                    "error": "Failed to load image"
                }

            processed_result = self.processImg(img)
            if processed_result is None:
                return {
                    "success": False,
                    "outer_boundary": [],
                    "inner_boundary": [],
                    "error": "Failed to process image"
                }

            boundaries = self.detectBoundaries(processed_result["processed_image"])
            if boundaries is None:
                return {
                    "success": False,
                    "outer_boundary": [],
                    "inner_boundary": [],
                    "error": "Failed to detect track boundaries"
                }

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
    
    processor = TrackProcessor()
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