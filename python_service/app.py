from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.responses import JSONResponse
import uvicorn
import io
import os
import csv
from pathlib import Path

app = FastAPI(title="Raid OCR API Service")

# 나중에 실제 OCR 모델을 불러오는 객체로 교체합니다.
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
            print(f"Warning: {csv_path} not found. Starting with empty known labels.")
            # 기본 정답 셋 몇 개 하드코딩
            self.known_labels.update([
                "계곡길 개미굴 습격전 (상위 100명)",
                "팔당댐 공원 습격전 (상위 20명)",
                "농업진흥구역 방벽 습격전 (상위 5명)"
            ])

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

        # 테스트를 위해 항상 알 수 없는 습격전 반환 확률을 낮추고,
        # 임의의(혹은 첫 번째) known label을 반환하는 척 합니다.
        if not self.known_labels:
            return {"result": "알 수 없는 습격전", "confidence": 0.0, "message": "No known labels"}

        dummy_result = list(self.known_labels)[0]
        return {
            "result": dummy_result,
            "confidence": 0.95, # 매우 높다고 가정
            "message": "Stub OCR prediction"
        }

ocr_model = None

@app.on_event("startup")
def load_model():
    global ocr_model
    csv_path = Path(__file__).parent / "dataset.csv"
    ocr_model = OCRModelStub(str(csv_path))
    print("OCR API Service Started")

@app.post("/recognize")
async def recognize_image(file: UploadFile = File(...)):
    if not file.filename.lower().endswith(('.png', '.jpg', '.jpeg')):
        raise HTTPException(status_code=400, detail="Invalid file type. Please upload a PNG or JPEG.")

    try:
        contents = await file.read()

        # 모델 예측
        prediction = ocr_model.predict(contents)

        return JSONResponse(content={
            "filename": file.filename,
            "recognized_mission": prediction["result"],
            "confidence": prediction["confidence"],
            "message": prediction["message"]
        })
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    uvicorn.run("app:app", host="0.0.0.0", port=8000, reload=True)
