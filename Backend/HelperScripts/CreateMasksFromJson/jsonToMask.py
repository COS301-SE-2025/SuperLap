import argparse
import json
import os

from PIL import Image, ImageDraw


def find_box(boxes, keywords):
    for b in boxes:
        lab = b.get("label", "").lower()
        for k in keywords:
            if k in lab:
                return b
    return None


def points_to_tuples(points):
    return [(float(x), float(y)) for x, y in points]


def process_json_file(json_path, output_dir, supersample=3):
    track_name = os.path.splitext(os.path.basename(json_path))[0]
    with open(json_path, "r", encoding="utf-8") as f:
        data = json.load(f)

    width = int(float(data.get("width", 0)))
    height = int(float(data.get("height", 0)))
    if width == 0 or height == 0:
        all_pts = [pt for box in data.get("boxes", []) for pt in box.get("points", [])]
        xs = [p[0] for p in all_pts]
        ys = [p[1] for p in all_pts]
        width = int(max(xs) - min(xs)) + 10
        height = int(max(ys) - min(ys)) + 10

    boxes = data.get("boxes", [])
    inner_box = find_box(boxes, ["outside", "outer"])
    outer_box = find_box(boxes, ["inside", "inner"])

    if not outer_box:
        boxes_sorted = sorted(
            boxes, key=lambda b: len(b.get("points", [])), reverse=True
        )
        outer_box = boxes_sorted[0] if boxes_sorted else None
        inner_box = boxes_sorted[1] if len(boxes_sorted) > 1 else None

    if not outer_box:
        print(f"No polygons found in {json_path}, skipping.")
        return

    outer_pts = points_to_tuples(outer_box["points"])
    inner_pts = points_to_tuples(inner_box["points"]) if inner_box else None

    ss = max(1, int(supersample))
    W, H = width * ss, height * ss

    mask = Image.new("L", (W, H), 0)
    draw = ImageDraw.Draw(mask)

    outer_pts_ss = [(x * ss, y * ss) for x, y in outer_pts]
    draw.polygon(outer_pts_ss, fill=255)

    if inner_pts:
        inner_pts_ss = [(x * ss, y * ss) for x, y in inner_pts]
        draw.polygon(inner_pts_ss, fill=0)

    out_big = Image.new("RGB", (W, H), (255, 255, 255))
    white_layer = Image.new("RGB", (W, H), (0, 0, 0))
    out_big.paste(white_layer, mask=mask)

    out = out_big.resize((width, height), resample=Image.LANCZOS) if ss > 1 else out_big

    os.makedirs(output_dir, exist_ok=True)
    output_path = os.path.join(output_dir, f"{track_name}.png")
    out.save(output_path)
    print(f"Saved {output_path} ({width}x{height})")


def main():
    parser = argparse.ArgumentParser(
        description="Draw masks from all JSON polygons in a folder."
    )
    parser.add_argument(
        "--input_dir",
        "-i",
        default="JSON",
        help="Folder containing JSON files (default: JSON)",
    )
    parser.add_argument(
        "--output_dir",
        "-o",
        default="Tracks",
        help="Folder to save PNG masks (default: Tracks)",
    )
    parser.add_argument(
        "--supersample",
        "-s",
        type=int,
        default=3,
        help="Supersampling factor (default 3)",
    )
    args = parser.parse_args()

    json_files = [f for f in os.listdir(args.input_dir) if f.endswith(".json")]
    if not json_files:
        print(f"No JSON files found in {args.input_dir}")
        return

    for f in json_files:
        json_path = os.path.join(args.input_dir, f)
        process_json_file(json_path, args.output_dir, supersample=args.supersample)


if __name__ == "__main__":
    main()
