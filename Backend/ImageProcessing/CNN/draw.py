import json
import os
import sys

import cv2 as cv
import matplotlib.pyplot as plt
import numpy as np
from PIL import Image


def load_boundaries_from_json(json_path):
    """Load boundary data from JSON file"""
    try:
        with open(json_path, "r") as f:
            data = json.load(f)

        if not data.get("success", False):
            print(f"Error in JSON data: {data.get('error', 'Unknown error')}")
            return None, None

        outer_boundary = data.get("outer_boundary", [])
        inner_boundary = data.get("inner_boundary", [])

        if not outer_boundary and not inner_boundary:
            print("No boundary data found in JSON file")
            return None, None

        return outer_boundary, inner_boundary

    except FileNotFoundError:
        print(f"JSON file not found: {json_path}")
        return None, None
    except json.JSONDecodeError:
        print(f"Invalid JSON format in file: {json_path}")
        return None, None
    except Exception as e:
        print(f"Error loading JSON file: {e}")
        return None, None


def draw_boundaries_matplotlib(
    outer_boundary, inner_boundary, title="Track Boundaries"
):
    """Draw boundaries using matplotlib"""
    plt.figure(figsize=(12, 8))

    # Draw outer boundary
    if outer_boundary:
        outer_points = np.array(outer_boundary)
        # Close the boundary by adding first point at the end
        outer_closed = np.vstack([outer_points, outer_points[0]])
        plt.plot(
            outer_closed[:, 0],
            outer_closed[:, 1],
            "r-",
            linewidth=2,
            label="Outer Boundary",
        )

    # Draw inner boundary
    if inner_boundary:
        inner_points = np.array(inner_boundary)
        # Close the boundary by adding first point at the end
        inner_closed = np.vstack([inner_points, inner_points[0]])
        plt.plot(
            inner_closed[:, 0],
            inner_closed[:, 1],
            "b-",
            linewidth=2,
            label="Inner Boundary",
        )

    plt.axis("equal")  # Keep aspect ratio
    plt.grid(True, alpha=0.3)
    plt.legend()
    plt.title(title)
    plt.xlabel("X coordinate")
    plt.ylabel("Y coordinate")

    # Invert Y axis to match image coordinates
    plt.gca().invert_yaxis()

    plt.tight_layout()
    plt.show()


def create_boundary_mask(outer_boundary, inner_boundary, image_shape):
    """Create a binary mask of the track boundaries"""
    height, width = image_shape[:2]
    mask = np.zeros((height, width), dtype=np.uint8)

    if outer_boundary:
        outer_points = np.array(outer_boundary, dtype=np.int32)
        cv.fillPoly(mask, [outer_points], 255)

    if inner_boundary:
        inner_points = np.array(inner_boundary, dtype=np.int32)
        cv.fillPoly(mask, [inner_points], 0)  # Cut out inner area

    return mask


def draw_boundaries_on_image(outer_boundary, inner_boundary, original_image_path=None):
    """Draw boundaries on original image if available"""
    if original_image_path and os.path.exists(original_image_path):
        # Load original image
        original = cv.imread(original_image_path)
        if original is not None:
            # Convert BGR to RGB for matplotlib
            original_rgb = cv.cvtColor(original, cv.COLOR_BGR2RGB)

            # Create overlay
            overlay = original_rgb.copy()

            # Draw boundaries on the image
            if outer_boundary:
                outer_points = np.array(outer_boundary, dtype=np.int32)
                cv.polylines(overlay, [outer_points], True, (255, 0, 0), 3)  # Red

            if inner_boundary:
                inner_points = np.array(inner_boundary, dtype=np.int32)
                cv.polylines(overlay, [inner_points], True, (0, 0, 255), 3)  # Blue

            # Create figure with subplots
            fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(16, 8))

            # Original image
            ax1.imshow(original_rgb)
            ax1.set_title("Original Image")
            ax1.axis("off")

            # Image with boundaries
            ax2.imshow(overlay)
            ax2.set_title("Image with Track Boundaries")
            ax2.axis("off")

            plt.tight_layout()
            plt.show()

            return True

    return False


def save_boundary_image(
    outer_boundary, inner_boundary, output_path, image_size=(800, 600)
):
    """Save boundary visualization as image"""
    # Create a white background
    width, height = image_size
    img = np.ones((height, width, 3), dtype=np.uint8) * 255

    # Draw outer boundary in red
    if outer_boundary:
        outer_points = np.array(outer_boundary, dtype=np.int32)
        cv.polylines(img, [outer_points], True, (0, 0, 255), 2)  # Red in BGR

    # Draw inner boundary in blue
    if inner_boundary:
        inner_points = np.array(inner_boundary, dtype=np.int32)
        cv.polylines(img, [inner_points], True, (255, 0, 0), 2)  # Blue in BGR

    # Save image
    cv.imwrite(output_path, img)
    print(f"Boundary visualization saved to: {output_path}")


def print_boundary_stats(outer_boundary, inner_boundary):
    """Print statistics about the boundaries"""
    print("\n" + "=" * 50)
    print("BOUNDARY STATISTICS")
    print("=" * 50)

    if outer_boundary:
        outer_points = np.array(outer_boundary)
        print(f"Outer boundary points: {len(outer_boundary)}")
        print(
            f"Outer boundary X range: {outer_points[:, 0].min():.1f} to {outer_points[:, 0].max():.1f}"
        )
        print(
            f"Outer boundary Y range: {outer_points[:, 1].min():.1f} to {outer_points[:, 1].max():.1f}"
        )
    else:
        print("No outer boundary data")

    if inner_boundary:
        inner_points = np.array(inner_boundary)
        print(f"Inner boundary points: {len(inner_boundary)}")
        print(
            f"Inner boundary X range: {inner_points[:, 0].min():.1f} to {inner_points[:, 0].max():.1f}"
        )
        print(
            f"Inner boundary Y range: {inner_points[:, 1].min():.1f} to {inner_points[:, 1].max():.1f}"
        )
    else:
        print("No inner boundary data")


def main():
    # Default file path
    json_path = "Output.json"

    # Allow custom JSON file path as command line argument
    if len(sys.argv) > 1:
        json_path = sys.argv[1]

    print(f"Loading boundaries from: {json_path}")

    # Load boundary data
    outer_boundary, inner_boundary = load_boundaries_from_json(json_path)

    if outer_boundary is None and inner_boundary is None:
        print("Failed to load boundary data. Exiting.")
        return

    # Print statistics
    print_boundary_stats(outer_boundary, inner_boundary)

    # Draw boundaries using matplotlib
    print("\nDisplaying boundaries...")
    draw_boundaries_matplotlib(outer_boundary, inner_boundary)

    # Try to overlay on original image if available
    # Common image names to try
    possible_images = [
        "Losail(Masked).png",
        "original.png",
        "input.png",
        "track.png",
        "image.png",
    ]

    overlay_success = False
    for img_path in possible_images:
        if os.path.exists(img_path):
            print(f"\nFound original image: {img_path}")
            print("Displaying boundaries overlaid on original image...")
            overlay_success = draw_boundaries_on_image(
                outer_boundary, inner_boundary, img_path
            )
            break

    if not overlay_success:
        print(f"\nNo original image found. Tried: {', '.join(possible_images)}")
        print(
            "To overlay on original image, ensure one of these files exists in the current directory"
        )

    # Save boundary visualization
    output_image = "track_boundaries.png"

    # Determine image size from boundaries
    all_points = []
    if outer_boundary:
        all_points.extend(outer_boundary)
    if inner_boundary:
        all_points.extend(inner_boundary)

    if all_points:
        all_points = np.array(all_points)
        width = int(all_points[:, 0].max() - all_points[:, 0].min()) + 100
        height = int(all_points[:, 1].max() - all_points[:, 1].min()) + 100
        save_boundary_image(
            outer_boundary, inner_boundary, output_image, (width, height)
        )

    print(f"\nProcessing complete!")


if __name__ == "__main__":
    main()
