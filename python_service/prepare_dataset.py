import os
import glob
import re
import csv
from pathlib import Path

def prepare_dataset(success_folder, output_csv="dataset.csv"):
    """
    Success 폴더 내의 이미지 파일명에서 라벨(습격전 이름)을 추출하여
    학습용 데이터셋 메타데이터(CSV)를 생성합니다.
    파일명 예시: 20260913_003745_계곡길 개미굴 습격전 (상위 100명).png
    """
    success_dir = Path(success_folder)
    if not success_dir.exists():
        print(f"Error: {success_folder} does not exist.")
        return

    # 지원하는 이미지 확장자
    image_extensions = ('*.png', '*.jpg', '*.jpeg')
    image_files = []
    for ext in image_extensions:
        image_files.extend(success_dir.glob(ext))

    dataset = []

    # 파일명에서 정답 추출을 위한 정규표현식
    # 예: 20260913_003745_계곡길 개미굴 습격전 (상위 100명).png
    # -> 계곡길 개미굴 습격전 (상위 100명)

    for img_path in image_files:
        filename = img_path.name
        # 확장자 제거
        name_without_ext = img_path.stem

        # 마지막 '_' 이후의 텍스트 추출
        parts = name_without_ext.split('_')
        if len(parts) >= 2:
            label = parts[-1].strip()

            # 절대 경로나 상대 경로 저장 (여기서는 스크립트 실행 위치 기준 상대 경로 또는 절대 경로)
            dataset.append({
                "image_path": str(img_path.resolve()),
                "label": label
            })
        else:
            print(f"Warning: Could not parse label from {filename}")

    # CSV로 저장
    with open(output_csv, 'w', newline='', encoding='utf-8') as f:
        writer = csv.DictWriter(f, fieldnames=["image_path", "label"])
        writer.writeheader()
        writer.writerows(dataset)

    print(f"Successfully processed {len(dataset)} images.")
    print(f"Dataset mapping saved to {output_csv}")

if __name__ == "__main__":
    # C# 프로젝트의 Success 폴더 경로를 지정 (기본값 설정)
    # 실제 환경에 맞게 경로 조정 필요
    base_dir = Path(__file__).parent.parent
    success_folder_path = base_dir / "bin" / "Release" / "net9.0-windows" / "Success"

    # 기본 경로에 없으면 현재 디렉토리 상위의 Success를 찾음
    if not success_folder_path.exists():
        success_folder_path = base_dir / "Success"

    output_csv_path = Path(__file__).parent / "dataset.csv"

    print(f"Looking for images in: {success_folder_path}")
    prepare_dataset(success_folder_path, output_csv_path)
