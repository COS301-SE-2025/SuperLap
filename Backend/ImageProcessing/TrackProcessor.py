from pathlib import Path
import cv2 as cv
from networkx import astar_path
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

class InteractiveCenterlineDrawer:
    def __init__(self, image):
        self.image = image.copy()
        self.original_image = image.copy()
        self.drawing = False
        self.centerline_points = []
        self.current_stroke = []
        self.window_name = "Draw Centerline - Left click and drag to draw, 'c' to clear, 's' to smooth, ENTER to finish"

    def mouse_callback(self, event, x, y):
        if event == cv.EVENT_LBUTTONDOWN:
            self.drawing = True
            self.current_stroke = [(x, y)]
            
        elif event == cv.EVENT_MOUSEMOVE and self.drawing:
            self.current_stroke.append((x, y))
            cv.circle(self.image, (x, y), 3, (0, 255, 255), -1)  # Yellow dots
            if len(self.current_stroke) > 1:
                cv.line(self.image, self.current_stroke[-2], self.current_stroke[-1], (0, 255, 255), 2)
            cv.imshow(self.window_name, self.image)
            
        elif event == cv.EVENT_LBUTTONUP:
            self.drawing = False
            if len(self.current_stroke) > 1:
                self.centerline_points.extend(self.current_stroke)
                print(f"Stroke added with {len(self.current_stroke)} points. Total points: {len(self.centerline_points)}")
            self.current_stroke = []
        
    def smooth_centerline(self, points, smoothing_factor=0.1):
        if len(points) < 4:
            return points
            
        points_array = np.array(points)
        
        # Remove duplicate consecutive points
        unique_points = [points_array[0]]
        for i in range(1, len(points_array)):
            if np.linalg.norm(points_array[i] - points_array[i-1]) > 2:  # Minimum distance threshold
                unique_points.append(points_array[i])
        
        if len(unique_points) < 4:
            return points
            
        unique_points = np.array(unique_points)
        
        try:
            # Use parametric spline fitting
            t = np.linspace(0, 1, len(unique_points))
            
            # Fit splines for x and y coordinates
            from scipy.interpolate import UnivariateSpline
            spline_x = UnivariateSpline(t, unique_points[:, 0], s=len(unique_points) * smoothing_factor)
            spline_y = UnivariateSpline(t, unique_points[:, 1], s=len(unique_points) * smoothing_factor)
            
            # Generate smoothed points
            t_smooth = np.linspace(0, 1, len(unique_points) * 2)
            smooth_x = spline_x(t_smooth)
            smooth_y = spline_y(t_smooth)
            
            smoothed_points = [(int(x), int(y)) for x, y in zip(smooth_x, smooth_y)]
            return smoothed_points
            
        except Exception as e:
            print(f"Smoothing failed: {e}. Returning original points.")
            return points
        
    def redraw_centerline(self):
        self.image = self.original_image.copy()
        if len(self.centerline_points) > 1:
            for i in range(1, len(self.centerline_points)):
                cv.line(self.image, self.centerline_points[i-1], self.centerline_points[i], (0, 255, 255), 2)
            # Draw points
            for point in self.centerline_points[::5]:  # Draw every 5th point to avoid clutter
                cv.circle(self.image, point, 2, (0, 255, 0), -1)
        cv.imshow(self.window_name, self.image)

class TrackProcessor:
    def __init__(self):
        #initialize track values
        self.original_image = None
        self.processed_image = None
        self.track_mask = None
        self.track_boundaries = None
        self.centerline = None
        self.centerline_smoothed = None
        self.distance_transform = None
        self.adaptive_mask_params = None

    def loadImg(self, img_path):
        # Load and convert the image
        self.original_image = cv.imread(img_path)
        if self.original_image is None:
            raise ValueError(f"Could not load image from {img_path}")
        print(f"Image loaded successfully.")
        return self.original_image
    
    def analyzeImageCharacteristics(self, img):
        hsv = cv.cvtColor(img, cv.COLOR_BGR2HSV)
        lab = cv.cvtColor(img, cv.COLOR_BGR2LAB)

        h_mean, h_std = cv.meanStdDev(hsv[:,:,0])
        s_mean, s_std = cv.meanStdDev(hsv[:,:,1])
        v_mean, v_std = cv.meanStdDev(hsv[:,:,2])

        l_mean, l_std = cv.meanStdDev(lab[:,:,0])
        
        # Analyze histogram peaks for value channel (brightness)
        hist_v = cv.calcHist([hsv], [2], None, [256], [0,256])
        
        # Find dark regions (potential track areas)
        dark_threshold = np.percentile(hsv[:,:,2], 30)  # Bottom 30% of brightness
        bright_threshold = np.percentile(hsv[:,:,2], 70) # Top 30% of brightness
        
        # Adaptive thresholds based on image characteristics
        params = {
            'v_mean': float(v_mean[0][0]),
            'v_std': float(v_std[0][0]),
            'l_mean': float(l_mean[0][0]),
            'l_std': float(l_std[0][0]),
            'dark_threshold': float(dark_threshold),
            'bright_threshold': float(bright_threshold),
            'brightness_factor': 1.0
        }

        print(f"Image analysis - V_mean: {v_mean[0][0]:.1f}, V_std: {v_std[0][0]:.1f}, L_mean: {l_mean[0][0]:.1f}, L_std: {l_std[0][0]:.1f}")
        print(f"Dark threshold: {dark_threshold:.1f}, Bright threshold: {bright_threshold:.1f}")
        
        if v_mean[0][0] < 80:
            print("Very dark surface detected (likely asphalt)...")
            params['track_detection_method'] = 'very_dark_asphalt'
            params['upper_v'] = min(60, dark_threshold + 2.0 * v_std[0][0])
            params['brightness_factor'] = 1.4
        elif v_mean[0][0] < 120:
            print("Dark surface detected (asphalt/dark concrete)...")
            params['track_detection_method'] = 'dark_asphalt'
            params['upper_v'] = min(100, dark_threshold + 1.8 * v_std[0][0])
            params['brightness_factor'] = 1.2
        elif v_mean[0][0] > 160:
            print("Light surface detected (concrete/light surfaces)...")
            params['track_detection_method'] = 'bright_surface'
            params['upper_v'] = min(180, bright_threshold - 0.5 * v_std[0][0])
            params['brightness_factor'] = 0.8
        else:
            print("Standard surface detected...")
            params['track_detection_method'] = 'standard'
            params['upper_v'] = min(110, dark_threshold + 2.5 * v_std[0][0])
            params['brightness_factor'] = 1.0
        
        self.adaptive_mask_params = params
        return params
    
    def createAdaptiveMask(self, img, method='enhanced_multi_approach', show_debug=False):
        hsv = cv.cvtColor(img, cv.COLOR_BGR2HSV)
        gray = cv.cvtColor(img, cv.COLOR_BGR2GRAY)
        lab = cv.cvtColor(img, cv.COLOR_BGR2LAB)
        
        if method == 'enhanced_multi_approach':
            return self.enhancedMultiApproachMask(img, hsv, gray, lab, show_debug)
        elif method == 'multi_approach':
            return self.multiApproachMask(img, hsv, gray, show_debug)
        elif method == 'adaptive_hsv':
            return self.adaptiveHSVMask(hsv, show_debug)
        elif method == 'otsu_adaptive':
            return self.otsuAdaptiveMask(gray, show_debug)
        else:
            return self.adaptiveHSVMask(hsv, show_debug)
        
    def enhancedMultiApproachMask(self, img, hsv, gray, lab, show_debug=False):
        masks = {}
        params = self.analyzeImageCharacteristics(img)
        
        # Method 1: Improved LAB color space detection
        l_channel = lab[:,:,0]
        
        if params['track_detection_method'] in ['very_dark_asphalt', 'dark_asphalt']:
            # Adaptive threshold for dark surfaces
            lab_mask = cv.adaptiveThreshold(l_channel, 255, cv.ADAPTIVE_THRESH_GAUSSIAN_C, cv.THRESH_BINARY_INV, 25, 8)
            kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (3, 3))
            lab_mask = cv.morphologyEx(lab_mask, cv.MORPH_CLOSE, kernel, iterations=1)
        else:
            # Otsu for lighter surfaces
            blurred_l = cv.GaussianBlur(l_channel, (3, 3), 0)
            _, lab_mask = cv.threshold(blurred_l, 0, 255, cv.THRESH_BINARY_INV + cv.THRESH_OTSU)
        
        masks['lab_enhanced'] = lab_mask
        
        # Method 2: Improved HSV masking
        v_channel = hsv[:,:,2]
        
        # Create more accurate HSV mask based on track type
        if params['track_detection_method'] == 'bright_surface':
            # For bright surfaces, look for darker areas within the bright surface
            lower_bound = np.array([0, 0, max(20, int(params['dark_threshold'] - 20))])
            upper_bound = np.array([180, 120, int(params['upper_v'])])
        else:
            # For dark surfaces, be more inclusive
            lower_bound = np.array([0, 0, 0])
            upper_bound = np.array([180, 150, int(params['upper_v'])])
        
        hsv_mask = cv.inRange(hsv, lower_bound, upper_bound)
        masks['hsv_improved'] = hsv_mask
        
        # Method 3: Enhanced Otsu with CLAHE
        clahe = cv.createCLAHE(clipLimit=3.0, tileGridSize=(8,8))
        enhanced_gray = clahe.apply(gray)
        blurred = cv.GaussianBlur(enhanced_gray, (5, 5), 0)
        
        _, otsu_enhanced = cv.threshold(blurred, 0, 255, cv.THRESH_BINARY_INV + cv.THRESH_OTSU)
        masks['otsu_enhanced'] = otsu_enhanced
        
        # Method 4: Gradient-based approach
        # Use Laplacian for better edge detection
        laplacian = cv.Laplacian(gray, cv.CV_64F, ksize=3)
        laplacian = np.absolute(laplacian)
        laplacian = np.uint8(laplacian / laplacian.max() * 255)
        
        # Invert to get low-gradient areas (smooth track surface)
        _, gradient_mask = cv.threshold(laplacian, 20, 255, cv.THRESH_BINARY_INV)
        
        # Clean gradient mask
        kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (7, 7))
        gradient_mask = cv.morphologyEx(gradient_mask, cv.MORPH_CLOSE, kernel, iterations=2)

        corner_response = cv.cornerHarris(gray, 2, 3, 0.04)
        corner_response = cv.dilate(corner_response, None)
        _, corner_mask = cv.threshold(corner_response, 0.01 * corner_response.max(), 255, cv.THRESH_BINARY)
        corner_mask = corner_mask.astype(np.uint8)

        gradient_mask = cv.bitwise_and(gradient_mask, cv.bitwise_not(corner_mask))
        
        masks['gradient_enhanced'] = gradient_mask
        
        # Method 5: Improved K-means clustering
        kmeans_mask = self.createEnhancedKMeansMask(img, n_clusters=4)
        masks['kmeans_enhanced'] = kmeans_mask
        
        # mask combination based on track type
        if params['track_detection_method'] in ['very_dark_asphalt', 'dark_asphalt']:
            weights = {
                'lab_enhanced': 0.35, 
                'hsv_improved': 0.1, 
                'otsu_enhanced': 0.25, 
                'gradient_enhanced': 0.1, 
                'kmeans_enhanced': 0.2
            }
        elif params['track_detection_method'] == 'bright_surface':
            weights = {
                'lab_enhanced': 0.2, 
                'hsv_improved': 0.1, 
                'otsu_enhanced': 0.4, 
                'gradient_enhanced': 0.1, 
                'kmeans_enhanced': 0.2
            }
        else:
            weights = {
                'lab_enhanced': 0.25, 
                'hsv_improved': 0.15, 
                'otsu_enhanced': 0.25, 
                'gradient_enhanced': 0.1, 
                'kmeans_enhanced': 0.25
            }
        
        combined = self.combineMasks(masks, weights)
        
        if show_debug:
            print("Showing mask comparison...")
            self.showMaskComparison(masks, combined)
        
        return combined
    
    def createEnhancedKMeansMask(self, img, n_clusters=4):
        # Reshape image for k-means
        data = img.reshape((-1, 3))
        data = np.float32(data)
        
        # Apply k-means
        criteria = (cv.TERM_CRITERIA_EPS + cv.TERM_CRITERIA_MAX_ITER, 30, 1.0)
        _, labels, centers = cv.kmeans(data, n_clusters, None, criteria, 10, cv.KMEANS_RANDOM_CENTERS)
        
        labels = labels.reshape(img.shape[:2])
        
        # Convert centers to different color spaces for analysis
        centers_bgr = centers.reshape(-1, 1, 3).astype(np.uint8)
        centers_hsv = cv.cvtColor(centers_bgr, cv.COLOR_BGR2HSV).reshape(-1, 3)
        centers_lab = cv.cvtColor(centers_bgr, cv.COLOR_BGR2LAB).reshape(-1, 3)
        
        # Find track-like clusters using multiple criteria
        track_clusters = []
        center_brightness = [center[2] for center in centers_hsv]  # V channel
        center_lightness = [center[0] for center in centers_lab]   # L channel
        
        brightness_threshold = np.mean(center_brightness) - 0.3 * np.std(center_brightness)
        
        for i, (center_hsv, center_lab) in enumerate(zip(centers_hsv, centers_lab)):
            h, s, v = center_hsv
            l_val = center_lab[0]
            
            # Track surfaces: darker than average, moderate saturation
            if v < brightness_threshold and s < 100:
                track_clusters.append(i)
        
        # Fallback: use darkest clusters if none found
        if not track_clusters:
            sorted_indices = np.argsort(center_brightness)
            track_clusters = sorted_indices[:max(1, n_clusters//2)].tolist()
        
        # Create mask for track clusters
        kmeans_mask = np.zeros(img.shape[:2], dtype=np.uint8)
        for cluster_id in track_clusters:
            cluster_mask = (labels == cluster_id).astype(np.uint8) * 255
            kmeans_mask = cv.bitwise_or(kmeans_mask, cluster_mask)
        
        return kmeans_mask
        
    def multiApproachMask(self, img, hsv, gray, show_debug=False):
        masks = {}
        params = self.analyzeImageCharacteristics(img)
        
        # Approach 1: Improved HSV masking
        lower_bound = np.array([0, 0, 0])
        upper_bound = np.array([180, 200, int(params['upper_v'])])
        
        hsv_mask = cv.inRange(hsv, lower_bound, upper_bound)
        masks['hsv'] = hsv_mask
        
        # Approach 2: Adaptive threshold with preprocessing
        blurred = cv.GaussianBlur(gray, (3, 3), 0)
        _, otsu_thresh = cv.threshold(blurred, 0, 255, cv.THRESH_BINARY_INV + cv.THRESH_OTSU)
        masks['otsu'] = otsu_thresh
        
        # Approach 3: Local adaptive threshold
        adaptive_thresh = cv.adaptiveThreshold(gray, 255, cv.ADAPTIVE_THRESH_GAUSSIAN_C, cv.THRESH_BINARY_INV, 21, 10)
        masks['adaptive'] = adaptive_thresh
        
        # Approach 4: Improved K-means
        kmeans_mask = self.createKMeansMask(img, n_clusters=5)
        masks['kmeans'] = kmeans_mask
        
        # Combine masks with adjusted weights
        combined = self.combineMasks(masks, weights={'hsv': 0.2, 'otsu': 0.4, 'adaptive': 0.1, 'kmeans': 0.3})
        
        if show_debug:
            print("Showing mask comparison...")
            self.showMaskComparison(masks, combined)
        
        return combined
    
    def createKMeansMask(self, img, n_clusters=5):
        data = img.reshape((-1, 3))
        data = np.float32(data)
        
        # Apply k-means
        criteria = (cv.TERM_CRITERIA_EPS + cv.TERM_CRITERIA_MAX_ITER, 20, 1.0)
        _, labels, centers = cv.kmeans(data, n_clusters, None, criteria, 10, cv.KMEANS_RANDOM_CENTERS)
        
        labels = labels.reshape(img.shape[:2])
        
        # Find track-like clusters (darker clusters)
        center_brightness = [np.mean(center) for center in centers]
        sorted_indices = np.argsort(center_brightness)
        
        # Take bottom 40% of clusters as potential track
        num_track_clusters = max(1, int(n_clusters * 0.4))
        track_clusters = sorted_indices[:num_track_clusters]
        
        kmeans_mask = np.zeros(img.shape[:2], dtype=np.uint8)
        for cluster_id in track_clusters:
            cluster_mask = (labels == cluster_id).astype(np.uint8) * 255
            kmeans_mask = cv.bitwise_or(kmeans_mask, cluster_mask)
        
        return kmeans_mask
    
    def adaptiveHSVMask(self, hsv, show_debug=False):
        params = self.adaptive_mask_params or self.analyzeImageCharacteristics(cv.cvtColor(hsv, cv.COLOR_HSV2BGR))
        
        lower_bound = np.array([0, 0, 0])
        upper_bound = np.array([180, 200, int(params['upper_v'])])
        mask = cv.inRange(hsv, lower_bound, upper_bound)
        
        if show_debug:
            print(f"Adaptive HSV bounds: Lower {lower_bound}, Upper {upper_bound}")
        
        return mask
    
    def otsuAdaptiveMask(self, gray, show_debug=False):
        blurred = cv.GaussianBlur(gray, (5, 5), 0)
        thresh_val, otsu_mask = cv.threshold(blurred, 0, 255, cv.THRESH_BINARY_INV + cv.THRESH_OTSU)
        if show_debug:
            print(f"Otsu threshold value: {thresh_val}")
        
        return otsu_mask
    
    def combineMasks(self, masks, weights=None):
        if weights is None:
            weights = {name: 1.0/len(masks) for name in masks.keys()}
        
        total_weight = sum(weights.values())
        weights = {k: v/total_weight for k, v in weights.items()}
        
        combined = np.zeros_like(list(masks.values())[0], dtype=np.float32)
        
        for name, mask in masks.items():
            weight = weights.get(name, 0)
            combined += (mask.astype(np.float32) / 255.0) * weight
        
        # lower threshold for more inclusive combination
        final_mask = (combined > 0.4).astype(np.uint8) * 255
        
        return final_mask
    
    def showMaskComparison(self, masks, combined):
        n_masks = len(masks) + 1
        cols = 3
        rows = (n_masks + cols - 1) // cols
        
        plt.figure(figsize=(15, 5 * rows))
        
        for i, (name, mask) in enumerate(masks.items()):
            plt.subplot(rows, cols, i + 1)
            plt.imshow(mask, cmap='gray')
            plt.title(f'{name.upper()} Mask')
            plt.axis('off')
        
        plt.subplot(rows, cols, len(masks) + 1)
        plt.imshow(combined, cmap='gray')
        plt.title('Combined Mask')
        plt.axis('off')
        
        plt.tight_layout()
        plt.show()
        plt.close()
    
    def processImg(self, img, mask_method='enhanced_multi_approach', show_debug=False):
        print(f"Processing image with method: {mask_method}")
        
        # Create adaptive mask
        adaptive_mask = self.createAdaptiveMask(img, method=mask_method, show_debug=show_debug)

        # Apply bilateral filter to reduce noise while preserving edges
        bi_lat_filter = cv.bilateralFilter(adaptive_mask, 9, 80, 80)

        # Calculate kernel size based on image dimensions
        img_area = img.shape[0] * img.shape[1]
        kernel_scale = max(3, int(np.sqrt(img_area) / 400))
        
        print(f"Using kernel scale: {kernel_scale} for image area: {img_area}")

        # Improved morphological operations
        # Fill small holes first
        close_kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (kernel_scale - 1, kernel_scale - 1))
        closing = cv.morphologyEx(bi_lat_filter, cv.MORPH_CLOSE, close_kernel, iterations=1)
        
        # Remove small noise
        open_kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (kernel_scale, kernel_scale))
        opening = cv.morphologyEx(closing, cv.MORPH_OPEN, open_kernel, iterations=3)

        # Final dilation to ensure connectivity
        final_kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (kernel_scale - 1, kernel_scale - 1))
        dilated = cv.dilate(opening, final_kernel, iterations=1)

        # Apply final threshold
        _, thresh = cv.threshold(dilated, 127, 255, cv.THRESH_BINARY)

        if show_debug:
            print("Displaying debug images...")
            cv.imshow("1. Adaptive Mask", adaptive_mask)
            cv.imshow("2. After Bilateral Filter", bi_lat_filter)
            cv.imshow("3. After Morphological Ops", opening)
            cv.imshow("4. Final Processed", thresh)
            cv.waitKey(1)  # Brief pause to allow display

        self.processed_image = thresh
        self.track_mask = thresh
        
        return {
            'processed_image': thresh,
            'track_mask': thresh,
            'adaptive_mask': adaptive_mask,
            'filtered': bi_lat_filter,
            'morphological': opening,
            'mask_params': self.adaptive_mask_params
        }
    
    def detectBoundaries(self, img, show_debug=False):
        print("Using improved boundary detection...")
        return self.detectBoundariesImproved(img, show_debug)
    
    def generatePreciseBoundariesFromSkeleton(self, step_limit=20, min_spacing=5, show_debug=False):
        if self.track_mask is None or self.centerline_smoothed is None:
            print("Missing track mask or centerline")
            return None

        mask = self.track_mask
        h, w = mask.shape

        centerline = self.centerline_smoothed
        inner_points = []
        outer_points = []

        def is_inside(x, y):
            return 0 <= x < w and 0 <= y < h and mask[int(y), int(x)] > 0

        for i in range(1, len(centerline) - 1, min_spacing):
            x0, y0 = centerline[i]
            x1, y1 = centerline[i - 1]
            x2, y2 = centerline[i + 1]

            # Tangent vector
            dx = x2 - x1
            dy = y2 - y1
            length = np.hypot(dx, dy)
            if length == 0:
                continue
            # Perpendicular normal vector
            nx, ny = -dy / length, dx / length

            # Walk outward until track ends
        def trace_outward(x, y, nx, ny, direction):
            for step in range(1, step_limit):
                tx = int(round(x + direction * step * nx))
                ty = int(round(y + direction * step * ny))
                if not is_inside(tx, ty):
                    return (tx, ty)
            return (int(round(x + direction * step_limit * nx)),
                    int(round(y + direction * step_limit * ny)))

        outer_pt = trace_outward(x0, y0, nx, ny, direction=+1)
        inner_pt = trace_outward(x0, y0, nx, ny, direction=-1)

        outer_points.append(outer_pt)
        inner_points.append(inner_pt)

        print(f"Extracted {len(inner_points)} inner and {len(outer_points)} outer boundary points.")

        if show_debug and self.original_image is not None:
            debug_img = self.original_image.copy()
            for pt in outer_points:
                cv.circle(debug_img, pt, 1, (0, 255, 0), -1)  # green
            for pt in inner_points:
                cv.circle(debug_img, pt, 1, (0, 0, 255), -1)  # red
            for pt in centerline[::min_spacing]:
                cv.circle(debug_img, pt, 1, (255, 255, 0), -1)  # yellow
            cv.imshow("Precise Boundary Points", debug_img)
            cv.waitKey(1)

        return {
            'outer': np.array(outer_points, dtype=np.int32).reshape(-1, 1, 2),
            'inner': np.array(inner_points, dtype=np.int32).reshape(-1, 1, 2),
            'method': 'centerline_tracing'
        }

    def detectBoundariesImproved(self, img, show_debug=False):
        if len(img.shape) == 3:
            img_gray = cv.cvtColor(img, cv.COLOR_BGR2GRAY)
        else:
            img_gray = img.copy()

        # Ensure binary image
        _, binary_mask = cv.threshold(img_gray, 127, 255, cv.THRESH_BINARY)
        
        # Calculate distance transform for centerline detection
        self.distance_transform = cv.distanceTransform(binary_mask, cv.DIST_L2, 5)
        
        # Find contours from the binary mask
        contours, _ = cv.findContours(binary_mask, cv.RETR_EXTERNAL, cv.CHAIN_APPROX_SIMPLE)
        
        if not contours:
            print("No contours found in processed image")
            return None
        
        # Filter contours based on area and shape characteristics
        img_area = img_gray.shape[0] * img_gray.shape[1]
        min_area = max(10000, img_area * 0.02)  # At least 2% of image area
        min_perimeter = max(1000, np.sqrt(img_area) * 3)  # Minimum perimeter
        
        valid_contours = []
        for contour in contours:
            area = cv.contourArea(contour)
            perimeter = cv.arcLength(contour, True)
            
            if area > min_area and perimeter > min_perimeter:
                # Additional checks for track-like shapes
                hull = cv.convexHull(contour)
                hull_area = cv.contourArea(hull)
                solidity = area / hull_area if hull_area > 0 else 0
                
                # Check if contour is reasonably complex (not just a simple shape)
                rect = cv.minAreaRect(contour)
                width, height = rect[1]
                if width > 0 and height > 0:
                    aspect_ratio = max(width, height) / (min(width, height) +1e-5)
                    if aspect_ratio < 1.3 or min(width, height) < 15:  # Elongated shape typical of tracks
                        continue

                if solidity > 0.3 and solidity < 0.95:  # Not too simple, not too complex
                    valid_contours.append(contour)
        
        if not valid_contours:
            print("No valid track contours found")
            return self.detectBoundariesFallback(img_gray, show_debug)
        
        # Get the largest valid contour as the main track
        main_contour = max(valid_contours, key=cv.contourArea)
        
        # Create boundaries using improved morphological approach
        boundaries = self.createBoundariesImproved(binary_mask, main_contour, show_debug)
        
        if boundaries is None:
            print("Falling back to alternative boundary detection...")
            return self.detectBoundariesFallback(img_gray, show_debug)
        
        self.track_boundaries = boundaries

        if show_debug and boundaries:
            print("Displaying boundary detection results...")
            # Show distance transform
            dist_norm = cv.normalize(self.distance_transform, None, 0, 255, cv.NORM_MINMAX, dtype=cv.CV_8U)
            cv.imshow("Distance Transform", dist_norm)
            
            # Show boundaries on original image
            if self.original_image is not None:
                contour_img = self.original_image.copy()
                if boundaries['outer'] is not None:
                    cv.drawContours(contour_img, [boundaries['outer']], -1, (0, 255, 0), thickness=2)
                if boundaries['inner'] is not None:
                    cv.drawContours(contour_img, [boundaries['inner']], -1, (0, 0, 255), thickness=2)
                cv.imshow("Detected Boundaries", contour_img)
                cv.waitKey(1)

        return boundaries
    
    def createBoundariesImproved(self, track_mask, main_contour, show_debug=False):
        img_area = track_mask.shape[0] * track_mask.shape[1]

        # Create a clean mask from the main contour
        mask = np.zeros(track_mask.shape, dtype=np.uint8)
        cv.fillPoly(mask, [main_contour], 255)

        # Estimate track half-width
        distance_map = cv.distanceTransform(mask, cv.DIST_L2, 5)
        non_zero = distance_map[distance_map > 0]

        if len(non_zero) == 0:
            print("Warning: Distance transform returned no values.")
            return None

        # Use median or 80th percentile for a more typical width
        estimated_half_width = np.percentile(non_zero, 80)
        print(f"Estimated track half-width (80th percentile): {estimated_half_width:.2f} px")

        # Clamp width to avoid over-expansion
        clamped_half_width = np.clip(estimated_half_width, 5, 12)
        inner_kernel_size = max(3, int(clamped_half_width))
        outer_kernel_size = max(3, int(clamped_half_width + 2))

        print(f"Using inner kernel size: {inner_kernel_size}, outer kernel size: {outer_kernel_size}")

        outer_kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (outer_kernel_size, outer_kernel_size))
        inner_kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (inner_kernel_size, inner_kernel_size))

        # Apply operations
        outer_mask = cv.dilate(mask, outer_kernel, iterations=1)
        inner_mask = cv.erode(mask, inner_kernel, iterations=1)

        # Extract contours
        outer_contours, _ = cv.findContours(outer_mask, cv.RETR_EXTERNAL, cv.CHAIN_APPROX_NONE)
        inner_contours, _ = cv.findContours(inner_mask, cv.RETR_EXTERNAL, cv.CHAIN_APPROX_NONE)

        outer_boundary = max(outer_contours, key=cv.contourArea) if outer_contours else None
        inner_boundary = max(inner_contours, key=cv.contourArea) if inner_contours else None

        if outer_boundary is None or inner_boundary is None:
            print("Fallback to main contour due to invalid boundaries.")
            outer_boundary = outer_boundary or main_contour
            inner_boundary = inner_boundary or main_contour

        print(f"Created boundaries — Outer: {len(outer_boundary)} points, Inner: {len(inner_boundary)} points")

        return {
            'outer': outer_boundary,
            'inner': inner_boundary,
            'method': 'improved_morphological_conservative'
        }


    
    def detectBoundariesFallback(self, img, show_debug=False):
        print("Using fallback edge-based boundary detection...")
        
        # Apply Gaussian blur
        blurred = cv.GaussianBlur(img, (5, 5), 0)
        
        # Use Canny edge detection with adaptive thresholds
        edges = cv.Canny(blurred, 30, 100)
        
        # Morphological operations to close gaps
        kernel = cv.getStructuringElement(cv.MORPH_ELLIPSE, (7, 7))
        edges = cv.morphologyEx(edges, cv.MORPH_CLOSE, kernel, iterations=2)
        edges = cv.dilate(edges, kernel, iterations=1)
        
        # Find contours
        contours, _ = cv.findContours(edges, cv.RETR_EXTERNAL, cv.CHAIN_APPROX_SIMPLE)
        
        if not contours:
            print("No contours found in fallback method")
            return None
        
        # Filter and sort contours
        img_area = img.shape[0] * img.shape[1]
        min_area = max(5000, img_area * 0.01)
        
        valid_contours = [c for c in contours if cv.contourArea(c) > min_area]
        
        filtered = []
        for contour in valid_contours:
            rect = cv.minAreaRect(contour)
            width, height = rect[1]
            if width > 0 and height > 0:
                aspect_ratio = max(width, height) / (min(width, height) + 1e-5)
                if aspect_ratio < 1.3:
                    continue
            filtered.append(contour)

        if len(filtered) < 1:
            print("No valid contours found in fallback method")
            return None
        
        # Sort by area
        valid_contours = sorted(filtered, key=cv.contourArea, reverse=True)
        
        if show_debug:
            fallback_img = cv.cvtColor(img, cv.COLOR_GRAY2BGR)
            cv.imshow("Fallback Edges", edges)
            for i, contour in enumerate(valid_contours[:2]):
                color = [(0, 255, 0), (0, 0, 255)][i] if i < 2 else (255, 0, 0)
                cv.drawContours(fallback_img, [contour], -1, color, thickness=2)
            cv.imshow("Fallback Boundaries", fallback_img)
            cv.waitKey(1)
        
        return {
            'outer': valid_contours[0],
            'inner': valid_contours[1] if len(valid_contours) > 1 else valid_contours[0],
            'method': 'fallback_edge_detection'
        }
    
    def calculateCurvature(self, points, window_size=5):
        curvatures = np.zeros(len(points))
        
        for i in range(len(points)):
            start_idx = max(0, i - window_size)
            end_idx = min(len(points), i + window_size + 1)
            window_points = points[start_idx:end_idx]
            
            if len(window_points) < 3:
                curvatures[i] = 0
                continue
            
            current = points[i]
            start_vec = window_points[0] - current
            end_vec = window_points[-1] - current
            
            start_norm = np.linalg.norm(start_vec)
            end_norm = np.linalg.norm(end_vec)
            
            if start_norm == 0 or end_norm == 0:
                curvatures[i] = 0
                continue
            
            dot_product = np.dot(start_vec, end_vec) / (start_norm * end_norm)
            dot_product = np.clip(dot_product, -1.0, 1.0)
            angle = np.arccos(dot_product)
            
            curvatures[i] = angle
        
        return curvatures
    
    def normalizeContour(self, contour, target_points=1800):
        if contour is None or len(contour) == 0:
            print("Warning: Empty contour provided")
            return np.array([])
    
        min_required_points = max(50, target_points // 36)
        if len(contour) < min_required_points:
            print(f"Warning: Contour has too few points ({len(contour)}) for reliable normalization")
            return contour
        
        if contour.ndim == 3:
            points = contour.squeeze()
        else:
            points = contour

        if not np.array_equal(points[0], points[-1]):
            points = np.vstack([points, points[0]])

        x_coords = points[:, 0]
        y_coords = points[:, 1]

        curvatures = self.calculateCurvature(points)

        #if np.mean(curvatures) > 1.2 or np.max(curvatures) > 2.5:
        #    print("Rejected contour: too jagged or has sharp turns")
        #    return np.array([])

        from scipy.ndimage import gaussian_filter1d
        curvatures_smooth = gaussian_filter1d(curvatures, sigma=2)

        distances = np.sqrt(np.diff(x_coords)**2 + np.diff(y_coords)**2)
        cumulative_dist = np.concatenate([[0], np.cumsum(distances)])

        total_length = cumulative_dist[-1]
        if total_length == 0:
            print("Warning: Contour has zero length")
            return contour
        
        normalized_dist = cumulative_dist / total_length

        try:
            interp_x = interp1d(normalized_dist, x_coords, kind='linear', assume_sorted=True)
            interp_y = interp1d(normalized_dist, y_coords, kind='linear', assume_sorted=True)
            interp_curvature = interp1d(normalized_dist, curvatures_smooth, kind='linear', assume_sorted=True)
            
            base_samples = np.linspace(0, 1, target_points * 2)
            curvature_at_samples = interp_curvature(base_samples)
            
            min_curvature = np.min(curvature_at_samples)
            max_curvature = np.max(curvature_at_samples)
            
            if max_curvature - min_curvature > 0:
                normalized_curvature = 0.2 + 0.8 * (curvature_at_samples - min_curvature) / (max_curvature - min_curvature)
            else:
                normalized_curvature = np.ones_like(curvature_at_samples)
            
            cumulative_density = np.cumsum(normalized_curvature)
            cumulative_density = cumulative_density / cumulative_density[-1]
            
            target_density = np.linspace(0, 1, target_points + 1)[:-1]
            
            adaptive_samples = np.interp(target_density, cumulative_density, base_samples)
            new_x = interp_x(adaptive_samples)
            new_y = interp_y(adaptive_samples)
            
            norm_contour = np.column_stack([new_x, new_y]).astype(np.float32)
            norm_contour = cv.approxPolyDP(norm_contour, epsilon=2.0, closed=True)
            
            final_curvatures = interp_curvature(adaptive_samples)
            high_curvature_points = np.sum(final_curvatures > np.mean(final_curvatures))
            
            print(f"Normalized contour from {len(contour)} to {len(norm_contour)} points")
            print(f"  - {high_curvature_points}/{len(norm_contour)} points in high-curvature areas")
            print(f"  - Curvature range: {np.min(final_curvatures):.3f} to {np.max(final_curvatures):.3f}")
            
            return norm_contour
        
        except Exception as e:
            print(f"Error during curvature-based normalization: {e}")
            print(f"Falling back to even spacing...")
            new_dist = np.linspace(0, 1, target_points + 1)[:-1]
            new_x = interp_x(new_dist)
            new_y = interp_y(new_dist)
            norm_contour = np.column_stack([new_x, new_y]).astype(np.float32)
            return norm_contour

    def extractCenterline(self, method='skeleton', smooth=True, show_debug=True):
        if self.track_mask is None:
            print("Track mask not found")
            return None
        
        track_bin = (self.track_mask > 0).astype(bool)

        #if method == 'skeleton':
        skeleton = skeletonize(track_bin)
        skeleton_img = (skeleton * 255).astype(np.uint8)
        centerline_points = self.skeletonToPoints(skeleton_img)

        #elif method == 'distance_transform':
            #dist_transform = cv.distanceTransform(track_bin, cv.DIST_L2, 5)


        #elif method == 'medial_axis':
            #dist_transform = cv.distanceTransform(track_bin, cv.DIST_L2, 5)

        #else:
            #print(f"{method} not recognised, defaulting to geometric")


        if not centerline_points:
            print(f"No centerline points found with method: {method}")
            return None
        
        ordered_points = self.orderPoints(centerline_points)
        smoothed_points = self.smoothCenterline(ordered_points) if smooth and len(ordered_points) > 10 else ordered_points

        self.centerline = ordered_points
        self.centerline_smoothed = smoothed_points

        if show_debug:
            cv.imshow("skeleton", skeleton)

        return {
            'centerline_raw': ordered_points,
            'centerline_smoothed': smoothed_points,
            'skeleton_image': skeleton if method == 'skeleton' else None
        }
    
    def skeletonToPoints(self, skeleton):
        points = []
        y_coords, x_coords = np.where(skeleton > 0)
        for x, y in zip(x_coords, y_coords):
            points.append((int(x), int(y)))
        return points
    
    def orderPoints(self, points):
        if len(points) < 2:
            return points
        
        points_arr = np.array(points)
        ordered = [points_arr[0]]
        remaining = points_arr[1:].tolist()
        current = np.array(points_arr[0])

        while len(remaining) > 0:
            distances = [np.linalg.norm(current - np.array(p)) for p in remaining]
            nearest = np.argmin(distances)
            nearest_pnt = remaining.pop(nearest)
            ordered.append(nearest_pnt)
            current = np.array(nearest_pnt)
        
        return ordered
    
    def smoothCenterline(self, points, factor=0.1):
        if len(points) < 4:
            return points
        
        points_arr = np.array(points)
        x_coords = points_arr[:, 0]
        y_coords = points_arr[:, 1]

        t = np.linspace(0, 1, len(points))

        try:
            interp_x = interp1d(t, x_coords, kind='cubic', fill_value='extrapolate')
            interp_y = interp1d(t, y_coords, kind='cubic', fill_value='extrapolate')

            t_smooth = np.linspace(0, 1, len(points) * 2)
            x_smooth = interp_x(t_smooth)
            y_smooth = interp_y(t_smooth)

            smoothed_points = [(int(x), int(y)) for x, y in zip(x_smooth, y_smooth)]
            return smoothed_points

        except Exception as e:
            print(f"Smoothing failed: {e}. Returning original points")
            return points

    
    def visualizeCenterline(self, use_smoothed=True):
        if self.original_image is None:
            return None
        
        overlay = self.original_image.copy()
        points = self.centerline_smoothed if use_smoothed else self.centerline

        for i in range(1, len(points)):
            cv.line(overlay, points[i - 1], points[i], (0,255,255), thickness=2)

        return overlay
    
    def saveCenterlineToBin(self, filepath, use_smoothed=True):
        points = self.centerline_smoothed if use_smoothed else self.centerline
        if not points:
            print(f"No centerline data to save")
            return False
        
        with open(filepath, 'wb') as f:
            f.write(struct.pack('<I', len(points)))
            for x, y in points:
                f.write(struct.pack('<ff', float(x), float(y)))

        print(f"Centerline saved to {filepath}")
        return True

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

    def drawEdgesFromBin(self, bin_path, canvas_size=None):
        # Draw edges from binary file onto a blank canvas
        if canvas_size is None:
            if self.original_image is not None:
                h, w = self.original_image.shape[:2]
                canvas_size = (w, h)
            else:
                canvas_size = (1524, 1024)

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

        print(f"Edge visualization image created from: {bin_path}")
        
        return image
    
    def saveProcessedImages(self, results, output_dir, base_filename):
        saved_files = []

        os.makedirs(output_dir, exist_ok=True)

        for name, image in results.items():
            if isinstance(image, np.ndarray) and image.ndim in [2, 3]:
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
    
def processTrack(img_path, output_base_dir="processedTracks", show_debug=True, centerline_method='skeleton', extract_centerline=False):
    processor = TrackProcessor()
    base_filename = Path(img_path).stem
    output_dir = os.path.join(output_base_dir, base_filename)
    os.makedirs(output_dir, exist_ok=True)

    try:
        print(f"Loading Image: {img_path}")
        processor.loadImg(img_path)
        
        print(f"Processing Image: {base_filename}")
        results = processor.processImg(processor.original_image, show_debug=show_debug)

        print("Generating boundaries...")
        boundaries = processor.detectBoundaries(results['processed_image'], show_debug=show_debug)

        if boundaries:
            print("Normalizing boundaries to 1800 points using curvature-based adaptive sampling...")
            normalized_outer = processor.normalizeContour(boundaries['outer'], target_points=1800)
            normalized_inner = processor.normalizeContour(boundaries['inner'], target_points=1800)
            
            boundaries['outer'] = normalized_outer
            boundaries['inner'] = normalized_inner

            if extract_centerline:
                print(f"Extracting centerline using {centerline_method} method...")
                centerline_results = processor.extractCenterline(method=centerline_method, show_debug=show_debug)

                if centerline_results:
                    results.update(centerline_results)

                    centerline_bin_path = os.path.join(output_dir, f'{base_filename}_centerline.bin')
                    processor.saveCenterlineToBin(centerline_bin_path)
            else:
                print("Skipping centerline extraction")

            def contour_to_list(contour):
                return contour.squeeze().tolist() if contour.ndim == 3 else contour.tolist()

            edge_data = {
                'outer_boundary': contour_to_list(normalized_outer),
                'inner_boundary': contour_to_list(normalized_inner)
            }

            # Save binary edge coordinates
            edge_bin_path = os.path.join(output_dir, f'{base_filename}_edge_coords.bin')
            os.makedirs(output_dir, exist_ok=True)
            with open(edge_bin_path, 'wb') as f:
                for key in ['outer_boundary', 'inner_boundary']:
                    points = edge_data[key]
                    f.write(struct.pack('<I', len(points)))
                    for x, y in points:
                        f.write(struct.pack('<ff', float(x), float(y)))
            print(f"Edge coordinates saved to: {edge_bin_path} (outer: {len(edge_data['outer_boundary'])} points, inner: {len(edge_data['inner_boundary'])} points)")

            results['edge_visualization'] = processor.drawEdgesFromBin(edge_bin_path)

            # Add centerline visualization
            if show_debug:
                if extract_centerline and processor.centerline:
                    centerline_img = processor.visualizeCenterline()
                    if centerline_img is not None:
                        results['centerline_visualization'] = centerline_img

        saved_files = processor.saveProcessedImages(results, output_dir, base_filename)

        summary = {
            'original_image': img_path,
            'output_directory': output_dir,
            'processed_files': saved_files,
            'processing_successful': True,
            'boundary_points_normalized': True,
            'outer_boundary_points': 1800 if boundaries else 0,
            'inner_boundary_points': 1800 if boundaries else 0,
            'centerline_extracted': extract_centerline and processor.centerline is not None,
            'centerline_points': len(processor.centerline) if processor.centerline else 0
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
    
def processAllTracks(input_dir='trackImages', output_base_dir='processedTracks', show_debug=True, centerline_method='skeleton', extract_centerline=False):
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
        result = processTrack(img_path, output_base_dir, show_debug, centerline_method, extract_centerline)
        if result:
            results.append(result)

    return results


def main():
    parser = argparse.ArgumentParser(description="Process racetrack images for ML algorithm")
    parser.add_argument('--input', '-i', default='trackImages', help='Input directory containing track images (Default: trackImages)')
    parser.add_argument('--output', '-o', default='processedTracks', help='Output base directory (Default: processedTracks)')
    parser.add_argument('--file', '-f', type=str, help='Process a single specific file instead of all files in the input directory')
    parser.add_argument('--debug', '-d', action='store_true', help='Show debug images during processing and generate edge visualization')
    parser.add_argument('--extract-centerline', '-e', action='store_true', help='Extract centerline data (disabled by default)')
    parser.add_argument('--centerline-method', '-c', choices=['skeleton', 'distance_transform', 'medial_axis'], default='skeleton', help='Method for centerline extraction (Default: skeleton)')

    args = parser.parse_args()

    os.makedirs(args.output, exist_ok=True)

    if args.file:
        if os.path.exists(args.file):
            result = processTrack(args.file, args.output, args.debug, args.centerline_method, args.extract_centerline)
            if result:
                print(f"\nSingle file processing complete")
                print(f"Boundaries normalized to {result['outer_boundary_points']} outer and {result['inner_boundary_points']} inner points")

                if args.extract_centerline and result['centerline_extracted']:
                    print(f"Centerline extracted with {result['centerline_points']} points")
                elif args.extract_centerline:
                    print("Centerline extraction failed")
                else:
                    print("Centerline extraction skipped")
            else:
                print(f"\nFailed to process {args.file}")
        else:
            print(f"File not found: {args.file}")
    else:
        results = processAllTracks(args.input, args.output, args.debug, args.centerline_method, args.extract_centerline)

        print(f"\n{'='*50}")
        print(f"PROCESSING SUMMARY")
        print(f"\n{'='*50}")
        print(f"Total files processed: {len(results)}")
        print(f"Output directory: {args.output}")
        print(f"Centerline extraction: {'Enabled' if args.extract_centerline else 'Disabled'}")
        if args.extract_centerline:
            print(f"Centerline method used: {args.centerline_method}")

        if results:
            print(f"\nProcessed tracks:")
            centerline_success = 0

            for result in results:
                trackName = Path(result['original_image']).stem
                status_info = []
                status_info.append(f"Boundaries normalized to 1800 points each")

                if args.extract_centerline and result['centerline_extracted']:
                    centerline_success += 1
                    status_info.append(f"{result['centerline_points']} centerline points")
                elif args.extract_centerline:
                    status_info.append("Centerline extraction failed")

                status_str = ", ".join(status_info) if status_info else "basic processing only"
                print(f" - {trackName}: {len(result['processed_files'])} files generated ({status_str})")

            if args.extract_centerline:
                print(f"\nCenterline extraction success rate: {centerline_success}/{len(results)} tracks")

                
if __name__ == "__main__":
    main()