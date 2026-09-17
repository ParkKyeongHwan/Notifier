import os
import csv
import shutil
from pathlib import Path

def main():
    print("==========================================================")
    print(" EasyOCR Fine-tuning Preparation Script")
    print("==========================================================")

    dataset_csv = Path(__file__).parent / "dataset.csv"
    if not dataset_csv.exists():
        print(f"Error: {dataset_csv} not found. Run prepare_dataset.py first.")
        return

    # Create EasyOCR training dataset format
    train_dir = Path(__file__).parent / "train_data"
    train_dir.mkdir(exist_ok=True)

    gt_file = train_dir / "labels.txt"

    success_count = 0
    with open(dataset_csv, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)

        with open(gt_file, 'w', encoding='utf-8') as gt:
            for row in reader:
                img_path = Path(row["image_path"])
                label = row["label"]

                if img_path.exists():
                    try:
                        # Copy image to train_data folder
                        dest_path = train_dir / img_path.name
                        shutil.copy2(img_path, dest_path)

                        # EasyOCR (deep-text-recognition-benchmark) format: filename<tab>label
                        gt.write(f"{img_path.name}\t{label}\n")
                        success_count += 1
                    except Exception as e:
                        print(f"Error processing {img_path.name}: {e}")
                else:
                    print(f"Warning: Image not found {img_path}")

    print(f"\nPrepared {success_count} images for fine-tuning in '{train_dir}'")
    print("\n[Next Steps for Fine-tuning EasyOCR]")
    print("EasyOCR uses the Clova AI 'deep-text-recognition-benchmark' architecture.")
    print("To actually train the model, you need to clone their repository and run it:")
    print("1. git clone https://github.com/clovaai/deep-text-recognition-benchmark")
    print("2. cd deep-text-recognition-benchmark")
    print("3. python train.py --train_data ../python_service/train_data --valid_data ../python_service/train_data --select_data / --batch_ratio 1.0 --Transformation TPS --FeatureExtraction ResNet --SequenceModeling BiLSTM --Prediction CTC")
    print("\nNote: For now, the combination of EasyOCR's pre-trained Korean model")
    print("plus our Fuzzy Matching algorithm (thefuzz) in ocr_model.py")
    print("will provide excellent accuracy without needing immediate fine-tuning.")
    print("==========================================================")

if __name__ == "__main__":
    main()
