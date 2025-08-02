import cv2 as cv
import numpy as np
import argparse
import os
from pathlib import Path
import json
import struct


class CenterlineMask:
    def __init__(self, mask_width=50):
        self.image = None
        self.original_image = None
        self.display_image = None
        self.centerline_points = []
        self.mask = None
        self.drawing = False
        self.mask_width = mask_width
        self.scale_factor = 1.0
        self.window_name = "Draw Centerline - Left Click and Drag to Draw, 'r' to Reset, 's' to Save, 'ESC' to Exit"

    def load_image(self, image_path):
        self.original_image = cv.imread(image_path)
        if self.original_image is None:
            raise ValueError(f"Could not load image from {image_path}")
        
        # Calculate scale factor to fit image on screen if it's too large
        max_display_size = 1200
        height, width = self.original_image.shape[:2]
        
        if max(height, width) > max_display_size:
            self.scale_factor = max_display_size / max(height, width)
            new_width = int(width * self.scale_factor)
            new_height = int(height * self.scale_factor)
            self.display_image = cv.resize(self.original_image, (new_width, new_height))
        else:
            self.scale_factor = 1.0
            self.display_image = self.original_image.copy()
            
        self.image = self.display_image.copy()
        print(f"Image loaded: {width}x{height}, Display scale: {self.scale_factor:.2f}")
        return True

def main():
    parser = argparse.ArgumentParser(description="Interactive centerline drawing tool for race tracks")
    parser.add_argument('image_path', help='Path to the track image')
    parser.add_argument('--output', '-o', help='Output directory (default: same as image directory)')
    parser.add_argument('--width', '-w', type=int, default=50, help='Initial mask width in pixels (default: 50)')
    
    args = parser.parse_args()
    
    if not os.path.exists(args.image_path):
        print(f"Error: Image file not found: {args.image_path}")
        return
        
    centerline_tool = CenterlineMask(mask_width=args.width)
    centerline_tool.run_interactive(args.image_path, args.output)

if __name__ == "__main__":
    main()