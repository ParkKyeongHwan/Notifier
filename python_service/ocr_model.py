import os
import csv
from pathlib import Path

class OCRModelStub:
    def __init__(self, dataset_csv="dataset.csv"):
        self.known_labels = set()
        self.load_known_labels(dataset_csv)

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
            # C# 코드(OcrCorrector.cs)에서 직접 습격전 전체 리스트를 파싱하여 불러옵니다.
            ocr_corrector_path = Path(__file__).parent.parent / "OcrCorrector.cs"
            if ocr_corrector_path.exists():
                with open(ocr_corrector_path, 'r', encoding='utf-8-sig') as cs_file:
                    cs_content = cs_file.read()
                    # 정규식을 통해 "XXX 습격전 (상위 N명)" 형태를 모두 추출합니다.
                    import re
                    matches = re.findall(r'"([^"]+ 습격전 \([^)]+\))"', cs_content)
                    if matches:
                        self.known_labels.update(matches)
                        print(f"Loaded {len(matches)} known labels directly from OcrCorrector.cs")
            else:
                print("Could not find OcrCorrector.cs to load default labels.")


    def predict(self, image_bytes: bytes):
        """
        실제 파인튜닝된 OCR 모델이 텍스트를 추출하고 교정하는 로직이 들어갈 자리입니다.
        현재는 Stub 형태로, 성공적인 응답의 예시 형식을 반환합니다.
        """
        # TODO: PIL이나 OpenCV로 image_bytes 디코딩
        # TODO: OCR 모델 인퍼런스
        # TODO: 텍스트 매칭 및 교정 알고리즘 적용

        # 임시 모의 로직:
        # 실제로는 OCR이 읽은 텍스트(예: '계곡길 개미굴 습격전')와
        # self.known_labels 중 가장 유사한 것을 반환합니다.

        # [버그 픽스]: 더미 로직이 무조건 0.95 확률로 첫 번째 known label을 반환하면,
        # C# 쪽(Recognizer.cs)에서 이 값을 맹신하여 로컬 방어 로직(어썰트 차단 등)을 모두 스킵하는 문제가 발생합니다.
        # 실제 모델이 연동되기 전까지는 낮은 신뢰도(0.0)로 반환하여,
        # C# 쪽의 OcrCorrector.cs 방어 로직이 정상적으로 실행되도록 유도합니다.

        return {
            "result": "",
            "confidence": 0.0,
            "message": "Stub OCR prediction - Waiting for real model integration"
        }
