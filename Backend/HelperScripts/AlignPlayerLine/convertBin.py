import os
import struct
import json
import numpy as np
import geopandas as gpd
from shapely.geometry import Polygon, LineString

BIN_DIR = "bin"
CSV_INPUT_DIR = "CSVInput"
QGIS_DIR = "QGIS_Editing"
OUTPUT_DIR = "Output"

CANVAS_IMAGE = "https://dummyimage.com/1920x1080/ffffff/000000.png"
CANVAS_WIDTH = 1920
CANVAS_HEIGHT = 1080

os.makedirs(QGIS_DIR, exist_ok=True)

def get_json_path(track_name):
    return os.path.join(OUTPUT_DIR, f"{track_name}.json")


def load_transform(track_name):
    """Load transform settings from ./Output/{track_name}.json"""
    path = get_json_path(track_name)
    if os.path.exists(path):
        with open(path, "r") as f:
            try:
                data = json.load(f)
                print(f"Loaded transform values from {path}")
                return data
            except Exception as e:
                print(f"Failed to load {path}: {e}")
    return {}


def apply_transform(points, tx=0, ty=0, scale=1.0, rotation_deg=0,
                    reflect_x=False, reflect_y=False, shear_x=0.0, shear_y=0.0):
    if not points:
        return []

    arr = np.array(points, dtype=float)
    angle = np.radians(rotation_deg)

    rot_matrix = np.array([[np.cos(angle), -np.sin(angle)],
                           [np.sin(angle),  np.cos(angle)]])
    shear_matrix = np.array([[1, shear_x],
                             [shear_y, 1]])
    transform_matrix = rot_matrix @ shear_matrix

    transformed = (arr @ transform_matrix.T) * scale
    if reflect_x:
        transformed[:, 0] *= -1
    if reflect_y:
        transformed[:, 1] *= -1
    transformed[:, 0] += tx
    transformed[:, 1] += ty
    return transformed.tolist()

def list_bin_files():
    bin_files = [f for f in os.listdir(BIN_DIR) if f.endswith(".bin")]
    if not bin_files:
        print(f"No BIN files found in {BIN_DIR}")
        return None
    print("\nAvailable BIN files:")
    for i, f in enumerate(bin_files, 1):
        print(f" {i}. {f}")
    choice = int(input("Select a BIN file: ")) - 1
    return os.path.join(BIN_DIR, bin_files[choice]), bin_files[choice].replace(".bin", "")


def load_bin_boundaries(bin_path):
    """Load outer and inner boundaries from the bin file."""
    def read_points(reader):
        count = struct.unpack("<i", reader.read(4))[0]
        return [list(struct.unpack("<ff", reader.read(8))) for _ in range(count)]

    with open(bin_path, "rb") as f:
        outer = read_points(f)
        inner = read_points(f)
        for _ in range(2):
            try:
                _ = read_points(f)
            except:
                pass
        playerline = []
        try:
            playerline = read_points(f)
        except:
            pass
    return outer, inner, playerline


def load_playerline_csv(track_name):
    """Load playerline from CSV file with the same name as the bin file."""
    csv_file = os.path.join(CSV_INPUT_DIR, f"{track_name}.csv")
    if not os.path.exists(csv_file):
        print(f"No matching CSV found for {track_name}, skipping playerline CSV.")
        return []

    xs, ys = [], []
    with open(csv_file, "r") as f:
        header = f.readline().strip().split("\t")
        x_idx = header.index("world_position_X")
        y_idx = header.index("world_position_Y")

        for line in f:
            fields = line.strip().split("\t")
            if len(fields) <= max(x_idx, y_idx):
                continue
            xs.append(float(fields[x_idx]))
            ys.append(float(fields[y_idx]))
    return [list(p) for p in zip(xs, ys)]

from shapely.geometry import Polygon, LineString

from shapely.geometry import Polygon, LineString

def save_qgis_files(outer, inner, playerline, track_name):
    """Save boundaries and playerline as GeoJSON for QGIS editing"""
    features = []

    if outer and inner:
        track_poly = Polygon(shell=outer, holes=[inner])
        features.append({
            "type": "Feature",
            "properties": {"type": "track_area", "name": track_name},
            "geometry": track_poly.__geo_interface__
        })
    else:
        if outer:
            track_poly = Polygon(shell=outer)
            features.append({
                "type": "Feature",
                "properties": {"type": "outer_boundary", "name": track_name},
                "geometry": track_poly.__geo_interface__
            })
        if inner:
            track_poly = Polygon(shell=inner)
            features.append({
                "type": "Feature",
                "properties": {"type": "inner_boundary", "name": track_name},
                "geometry": track_poly.__geo_interface__
            })

    if playerline:
        player_line = LineString(playerline)
        features.append({
            "type": "Feature",
            "properties": {
                "type": "playerline",
                "name": track_name,
                "editable": False
            },
            "geometry": player_line.__geo_interface__
        })

    geojson = {"type": "FeatureCollection", "features": features}

    track_dir = os.path.join(QGIS_DIR, track_name)
    os.makedirs(track_dir, exist_ok=True)
    output_path = os.path.join(track_dir, f"{track_name}_for_qgis.geojson")

    with open(output_path, "w") as f:
        json.dump(geojson, f, indent=4)
    print(f"QGIS GeoJSON saved to {output_path}")
    save_qgis_project(track_name)

def save_qgis_project(track_name):
    """Create a minimal QGIS .qgs project file (XML-based)"""
    project_xml = f"""<?xml version="1.0" encoding="UTF-8"?>
<qgis projectname="{track_name}" version="3.28">
  <projectlayers>
    <maplayer type="vector" geometry="Polygon" name="Track Boundaries">
      <datasource>{track_name}_for_qgis.geojson</datasource>
      <provider>ogr</provider>
    </maplayer>
  </projectlayers>
  <properties>
    <snapping>
      <enabled>true</enabled>
      <mode>vertex</mode>
      <tolerance>10</tolerance>
      <units>pixels</units>
    </snapping>
  </properties>
</qgis>
"""
    track_dir = os.path.join(QGIS_DIR, track_name)
    os.makedirs(track_dir, exist_ok=True)
    project_path = os.path.join(track_dir, f"{track_name}_project.qgs")
    with open(project_path, "w", encoding="utf-8") as f:
        f.write(project_xml)
    print(f"QGIS project file saved to {project_path}")

def main():
    bin_result = list_bin_files()
    if not bin_result:
        return
    bin_path, track_name = bin_result

    outer, inner, playerline_from_bin = load_bin_boundaries(bin_path)

    playerline_csv = load_playerline_csv(track_name)
    playerline = playerline_csv if playerline_csv else playerline_from_bin

    tf = load_transform(track_name)
    if tf and playerline:
        playerline = apply_transform(
            playerline,
            tx=tf.get("tx", 0),
            ty=tf.get("ty", 0),
            scale=tf.get("scale", 1.0),
            rotation_deg=tf.get("rotation", 0),
            reflect_x=tf.get("reflect_x", False),
            reflect_y=tf.get("reflect_y", False),
            shear_x=tf.get("shear_x", 0.0),
            shear_y=tf.get("shear_y", 0.0),
        )
        print(f"Applied transform to playerline for {track_name}")

    save_qgis_files(outer, inner, playerline, track_name)


if __name__ == "__main__":
    main()
