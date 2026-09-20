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
        self.mission_dict = {}
        self.people_count_dict = {}

        ocr_corrector_path = Path(__file__).parent.parent / "OcrCorrector.cs"
        if ocr_corrector_path.exists():
            with open(ocr_corrector_path, 'r', encoding='utf-8-sig') as cs_file:
                cs_content = cs_file.read()

                # Extract MissionDictionary
                dict_matches = re.findall(r'\{\s*"([^"]+)",\s*"([^"]+ 습격전 \([^)]+\))"\s*\}', cs_content)
                for k, v in dict_matches:
                    self.mission_dict[k] = v
                    self.known_labels.add(v)

                # Extract MissionPeopleCount
                count_matches = re.findall(r'\{\s*"([^"]+ 습격전 \([^)]+\))",\s*(\d+)\s*\}', cs_content)
                for k, v in count_matches:
                    self.people_count_dict[k] = int(v)
                    self.known_labels.add(k)

                # Extract just labels if needed
                matches = re.findall(r'"([^"]+ 습격전 \([^)]+\))"', cs_content)
                if matches:
                    self.known_labels.update(matches)

            print(f"Loaded {len(self.mission_dict)} mission aliases and {len(self.people_count_dict)} people count mappings from OcrCorrector.cs")
        else:
            print("Could not find OcrCorrector.cs to load advanced labels.")

        if os.path.exists(csv_path):
            with open(csv_path, 'r', encoding='utf-8') as f:
                reader = csv.DictReader(f)
                for row in reader:
                    if "label" in row:
                        self.known_labels.add(row["label"])
            print(f"Loaded additional labels from {csv_path}, total unique labels: {len(self.known_labels)}")

        # Also load some ignored keywords to skip Assault
        self.ignored_keywords = [
            "어설트", "패러사이트", "침공", "거미의숲", "시공", "바벨탑",
            "불시착", "부산행", "나로도", "우주센터", "우주", "마추픽추",
            "마추", "픽추", "모선", "유령선", "보르네오섬", "보르", "피라미드",
            "피라", "대마도", "명성황후", "명성", "마피아저택", "마피아",
            "하수도", "경성역", "시티", "시티타워", "한강둔치", "트랜실바니아",
            "트랜실", "정글", "달기지", "연구단지", "국지전"
        ]

    def extract_mission_enhanced(self, raw_text):
        import Levenshtein
        clean_text = raw_text

        # 2. Number extraction
        extracted_count = -1
        valid_counts = [150, 100, 50, 30, 20, 15, 10, 5, 3]

        match = re.search(r'\d+', clean_text)
        if match:
            num = int(match.group())
            if num in valid_counts:
                extracted_count = num

        if extracted_count == -1:
            for count in valid_counts:
                if str(count) in clean_text:
                    if count == 50 and "150" in clean_text: continue
                    if count == 10 and "100" in clean_text: continue
                    if count == 15 and "150" in clean_text: continue
                    extracted_count = count
                    break

        if extracted_count != -1:
            candidate_missions = [k for k, v in self.people_count_dict.items() if v == extracted_count]

            best_match_name = ""
            best_error_count = 999

            for candidate in candidate_missions:
                keywords = [k for k, v in self.mission_dict.items() if v == candidate]
                for target_keyword in keywords:
                    allowed_errors = len(target_keyword) // 2

                    if len(clean_text) < len(target_keyword):
                        error_count = Levenshtein.distance(clean_text, target_keyword[:len(clean_text)])
                        if error_count <= allowed_errors and error_count < best_error_count:
                            best_error_count = error_count
                            best_match_name = candidate
                    else:
                        for i in range(len(clean_text) - len(target_keyword) + 1):
                            window = clean_text[i:i+len(target_keyword)]
                            error_count = Levenshtein.distance(window, target_keyword)
                            if error_count <= allowed_errors and error_count < best_error_count:
                                best_error_count = error_count
                                best_match_name = candidate

            if best_match_name:
                return {
                    "result": best_match_name,
                    "confidence": 1.0,
                    "category": "Success",
                    "message": f"인원수({extracted_count}명) 기반 매칭 성공"
                }
            else:
                return {
                    "result": f"알 수 없는 습격전 (상위 {extracted_count}명)",
                    "confidence": 0.0,
                    "category": "Unknown",
                    "message": f"인원수({extracted_count}명)만 감지됨. 이름 매칭 실패"
                }

        # 3. Keyword sliding window matching
        best_match_name = ""
        best_error_count = 999

        for target_keyword, full_name in self.mission_dict.items():
            if len(clean_text) < len(target_keyword):
                continue

            for i in range(len(clean_text) - len(target_keyword) + 1):
                window = clean_text[i:i+len(target_keyword)]
                error_count = Levenshtein.distance(window, target_keyword)

                allowed_errors = 0
                if len(target_keyword) >= 5: allowed_errors = 2
                elif len(target_keyword) >= 3: allowed_errors = 1

                if error_count <= allowed_errors:
                    if error_count == 0:
                        return {
                            "result": full_name,
                            "confidence": 1.0,
                            "category": "Success",
                            "message": "완벽 매칭"
                        }

                    if error_count < best_error_count:
                        best_error_count = error_count
                        best_match_name = full_name

        if best_match_name:
            if best_match_name == "알 수 없는 습격전":
                return {
                    "result": best_match_name,
                    "confidence": 0.0,
                    "category": "Unknown",
                    "message": f"오타 허용 범위({best_error_count}개) 내 알 수 없는 습격전 매칭"
                }
            return {
                "result": best_match_name,
                "confidence": 1.0,
                "category": "Success",
                "message": f"오타 허용 범위({best_error_count}개) 내 매칭"
            }

        return None

    def predict(self, image_bytes: bytes):
        if self.reader is None:
            return {
                "result": "미발견",
                "confidence": 0.0,
                "message": "EasyOCR not installed."
            }

        try:
            image = Image.open(io.BytesIO(image_bytes)).convert('RGB')
            img_np = np.array(image)

            results = self.reader.readtext(img_np)
            raw_text = "".join([res[1] for res in results]).replace(" ", "").replace("\n", "").replace("\r", "")

            print(f"EasyOCR Extracted Raw Text: {raw_text}")

            if not raw_text:
                return {
                    "result": "미발견",
                    "confidence": 0.0,
                    "message": "No text detected."
                }

            # 3. Assault Check (Enhanced logic using Levenshtein to prevent numbers being caught first)
            import Levenshtein
            best_assault_match = ""
            best_assault_len = -1

            for ignored in self.ignored_keywords:
                if len(raw_text) < len(ignored): continue
                for i in range(len(raw_text) - len(ignored) + 1):
                    window = raw_text[i:i+len(ignored)]
                    allowed_errors = 0
                    if len(ignored) >= 5: allowed_errors = 2
                    elif len(ignored) >= 3: allowed_errors = 1

                    if Levenshtein.distance(window, ignored) <= allowed_errors:
                        if len(ignored) > best_assault_len or (len(ignored) == best_assault_len and ignored not in ["어설트", "침공"]):
                            best_assault_match = ignored
                            best_assault_len = len(ignored)

            if best_assault_match:
                return {
                    "result": f"어설트 ({best_assault_match})",
                    "confidence": 1.0,
                    "category": "Assault",
                    "message": "예외 키워드(어설트) 감지"
                }

            # Try the enhanced C# matching logic
            enhanced_result = self.extract_mission_enhanced(raw_text)
            if enhanced_result:
                return enhanced_result

            # 4. Fallback to Fuzzy Matching for anything else
            if self.known_labels:
                best_match, score = process.extractOne(raw_text, list(self.known_labels), scorer=fuzz.partial_ratio)
                print(f"Best match: {best_match} (Score: {score})")

                if score >= 75:
                    return {
                        "result": best_match,
                        "confidence": score / 100.0,
                        "category": "Success",
                        "message": f"Fuzzy matching success (Score: {score})"
                    }

            # Fallback
            fallback_keyword = "습격"
            if len(raw_text) >= len(fallback_keyword):
                for i in range(len(raw_text) - len(fallback_keyword) + 1):
                    window = raw_text[i:i+len(fallback_keyword)]
                    if Levenshtein.distance(window, fallback_keyword) <= 1:
                        return {
                            "result": "알 수 없는 습격전",
                            "confidence": 0.0,
                            "category": "Unknown",
                            "message": "'습격' 키워드 단독 감지"
                        }

            if "습" in raw_text or "격" in raw_text:
                 return {
                    "result": "알 수 없는 습격전",
                    "confidence": 0.0,
                    "category": "Unknown",
                    "message": "'습' 또는 '격' 1글자 감지"
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
