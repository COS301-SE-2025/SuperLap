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
        self.start_position = None
        self.race_direction = None
        
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
            
            orig_x = int(x / self.scale_factor)
            orig_y = int(y / self.scale_factor)
            self.centerline_points = [(orig_x, orig_y)]

            self.start_position = (orig_x, orig_y)
            print(f"Starting line set at: ({orig_x}, {orig_y})")
            
        elif event == cv.EVENT_MOUSEMOVE and self.drawing:
            orig_x = int(x / self.scale_factor)
            orig_y = int(y / self.scale_factor)
            
            # Add point if it's far enough from the last point to avoid clustering
            if len(self.centerline_points) == 0 or \
               np.sqrt((orig_x - self.centerline_points[-1][0])**2 + 
                      (orig_y - self.centerline_points[-1][1])**2) > 5:
                self.centerline_points.append((orig_x, orig_y))
                
                if len(self.centerline_points) >= 10:
                    self.calculate_race_direction()
                
            self.update_display()
                
        elif event == cv.EVENT_LBUTTONUP:
            self.drawing = False
            # Final calculation of race direction
            if len(self.centerline_points) >= 2:
                self.calculate_race_direction()
            print(f"Centerline drawn with {len(self.centerline_points)} points")
            if self.race_direction is not None:
                print(f"Race direction: {self.race_direction:.1f}° (0° = East, 90° = North)")
                # Convert to compass direction for easier understanding
                compass_dir = self.get_compass_direction(self.race_direction)
                print(f"Initial race direction: {compass_dir}")
            
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

    def calculate_race_direction(self):
        if len(self.centerline_points) < 2:
            return None
            
        # Use the first 10 points to determine direction
        end_idx = min(10, len(self.centerline_points))
        start_point = self.centerline_points[0]
        end_point = self.centerline_points[end_idx - 1]
        
        # Calculate angle in degrees
        # Note: Y increases downward
        dx = end_point[0] - start_point[0]
        dy = end_point[1] - start_point[1]
        
        # Calculate angle (0° = East, 90° = South)
        angle_rad = np.arctan2(dy, dx)
        angle_deg = np.degrees(angle_rad)
        
        # Normalize to 0-360 degrees
        if angle_deg < 0:
            angle_deg += 360
            
        self.race_direction = angle_deg
        return angle_deg
        
    def get_compass_direction(self, angle_deg):
        # Adjust for image coordinates (Y increases downward)
        directions = [
            "East", "Southeast", "South", "Southwest", 
            "West", "Northwest", "North", "Northeast"
        ]
        
        # Each direction covers 45 degrees
        idx = int((angle_deg + 22.5) / 45) % 8
        return directions[idx]
            
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
        
        # Create the combined mask with track image
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
            
        # Highlight start position
        if self.start_position is not None:
            cv.circle(viz, self.start_position, 8, (0, 0, 255), -1)  # Red filled circle
            cv.circle(viz, self.start_position, 12, (255, 255, 255), 3)  # White border
            
            # Add start position label
            label_pos = (self.start_position[0] + 25, self.start_position[1] - 10)
            cv.putText(viz, "START", label_pos, cv.FONT_HERSHEY_SIMPLEX, 0.8, (255, 255, 255), 2)
            cv.putText(viz, "START", label_pos, cv.FONT_HERSHEY_SIMPLEX, 0.8, (0, 0, 255), 1)
            
        # Draw direction arrow near start position
        if self.start_position is not None and self.race_direction is not None:
            # Calculate arrow end point
            arrow_length = 50
            angle_rad = np.radians(self.race_direction)
            end_x = int(self.start_position[0] + arrow_length * np.cos(angle_rad))
            end_y = int(self.start_position[1] + arrow_length * np.sin(angle_rad))
            
            # Draw arrow
            cv.arrowedLine(viz, self.start_position, (end_x, end_y), (255, 0, 0), 4, tipLength=0.3)
            
        # Create a colored version of the binary mask for overlay
        if hasattr(self, 'binary_mask'):
            # Show the mask area with a semi-transparent overlay
            mask_colored = np.zeros_like(viz)
            mask_colored[self.binary_mask == 255] = [0, 255, 255]  # Yellow overlay
            viz = cv.addWeighted(viz, 0.8, mask_colored, 0.2, 0)
        
        cv.imwrite(output_path, viz)
        print(f"Visualization saved to {output_path}")
        return True
        
    def run_interactive(self, image_path, output_dir=None):
        if not self.load_image(image_path):
            return False
            
        if output_dir is None:
            output_dir = os.path.join(os.path.dirname(image_path), "centerline_output")
        os.makedirs(output_dir, exist_ok=True)
        
        base_name = Path(image_path).stem
        
        # Setup window and mouse callback
        cv.namedWindow(self.window_name, cv.WINDOW_AUTOSIZE)
        cv.setMouseCallback(self.window_name, self.mouse_callback)
        
        print("\nInstructions:")
        print("- Left click and drag to draw the centerline")
        print("- The FIRST point you click will be the STARTING LINE")
        print("- Draw in the direction you want the race to go")
        print("- Press 'r' to reset and start over")
        print("- Press 's' to save the centerline and mask")
        print("- Press 'w' to adjust mask width (current: {})".format(self.mask_width))
        print("- Press 'ESC' to exit")
        
        while True:
            cv.imshow(self.window_name, self.image)
            key = cv.waitKey(1) & 0xFF
            
            if key == 27:  # ESC key
                break
            elif key == ord('r'):  # Reset
                self.centerline_points = []
                self.mask = None
                self.start_position = None
                self.race_direction = None
                self.image = self.display_image.copy()
                print("Reset centerline, start position, and direction")
            elif key == ord('s'):  # Save
                if len(self.centerline_points) > 1:
                    # Create mask
                    self.create_mask_from_centerline()
                    
                    # Save files
                    centerline_path = os.path.join(output_dir, f"{base_name}_centerline.bin")
                    mask_path = os.path.join(output_dir, f"{base_name}_combined_mask.png")
                    binary_mask_path = os.path.join(output_dir, f"{base_name}_binary_mask.png")
                    viz_path = os.path.join(output_dir, f"{base_name}_visualization.png")
                    
                    self.save_centerline(centerline_path)
                    self.save_mask(mask_path)
                    
                    # Also save the binary mask
                    if hasattr(self, 'binary_mask'):
                        cv.imwrite(binary_mask_path, self.binary_mask)
                        print(f"Binary mask saved to {binary_mask_path}")
                    
                    self.save_visualization(viz_path)
                    
                    # Save metadata
                    metadata = {
                        'original_image': image_path,
                        'centerline_points': len(self.centerline_points),
                        'mask_width': self.mask_width,
                        'image_dimensions': [self.original_image.shape[1], self.original_image.shape[0]],
                        'start_position': {
                            'x': self.start_position[0] if self.start_position else None,
                            'y': self.start_position[1] if self.start_position else None,
                            'description': 'Starting line position (finish line)'
                        },
                        'race_direction': {
                            'angle_degrees': self.race_direction,
                            'compass_direction': self.get_compass_direction(self.race_direction) if self.race_direction else None,
                            'description': 'Initial direction of travel from start position (0° = East, 90° = South)'
                        }
                    }
                    
                    metadata_path = os.path.join(output_dir, f"{base_name}_metadata.json")
                    with open(metadata_path, 'w') as f:
                        json.dump(metadata, f, indent=2)
                    
                    print(f"All files saved to {output_dir}")
                else:
                    print("Need at least 2 points to save")
            elif key == ord('w'):  # Adjust width
                print(f"Current mask width: {self.mask_width}")
                try:
                    new_width = int(input("Enter new mask width (pixels): "))
                    if new_width > 0:
                        self.mask_width = new_width
                        print(f"Mask width set to {self.mask_width}")
                    else:
                        print("Width must be positive")
                except ValueError:
                    print("Invalid input")
                    
        cv.destroyAllWindows()
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