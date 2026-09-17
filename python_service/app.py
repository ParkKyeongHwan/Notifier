from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.responses import JSONResponse
import uvicorn
import io
from pathlib import Path

from ocr_model import OCRModelStub

app = FastAPI(title="Raid OCR API Service")

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
            "recognized_mission": prediction.get("result", "미발견"),
            "confidence": prediction.get("confidence", 0.0),
            "category": prediction.get("category", "None"),
            "message": prediction.get("message", "")
        })
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    uvicorn.run("app:app", host="0.0.0.0", port=8000, reload=True)
