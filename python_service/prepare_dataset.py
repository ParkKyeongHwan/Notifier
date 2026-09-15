import os
import glob
import csv
from pathlib import Path

def prepare_dataset(success_folder, output_csv="dataset.csv"):
    success_dir = Path(success_folder)
    if not success_dir.exists():
        print(f"Error: {success_folder} does not exist.")
        return

    image_extensions = ('*.png', '*.jpg', '*.jpeg')
    image_files = []
    for ext in image_extensions:
        image_files.extend(success_dir.glob(ext))

    dataset = []

    for img_path in image_files:
        filename = img_path.name
        name_without_ext = img_path.stem

        parts = name_without_ext.split('_')
        if len(parts) >= 2:
            label = parts[-1].strip()

            dataset.append({
                "image_path": str(img_path.resolve()),
                "label": label
            })
        else:
            print(f"Warning: Could not parse label from {filename}")

    with open(output_csv, 'w', newline='', encoding='utf-8') as f:
        writer = csv.DictWriter(f, fieldnames=["image_path", "label"])
        writer.writeheader()
        writer.writerows(dataset)

    print(f"Successfully processed {len(dataset)} images.")
    print(f"Dataset mapping saved to {output_csv}")

if __name__ == "__main__":
    success_folder_path = Path.home() / "OneDrive" / "Desktop" / "Success"

    output_csv_path = Path(__file__).parent / "dataset.csv"

    print(f"Looking for images in: {success_folder_path}")
    prepare_dataset(success_folder_path, output_csv_path)