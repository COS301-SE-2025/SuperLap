import os
import json
from shapely.geometry import shape, Polygon

QGIS_DIR = "QGIS_Editing"
OUTPUT_DIR = "Output_JSONs"
os.makedirs(OUTPUT_DIR, exist_ok=True)

TARGET_WIDTH = 2560
TARGET_HEIGHT = 1440
PADDING = 20

def normalize_and_scale_coordinates(all_points):
    """
    Normalize coordinates to handle negative values and scale to fit 1440p resolution.
    Returns scaled points and the bounds used for scaling.
    """
    if not all_points:
        return [], 0, 0
    
    xs = [pt[0] for pt in all_points]
    ys = [pt[1] for pt in all_points]
    
    min_x, max_x = min(xs), max(xs)
    min_y, max_y = min(ys), max(ys)
    
    range_x = max_x - min_x
    range_y = max_y - min_y
    
    if range_x == 0:
        range_x = 1
    if range_y == 0:
        range_y = 1
    
    available_width = TARGET_WIDTH - (2 * PADDING)
    available_height = TARGET_HEIGHT - (2 * PADDING)
    
    scale_x = available_width / range_x
    scale_y = available_height / range_y
    
    scale = min(scale_x, scale_y)
    
    scaled_width = range_x * scale
    scaled_height = range_y * scale
    
    offset_x = (TARGET_WIDTH - scaled_width) / 2
    offset_y = (TARGET_HEIGHT - scaled_height) / 2
    
    scaled_points = []
    for x, y in all_points:
        norm_x = x - min_x
        norm_y = y - min_y
        
        scaled_x = norm_x * scale + offset_x
        scaled_y = norm_y * scale + offset_y
        
        scaled_points.append([scaled_x, scaled_y])
    
    return scaled_points, TARGET_WIDTH, TARGET_HEIGHT

def geojson_to_labelstudio(track_name, geojson_path):
    with open(geojson_path, "r", encoding="utf-8") as f:
        gj = json.load(f)

    boxes = []
    all_original_points = []
    
    for feat in gj.get("features", []):
        geom = shape(feat["geometry"])
        
        if isinstance(geom, Polygon):
            outer = list(map(list, geom.exterior.coords))
            all_original_points.extend(outer)
            
            for hole in geom.interiors:
                inner = list(map(list, hole.coords))
                all_original_points.extend(inner)
    
    if not all_original_points:
        print(f"No polygon coordinates found in {geojson_path}")
        return
    
    scaled_points, width, height = normalize_and_scale_coordinates(all_original_points)
    
    point_map = {}
    for orig, scaled in zip(all_original_points, scaled_points):
        key = (orig[0], orig[1])
        point_map[key] = scaled
    
    features_sorted = []
    for feat in gj.get("features", []):
        geom = shape(feat["geometry"])
        props = feat.get("properties", {})
        label = props.get("type", "unknown").lower()
        
        if isinstance(geom, Polygon):
            priority = 0 if any(keyword in label for keyword in ["outer", "outside"]) else 1
            features_sorted.append((priority, feat))
    
    features_sorted.sort(key=lambda x: x[0])
    
    for priority, feat in features_sorted:
        geom = shape(feat["geometry"])
        props = feat.get("properties", {})
        label = props.get("type", "unknown").lower()

        if isinstance(geom, Polygon):
            outer_coords = list(map(list, geom.exterior.coords))
            outer_scaled = [point_map[(pt[0], pt[1])] for pt in outer_coords]
            
            if "outer" in label or "outside" in label:
                final_label = "outer"
            elif "inner" in label or "inside" in label:
                final_label = "inner"
            else:
                final_label = "outer" if len(boxes) == 0 else "inner"
            
            boxes.append({
                "label": final_label,
                "points": outer_scaled
            })
            
            for hole in geom.interiors:
                inner_coords = list(map(list, hole.coords))
                inner_scaled = [point_map[(pt[0], pt[1])] for pt in inner_coords]
                
                boxes.append({
                    "label": "inner",
                    "points": inner_scaled
                })
    data = {
        "width": int(width),
        "height": int(height),
        "boxes": boxes
    }

    out_path = os.path.join(OUTPUT_DIR, f"{track_name}.json")
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4)
    
    print(f"Saved {out_path} - Scaled to {width}x{height} with {len(boxes)} polygons")
    print(f"  Outer polygons: {sum(1 for b in boxes if 'outer' in b['label'])}")
    print(f"  Inner polygons: {sum(1 for b in boxes if 'inner' in b['label'])}")

def main():
    processed = 0
    skipped = 0
    
    for track_name in os.listdir(QGIS_DIR):
        track_dir = os.path.join(QGIS_DIR, track_name)
        if not os.path.isdir(track_dir):
            continue

        geojson_path = os.path.join(track_dir, f"{track_name}_for_qgis.geojson")
        if not os.path.exists(geojson_path):
            print(f"Skipping {track_name}, no geojson found")
            skipped += 1
            continue

        try:
            geojson_to_labelstudio(track_name, geojson_path)
            processed += 1
        except Exception as e:
            print(f"Error processing {track_name}: {str(e)}")
            skipped += 1
    
    print(f"\nProcessing complete:")
    print(f"  Successfully processed: {processed}")
    print(f"  Skipped/Failed: {skipped}")

if __name__ == "__main__":
    main()