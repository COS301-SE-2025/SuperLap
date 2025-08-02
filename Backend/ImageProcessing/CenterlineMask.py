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