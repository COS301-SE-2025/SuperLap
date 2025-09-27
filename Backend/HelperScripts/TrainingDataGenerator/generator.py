import os
import numpy as np
from PIL import Image
import random

random.seed(42)
np.random.seed(42)

BASE_DIR = "TrainingImages"
OUTPUT_DIR = "data"
CROP_SIZE = 128

TRAIN_RATIO = 0.75
VALID_RATIO = 0.2
TEST_RATIO = 0.05

for split in ["train", "valid", "test"]:
    os.makedirs(os.path.join(OUTPUT_DIR, split), exist_ok=True)

def create_crops_with_split():
    """Create crops and split them properly into train/valid/test"""
    
    all_crops = []
    id_counter = 0
    stride = CROP_SIZE // 4

    print("🔄 Processing folders and creating crops...")

    for foldername in os.listdir(BASE_DIR):
        folder_path = os.path.join(BASE_DIR, foldername)
        if not os.path.isdir(folder_path):
            continue

        sat_path = os.path.join(folder_path, f"{foldername}.png")
        mask_path = os.path.join(folder_path, f"{foldername}(Mask).png")

        if not (os.path.exists(sat_path) and os.path.exists(mask_path)):
            print(f"⚠️ Skipping {foldername}, missing files")
            continue

        real_img = Image.open(sat_path).convert("RGB")
        mask_img = Image.open(mask_path).convert("L")
        mask_array = np.array(mask_img)

        ys, xs = np.where(mask_array > 200)
        coords = list(zip(xs, ys))
        coords.sort(key=lambda p: (p[1], p[0]))

        visited = set()
        folder_crops = []

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

            crop_data = {
                'id': id_counter,
                'real_crop': real_crop,
                'mask_crop': mask_crop,
                'source_folder': foldername
            }
            folder_crops.append(crop_data)
            id_counter += 1

        print(f"📁 {foldername}: {len(folder_crops)} crops")
        all_crops.extend(folder_crops)

    print(f"📊 Total crops created: {len(all_crops)}")

    random.shuffle(all_crops)

    n_total = len(all_crops)
    n_train = int(n_total * TRAIN_RATIO)
    n_valid = int(n_total * VALID_RATIO)
    n_test = n_total - n_train - n_valid

    print(f"📈 Split: Train={n_train}, Valid={n_valid}, Test={n_test}")

    train_crops = all_crops[:n_train]
    valid_crops = all_crops[n_train:n_train + n_valid]
    test_crops = all_crops[n_train + n_valid:]

    print("💾 Saving train set...")
    for crop_data in train_crops:
        crop_data['real_crop'].save(os.path.join(OUTPUT_DIR, "train", f"{crop_data['id']:04d}_sat.jpg"))
        crop_data['mask_crop'].save(os.path.join(OUTPUT_DIR, "train", f"{crop_data['id']:04d}_mask.png"))

    print("💾 Saving validation set...")
    for crop_data in valid_crops:
        crop_data['real_crop'].save(os.path.join(OUTPUT_DIR, "valid", f"{crop_data['id']:04d}_sat.jpg"))
        crop_data['mask_crop'].save(os.path.join(OUTPUT_DIR, "valid", f"{crop_data['id']:04d}_mask.png"))

    print("💾 Saving test set...")
    for crop_data in test_crops:
        crop_data['real_crop'].save(os.path.join(OUTPUT_DIR, "test", f"{crop_data['id']:04d}_sat.jpg"))

    print(f"✅ Finished! Train: {len(train_crops)}, Valid: {len(valid_crops)}, Test: {len(test_crops)}")

    return len(train_crops), len(valid_crops), len(test_crops)
  
create_crops_with_split()
