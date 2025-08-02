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
    
    def mouse_callback(self, event, x, y, flags, param):
        if event == cv.EVENT_LBUTTONDOWN:
            self.drawing = True
            # Convert display coordinates to original image coordinates
            orig_x = int(x / self.scale_factor)
            orig_y = int(y / self.scale_factor)
            self.centerline_points = [(orig_x, orig_y)]
            
        elif event == cv.EVENT_MOUSEMOVE and self.drawing:
            # Convert display coordinates to original image coordinates
            orig_x = int(x / self.scale_factor)
            orig_y = int(y / self.scale_factor)
            
            # Add point if it's far enough from the last point to avoid clustering
            if len(self.centerline_points) == 0 or \
               np.sqrt((orig_x - self.centerline_points[-1][0])**2 + 
                      (orig_y - self.centerline_points[-1][1])**2) > 5:
                self.centerline_points.append((orig_x, orig_y))
                
            # Draw on display image
            self.update_display()
                
        elif event == cv.EVENT_LBUTTONUP:
            self.drawing = False
            print(f"Centerline drawn with {len(self.centerline_points)} points")

    def update_display(self):
        self.image = self.display_image.copy()
        
        if len(self.centerline_points) > 1:
            # Draw centerline on display image (scaled coordinates)
            for i in range(1, len(self.centerline_points)):
                pt1 = (int(self.centerline_points[i-1][0] * self.scale_factor),
                       int(self.centerline_points[i-1][1] * self.scale_factor))
                pt2 = (int(self.centerline_points[i][0] * self.scale_factor),
                       int(self.centerline_points[i][1] * self.scale_factor))
                cv.line(self.image, pt1, pt2, (0, 255, 0), 2)
                
        # Show current point count
        if len(self.centerline_points) > 0:
            cv.putText(self.image, f"Points: {len(self.centerline_points)}", 
                      (10, 30), cv.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)
            
    def create_mask_from_centerline(self):
        if len(self.centerline_points) < 2:
            print("Need at least 2 points to create a mask")
            return None
            
        # Create binary mask with original image dimensions
        height, width = self.original_image.shape[:2]
        binary_mask = np.zeros((height, width), dtype=np.uint8)
        
        # Create a thicker line along the centerline
        for i in range(1, len(self.centerline_points)):
            cv.line(binary_mask, self.centerline_points[i-1], self.centerline_points[i], 
                   255, thickness=self.mask_width)
            
        # Optional: Apply morphological operations to smooth the mask
        kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (5, 5))
        binary_mask = cv.morphologyEx(binary_mask, cv.MORPH_CLOSE, kernel)
        binary_mask = cv.morphologyEx(binary_mask, cv.MORPH_OPEN, kernel)
        
        # Convert original image to grayscale for better contrast
        if len(self.original_image.shape) == 3:
            gray_image = cv.cvtColor(self.original_image, cv.COLOR_BGR2GRAY)
        else:
            gray_image = self.original_image.copy()
            
        # Create the final mask by combining the binary mask with the original image
        combined_mask = np.zeros_like(gray_image)
        combined_mask[binary_mask == 255] = gray_image[binary_mask == 255]
        
        self.mask = combined_mask
        self.binary_mask = binary_mask
        return combined_mask
    
    def smooth_centerline(self, factor=0.1):
        if len(self.centerline_points) < 4:
            return self.centerline_points
            
        from scipy.interpolate import interp1d
        
        points_arr = np.array(self.centerline_points)
        x_coords = points_arr[:, 0]
        y_coords = points_arr[:, 1]
        
        # Create parameter t based on cumulative distance
        distances = np.sqrt(np.diff(x_coords)**2 + np.diff(y_coords)**2)
        t = np.concatenate([[0], np.cumsum(distances)])
        t = t / t[-1]  # Normalize to [0, 1]
        
        try:
            # Interpolate with cubic splines
            interp_x = interp1d(t, x_coords, kind='cubic')
            interp_y = interp1d(t, y_coords, kind='cubic')
            
            # Create more points for smoother line
            t_smooth = np.linspace(0, 1, len(self.centerline_points) * 2)
            x_smooth = interp_x(t_smooth)
            y_smooth = interp_y(t_smooth)
            
            smoothed_points = [(int(x), int(y)) for x, y in zip(x_smooth, y_smooth)]
            return smoothed_points
            
        except Exception as e:
            print(f"Smoothing failed: {e}. Using original points")
            return self.centerline_points
        
    def save_centerline(self, output_path):
        if not self.centerline_points:
            print("No centerline to save")
            return False
            
        # Optionally smooth the centerline before saving
        smoothed_points = self.smooth_centerline()
        
        with open(output_path, 'wb') as f:
            f.write(struct.pack('<I', len(smoothed_points)))
            for x, y in smoothed_points:
                f.write(struct.pack('<ff', float(x), float(y)))
                
        print(f"Centerline saved to {output_path} with {len(smoothed_points)} points")
        return True
        
    def save_mask(self, output_path):
        if self.mask is None:
            print("No mask to save")
            return False
            
        cv.imwrite(output_path, self.mask)
        print(f"Mask saved to {output_path}")
        return True
    
    def save_visualization(self, output_path):
        if self.mask is None or not self.centerline_points:
            print("No data to visualize")
            return False
            
        # Create visualization
        viz = self.original_image.copy()
        
        # Draw centerline on the visualization
        for i in range(1, len(self.centerline_points)):
            cv.line(viz, self.centerline_points[i-1], self.centerline_points[i], (0, 255, 0), 3)
            
        # Create a colored version of the binary mask for overlay
        if hasattr(self, 'binary_mask'):
            # Show the mask area with a semi-transparent overlay
            mask_colored = np.zeros_like(viz)
            mask_colored[self.binary_mask == 255] = [0, 255, 255]  # Yellow overlay
            viz = cv.addWeighted(viz, 0.8, mask_colored, 0.2, 0)
        
        cv.imwrite(output_path, viz)
        print(f"Visualization saved to {output_path}")
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