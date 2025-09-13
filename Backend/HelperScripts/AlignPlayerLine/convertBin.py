import os
import struct
import json

BIN_DIR = "bin"
CSV_INPUT_DIR = "CSVInput"
OUTPUT_DIR = "BinToLabelStudio"

# Placeholder canvas image (can be replaced with an actual track image)
CANVAS_IMAGE = "https://dummyimage.com/1920x1080/ffffff/000000.png"
CANVAS_WIDTH = 1920
CANVAS_HEIGHT = 1080

os.makedirs(OUTPUT_DIR, exist_ok=True)


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
        # Skip raceline and optional playerline
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


def normalize_points(points, width=CANVAS_WIDTH, height=CANVAS_HEIGHT):
    """Normalize coordinates to 0–100% relative to canvas size."""
    if not points:
        return []
    xs, ys = zip(*points)
    min_x, max_x = min(xs), max(xs)
    min_y, max_y = min(ys), max(ys)

    scale_x = max_x - min_x if max_x - min_x != 0 else 1
    scale_y = max_y - min_y if max_y - min_y != 0 else 1

    normalized = []
    for x, y in points:
        nx = (x - min_x) / scale_x * 100
        ny = (y - min_y) / scale_y * 100
        normalized.append([nx, ny])
    return normalized


def save_labelstudio_json(outer, inner, playerline, track_name):
    def make_ls_polygon(points, label_name, closed=True):
        return {
            "id": label_name,
            "type": "polygon",
            "label": label_name,
            "points": points,
            "closed": closed
        }

    # Normalize points to 0–100%
    outer = normalize_points(outer)
    inner = normalize_points(inner)
    playerline = normalize_points(playerline)

    polygons = []
    if outer:
        polygons.append(make_ls_polygon(outer, "Outer Boundary", closed=True))
    if inner:
        polygons.append(make_ls_polygon(inner, "Inner Boundary", closed=True))
    if playerline:
        polygons.append(make_ls_polygon(playerline, "Playerline", closed=False))

    # Correct Label Studio structure
    data = {
        "data": {
            "canvas": CANVAS_IMAGE  # must exist
        },
        "annotations": [
            {
                "result": polygons
            }
        ]
    }

    output_path = os.path.join(OUTPUT_DIR, f"{track_name}_labelstudio.json")
    with open(output_path, "w") as f:
        json.dump(data, f, indent=4)
    print(f"Label Studio JSON saved to {output_path}")



def main():
    bin_result = list_bin_files()
    if not bin_result:
        return
    bin_path, track_name = bin_result

    outer, inner, playerline_from_bin = load_bin_boundaries(bin_path)

    # Automatically load CSV playerline if it exists
    playerline_csv = load_playerline_csv(track_name)
    playerline = playerline_csv if playerline_csv else playerline_from_bin

    save_labelstudio_json(outer, inner, playerline, track_name)


if __name__ == "__main__":
    main()
