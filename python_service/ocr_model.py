import os
import csv
import io
import re
from pathlib import Path
from PIL import Image
import numpy as np

# We import easyocr inside the class so it doesn't crash on import if not installed
# but we need it for the model.
try:
    import easyocr
except ImportError:
    easyocr = None

from thefuzz import fuzz
from thefuzz import process

class OCRModelStub:
    def __init__(self, dataset_csv="dataset.csv"):
        self.known_labels = set()
        self.load_known_labels(dataset_csv)
        self.reader = None

        if easyocr is not None:
            # Check for fine-tuned model
            import shutil
            import torch
            models_dir = Path(__file__).parent / "saved_models"
            best_model_path = models_dir / "best_accuracy.pth"
            custom_model_path = models_dir / "custom_ocr.pth"

            # Initialize EasyOCR for Korean and English normally first to get the default architecture
            print("Initializing EasyOCR Model (ko, en)... This might take a moment.")
            self.reader = easyocr.Reader(['ko', 'en'], gpu=False)

            # If a new best model exists, rename it so we don't apply it every boot, but we save it as our custom weights
            if best_model_path.exists():
                print(f"Found newly trained model: {best_model_path}. Applying...")
                try:
                    shutil.copy2(best_model_path, custom_model_path)
                    best_model_path.rename(models_dir / "best_accuracy_applied.pth")
                except Exception as e:
                    print(f"Warning: Failed to move trained model: {e}")

            # If our custom fine-tuned weights exist, dynamically load them into the pre-initialized model
            if custom_model_path.exists():
                print(f"Loading custom fine-tuned weights from {custom_model_path}...")
                try:
                    state_dict = torch.load(str(custom_model_path), map_location='cpu')
                    # Remove 'module.' prefix if the model was trained with DataParallel
                    new_state_dict = {}
                    for k, v in state_dict.items():
                        name = k[7:] if k.startswith('module.') else k
                        new_state_dict[name] = v

                    self.reader.recognizer.load_state_dict(new_state_dict)
                    print("Custom weights successfully applied!")
                except Exception as e:
                    print(f"Error loading custom weights, falling back to default: {e}")
        else:
            print("Warning: easyocr is not installed. Please pip install easyocr.")

    def load_known_labels(self, csv_path):
        if os.path.exists(csv_path):
            with open(csv_path, 'r', encoding='utf-8') as f:
                reader = csv.DictReader(f)
                for row in reader:
                    if "label" in row:
                        self.known_labels.add(row["label"])
            print(f"Loaded {len(self.known_labels)} known labels from {csv_path}")
        else:
            print(f"Warning: {csv_path} not found. Loading defaults from C# code.")
            ocr_corrector_path = Path(__file__).parent.parent / "OcrCorrector.cs"
            if ocr_corrector_path.exists():
                with open(ocr_corrector_path, 'r', encoding='utf-8-sig') as cs_file:
                    cs_content = cs_file.read()
                    matches = re.findall(r'"([^"]+ 습격전 \([^)]+\))"', cs_content)
                    if matches:
                        self.known_labels.update(matches)
                        print(f"Loaded {len(matches)} known labels directly from OcrCorrector.cs")
            else:
                print("Could not find OcrCorrector.cs to load default labels.")

        # Also load some ignored keywords to skip Assault
        self.ignored_keywords = [
            "어설트", "패러사이트", "침공", "거미의숲", "시공", "바벨탑",
            "불시착", "부산행", "나로도", "우주센터", "우주", "마추픽추",
            "마추", "픽추", "모선", "유령선", "보르네오섬", "보르", "피라미드",
            "피라", "대마도", "명성황후", "명성", "마피아저택", "마피아",
            "하수도", "경성역", "시티", "시티타워", "한강둔치", "트랜실바니아",
            "트랜실", "정글", "달기지", "연구단지", "국지전"
        ]

    def predict(self, image_bytes: bytes):
        if self.reader is None:
            return {
                "result": "미발견",
                "confidence": 0.0,
                "message": "EasyOCR not installed."
            }

        try:
            # 1. Decode Image
            image = Image.open(io.BytesIO(image_bytes)).convert('RGB')
            img_np = np.array(image)

            # 2. Run EasyOCR
            # readtext returns a list of tuples: (bounding_box, text, confidence)
            results = self.reader.readtext(img_np)

            # Combine all extracted text into one string without spaces
            raw_text = "".join([res[1] for res in results]).replace(" ", "").replace("\n", "").replace("\r", "")

            print(f"EasyOCR Extracted Raw Text: {raw_text}")

            if not raw_text:
                return {
                    "result": "미발견",
                    "confidence": 0.0,
                    "message": "No text detected."
                }

            # 3. Assault Check
            for ignored in self.ignored_keywords:
                if ignored in raw_text:
                    return {
                        "result": f"어설트 ({ignored})",
                        "confidence": 1.0,
                        "category": "Assault",
                        "message": "예외 키워드(어설트) 감지"
                    }

            # 4. Fuzzy Matching with known labels
            if self.known_labels:
                # We use token_sort_ratio for robust matching
                best_match, score = process.extractOne(raw_text, list(self.known_labels), scorer=fuzz.partial_ratio)

                print(f"Best match: {best_match} (Score: {score})")

                if score >= 75: # Threshold for considering it a success
                    return {
                        "result": best_match,
                        "confidence": score / 100.0,
                        "category": "Success",
                        "message": f"Fuzzy matching success (Score: {score})"
                    }
                else:
                    # Try to extract numbers (people count) as a fallback
                    match = re.search(r'\d+', raw_text)
                    if match:
                        num = match.group()
                        return {
                            "result": f"알 수 없는 습격전 (상위 {num}명)",
                            "confidence": score / 100.0,
                            "category": "Unknown",
                            "message": f"Low match score ({score}), but extracted people count ({num})."
                        }

            # Fallback
            if "습격" in raw_text or "습" in raw_text or "격" in raw_text:
                 return {
                    "result": "알 수 없는 습격전",
                    "confidence": 0.0,
                    "category": "Unknown",
                    "message": "Fallback: Found '습' or '격'."
                }

            return {
                "result": "미발견",
                "confidence": 0.0,
                "category": "None",
                "message": "매칭 실패"
            }

        except Exception as e:
            print(f"Error during OCR prediction: {e}")
            return {
                "result": "미발견",
                "confidence": 0.0,
                "category": "None",
                "message": str(e)
            }
