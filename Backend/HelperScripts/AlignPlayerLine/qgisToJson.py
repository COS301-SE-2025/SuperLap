import os
import json
import glob
from shapely.geometry import shape

QGIS_DIR = "QGIS_Editing"
OUTPUT_JSON_DIR = "Output_JSONs"

os.makedirs(OUTPUT_JSON_DIR, exist_ok=True)


def process_geojson_file(geojson_path, track_name):
    """Process a single GeoJSON file and convert to drawing JSON format"""
    
    with open(geojson_path, 'r', encoding='utf-8') as f:
        geojson_data = json.load(f)
    
    print(f"  Raw features in {track_name}: {len(geojson_data.get('features', []))}")
    
    output_data = {
        "width": 0,
        "height": 0,
        "boxes": []
    }
    
    all_boundary_points = []
    
    for feature in geojson_data.get('features', []):
        geometry = shape(feature['geometry'])
        properties = feature.get('properties', {})
        feature_type = properties.get('type', '').lower()
        
        print(f"    Feature type: {feature_type}, Geometry: {geometry.geom_type}")
        
        if 'playerline' in feature_type:
            print(f"    -> Skipping playerline")
            continue
        
        if geometry.geom_type == 'Polygon':
            
            if feature_type == 'track_area':
                print(f"    -> Processing track_area polygon")
                
                outer_coords = list(geometry.exterior.coords)[:-1]
                outer_points = [[float(x), float(y)] for x, y in outer_coords]
                print(f"      Outer boundary: {len(outer_points)} points")
                
                if outer_points:
                    output_data["boxes"].append({
                        "points": outer_points,
                        "label": "outer_boundary"
                    })
                    all_boundary_points.extend(outer_points)
                
                for i, interior in enumerate(geometry.interiors):
                    inner_coords = list(interior.coords)[:-1]
                    inner_points = [[float(x), float(y)] for x, y in inner_coords]
                    print(f"      Inner boundary {i+1}: {len(inner_points)} points")
                    
                    if inner_points:
                        output_data["boxes"].append({
                            "points": inner_points,
                            "label": "hole"
                        })
            
            elif 'outer' in feature_type:
                coords = list(geometry.exterior.coords)[:-1]
                points = [[float(x), float(y)] for x, y in coords]
                print(f"    -> Outer boundary: {len(points)} points")
                
                output_data["boxes"].append({
                    "points": points,
                    "label": "outer_boundary"
                })
                all_boundary_points.extend(points)
                
            elif 'inner' in feature_type:
                coords = list(geometry.exterior.coords)[:-1]
                points = [[float(x), float(y)] for x, y in coords]
                print(f"    -> Inner boundary: {len(points)} points")
                
                output_data["boxes"].append({
                    "points": points,
                    "label": "inner_boundary"
                })
                all_boundary_points.extend(points)
            
            else:
                print(f"    -> Unknown polygon type: {feature_type}")
                
        else:
            print(f"    -> Skipping {geometry.geom_type} geometry")
    
    print(f"  Total boundary points collected: {len(all_boundary_points)}")
    print(f"  Total boxes created: {len(output_data['boxes'])}")
    
    if all_boundary_points:
        xs = [p[0] for p in all_boundary_points]
        ys = [p[1] for p in all_boundary_points]
        
        min_x, max_x = min(xs), max(xs)
        min_y, max_y = min(ys), max(ys)
        
        padding = 50
        output_data["width"] = int(max_x - min_x) + padding * 2
        output_data["height"] = int(max_y - min_y) + padding * 2
        
        print(f"  Canvas size: {output_data['width']}x{output_data['height']}")
    else:
        print(f"  Warning: No boundary points found!")
    
    return output_data


def find_geojson_files():
    """Find all GeoJSON files in the QGIS_Editing directory structure"""
    geojson_files = []
    
    pattern = os.path.join(QGIS_DIR, "*", "*.geojson")
    found_files = glob.glob(pattern)
    
    for file_path in found_files:
        dir_path = os.path.dirname(file_path)
        track_name = os.path.basename(dir_path)
        geojson_files.append((file_path, track_name))
    
    direct_pattern = os.path.join(QGIS_DIR, "*.geojson")
    direct_files = glob.glob(direct_pattern)
    
    for file_path in direct_files:
        filename = os.path.basename(file_path)
        track_name = os.path.splitext(filename)[0]
        if track_name.endswith('_for_qgis'):
            track_name = track_name[:-9]
        geojson_files.append((file_path, track_name))
    
    return geojson_files


def main():
    """Main function to process all GeoJSON files"""
    
    print(f"Looking for GeoJSON files in {QGIS_DIR}...")
    
    geojson_files = find_geojson_files()
    
    if not geojson_files:
        print(f"No GeoJSON files found in {QGIS_DIR}")
        return
    
    print(f"Found {len(geojson_files)} GeoJSON files to process:")
    for file_path, track_name in geojson_files:
        print(f"  - {file_path} -> {track_name}")
    
    print("\nProcessing files...")
    
    for geojson_path, track_name in geojson_files:
        try:
            print(f"\nProcessing {track_name}...")
            
            output_data = process_geojson_file(geojson_path, track_name)
            
            output_filename = f"{track_name}.json"
            output_path = os.path.join(OUTPUT_JSON_DIR, output_filename)
            
            with open(output_path, 'w', encoding='utf-8') as f:
                json.dump(output_data, f, indent=2)
            
            print(f"✓ Saved {output_path}")
            
        except Exception as e:
            print(f"✗ Error processing {track_name}: {e}")
            import traceback
            traceback.print_exc()
    
    print(f"\nConversion complete! Check {OUTPUT_JSON_DIR}/ for output files")


if __name__ == "__main__":
    main()