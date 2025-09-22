import os
import numpy as np
from PIL import Image

BASE_DIR = "TrainingImages"
OUTPUT_DIR = "data"
CROP_SIZE = 128

# make output dirs
for split in ["train", "valid", "test"]:
    os.makedirs(os.path.join(OUTPUT_DIR, split), exist_ok=True)

id_counter = 0
stride = CROP_SIZE // 4   # overlap stride

# iterate over all subfolders
for foldername in os.listdir(BASE_DIR):
    folder_path = os.path.join(BASE_DIR, foldername)
    if not os.path.isdir(folder_path):
        continue

    sat_path = os.path.join(folder_path, f"{foldername}.png")
    mask_path = os.path.join(folder_path, f"{foldername}(Mask).png")

    if not (os.path.exists(sat_path) and os.path.exists(mask_path)):
        print(f"Skipping {foldername}, missing files")
        continue

    # load images
    real_img = Image.open(sat_path).convert("RGB")
    mask_img = Image.open(mask_path).convert("L")
    mask_array = np.array(mask_img)

    ys, xs = np.where(mask_array > 200)
    coords = list(zip(xs, ys))
    coords.sort(key=lambda p: (p[1], p[0]))

    visited = set()

    for x, y in coords:
        x0 = max(0, x - CROP_SIZE // 4)
        y0 = max(0, y - CROP_SIZE // 4)
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

        # save in train
        real_crop.save(os.path.join(OUTPUT_DIR, "train", f"{id_counter}_sat.jpg"))
        mask_crop.save(os.path.join(OUTPUT_DIR, "train", f"{id_counter}_mask.png"))

        # save in valid
        real_crop.save(os.path.join(OUTPUT_DIR, "valid", f"{id_counter}_sat.jpg"))
        mask_crop.save(os.path.join(OUTPUT_DIR, "valid", f"{id_counter}_mask.png"))

        # save only sat in test
        real_crop.save(os.path.join(OUTPUT_DIR, "test", f"{id_counter}_sat.jpg"))

        id_counter += 1

print(f"✅ Finished. Total crops saved: {id_counter}")
