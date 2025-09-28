import json

from PIL import Image, ImageDraw


def main():
    input_file = "Aragon_for_qgis.geojson"
    with open(input_file, "r", encoding="utf-8") as f:
        geojson_data = json.load(f)

    outer_coords = geojson_data["features"][0]["geometry"]["coordinates"][1]
    inner_coords = geojson_data["features"][0]["geometry"]["coordinates"][0]

    all_coords = outer_coords + inner_coords
    x_coords = [coord[0] for coord in all_coords]
    y_coords = [coord[1] for coord in all_coords]

    min_x, max_x = min(x_coords), max(x_coords)
    min_y, max_y = min(y_coords), max(y_coords)

    padding = 50
    width = int(max_x - min_x) + 2 * padding
    height = int(max_y - min_y) + 2 * padding

    # Coordinate transform
    def to_image_space(x, y):
        return (x - min_x + padding, height - (y - min_y) - padding)

    outer_points = [to_image_space(x, y) for x, y in outer_coords]
    inner_points = [to_image_space(x, y) for x, y in inner_coords]

    # Create mask (white outer, black inner hole)
    mask = Image.new("L", (width, height), 0)
    mask_draw = ImageDraw.Draw(mask)
    mask_draw.polygon(outer_points, fill=255)  # fill track area
    mask_draw.polygon(inner_points, fill=0)  # punch hole

    # Apply mask
    img = Image.new("RGB", (width, height), "black")
    white_layer = Image.new("RGB", (width, height), "white")
    img.paste(white_layer, mask=mask)

    output_file = "aragon_track.png"
    img.save(output_file)
    print(f"Saved {output_file} ({width}x{height})")


if __name__ == "__main__":
    main()
