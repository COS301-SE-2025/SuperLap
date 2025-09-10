import json
from PIL import Image, ImageDraw
import argparse

def find_box(boxes, keywords):
    for b in boxes:
        lab = b.get("label", "").lower()
        for k in keywords:
            if k in lab:
                return b
    return None

def points_to_tuples(points):
    return [(float(x), float(y)) for x, y in points]

def main():
    parser = argparse.ArgumentParser(description="Draw mask from JSON polygons.")
    parser.add_argument("--input", "-i", default="toMask.json", help="Input JSON file (default: toMask.json)")
    parser.add_argument("--output", "-o", default="mask.png", help="Output PNG file (default: mask.png)")
    parser.add_argument("--supersample", "-s", type=int, default=3,
                        help="Supersampling factor for smoother edges (default 3)")
    args = parser.parse_args()

    with open(args.input, "r", encoding="utf-8") as f:
        data = json.load(f)

    width = int(float(data.get("width", 0)))
    height = int(float(data.get("height", 0)))
    if width == 0 or height == 0:
        all_pts = [pt for box in data.get("boxes", []) for pt in box.get("points", [])]
        xs = [p[0] for p in all_pts]; ys = [p[1] for p in all_pts]
        width = int(max(xs) - min(xs)) + 10
        height = int(max(ys) - min(ys)) + 10

    boxes = data.get("boxes", [])
    outer_box = find_box(boxes, ["outside", "outer"])
    inner_box = find_box(boxes, ["inside", "inner"])

    if not outer_box:
        boxes_sorted = sorted(boxes, key=lambda b: len(b.get("points", [])), reverse=True)
        outer_box = boxes_sorted[0] if boxes_sorted else None
        inner_box = boxes_sorted[1] if len(boxes_sorted) > 1 else None

    if not outer_box:
        raise SystemExit("No polygons found in JSON.")

    outer_pts = points_to_tuples(outer_box["points"])
    inner_pts = points_to_tuples(inner_box["points"]) if inner_box else None

    ss = max(1, int(args.supersample))
    W, H = width * ss, height * ss

    mask = Image.new("L", (W, H), 0)
    draw = ImageDraw.Draw(mask)

    outer_pts_ss = [(x * ss, y * ss) for x, y in outer_pts]
    draw.polygon(outer_pts_ss, fill=255)

    if inner_pts:
        inner_pts_ss = [(x * ss, y * ss) for x, y in inner_pts]
        draw.polygon(inner_pts_ss, fill=0)

    out_big = Image.new("RGB", (W, H), (0, 0, 0))
    white_layer = Image.new("RGB", (W, H), (255, 255, 255))
    out_big.paste(white_layer, mask=mask)

    out = out_big.resize((width, height), resample=Image.LANCZOS) if ss > 1 else out_big
    out.save(args.output)
    print(f"Saved {args.output} ({width}x{height})")

if __name__ == "__main__":
    main()
