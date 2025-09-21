import os
import numpy as np
from PIL import Image

MASK_PATH = "10(Mask).png"   # change to the files you want to process
IMAGE_PATH = "10.png" 

OUTPUT_MASK_DIR = "mask"
OUTPUT_IMAGE_DIR = "image"
CROP_SIZE = 128

os.makedirs(OUTPUT_MASK_DIR, exist_ok=True)
os.makedirs(OUTPUT_IMAGE_DIR, exist_ok=True)

mask_img = Image.open(MASK_PATH).convert("L")
real_img = Image.open(IMAGE_PATH).convert("RGB")

mask_array = np.array(mask_img)

ys, xs = np.where(mask_array > 200)

coords = list(zip(xs, ys))

coords.sort(key=lambda p: (p[1], p[0]))

stride = CROP_SIZE // 2
id_counter = 0
visited = set()

for x, y in coords:
    x0 = max(0, x - CROP_SIZE // 2)
    y0 = max(0, y - CROP_SIZE // 2)
    x1 = x0 + CROP_SIZE
    y1 = y0 + CROP_SIZE

    if x1 > mask_img.width or y1 > mask_img.height:
        continue

    grid_key = (x0 // stride, y0 // stride)
    if grid_key in visited:
        continue
    visited.add(grid_key)

    mask_crop = mask_img.crop((x0, y0, x1, y1))
    real_crop = real_img.crop((x0, y0, x1, y1))

    filename = f"{id_counter}.png"
    mask_crop.save(os.path.join(OUTPUT_MASK_DIR, filename))
    real_crop.save(os.path.join(OUTPUT_IMAGE_DIR, filename))

    id_counter += 1

print(f"Saved {id_counter} crops to '{OUTPUT_MASK_DIR}' and '{OUTPUT_IMAGE_DIR}'")
