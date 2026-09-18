import os
import glob
import csv
import shutil
import random
import subprocess
import sys
from pathlib import Path

def main():
    print("==========================================================")
    print(" 자동 데이터셋 분할 및 LMDB 생성 스크립트")
    print("==========================================================")

    success_folder = Path.home() / "OneDrive" / "Desktop" / "Success"
    if not success_folder.exists() or not success_folder.is_dir():
        # Fallback to local Desktop just in case
        success_folder = Path.home() / "Desktop" / "Success"

    if not success_folder.exists() or not success_folder.is_dir():
        print(f"오류: '{success_folder}' 폴더를 찾을 수 없습니다.")
        print("바탕화면에 'Success' 폴더가 있고 학습할 이미지가 들어있는지 확인해주세요.")
        return

    # Gather images
    image_extensions = ('*.png', '*.jpg', '*.jpeg')
    image_files = []
    for ext in image_extensions:
        image_files.extend(success_folder.glob(ext))

    if not image_files:
        print(f"오류: '{success_folder}' 폴더 내에 이미지 파일이 없습니다.")
        return

    print(f"[Info] 총 {len(image_files)}개의 이미지를 찾았습니다.")

    # Shuffle and split 90% train, 10% valid (minimum 1 valid)
    random.seed(42)
    random.shuffle(image_files)

    num_valid = max(1, int(len(image_files) * 0.1))
    valid_files = image_files[:num_valid]
    train_files = image_files[num_valid:]

    print(f"[Info] Train 세트: {len(train_files)}개, Valid 세트: {len(valid_files)}개")

    service_dir = Path(__file__).parent.resolve()

    # Setup directories
    train_data_dir = service_dir / "train_data"
    data_lmdb_dir = service_dir / "data_lmdb"

    # Clean up existing train_data and data_lmdb directories
    if train_data_dir.exists():
        shutil.rmtree(train_data_dir)
    if data_lmdb_dir.exists():
        shutil.rmtree(data_lmdb_dir)

    train_raw_dir = train_data_dir / "train"
    valid_raw_dir = train_data_dir / "valid"

    train_lmdb_dir = data_lmdb_dir / "train"
    valid_lmdb_dir = data_lmdb_dir / "valid"

    for d in [train_raw_dir, valid_raw_dir, train_lmdb_dir, valid_lmdb_dir]:
        d.mkdir(parents=True, exist_ok=True)

    def process_split(split_name, files, raw_dir):
        labels_txt_path = raw_dir / "labels.txt"
        success_count = 0
        with open(labels_txt_path, 'w', encoding='utf-8') as f:
            for img_path in files:
                filename = img_path.name
                name_without_ext = img_path.stem
                parts = name_without_ext.split('_')
                if len(parts) >= 2:
                    label = parts[-1].strip()
                    dest_path = raw_dir / filename
                    shutil.copy2(img_path, dest_path)
                    f.write(f"{filename}\t{label}\n")
                    success_count += 1
                else:
                    print(f"경고: 파일 이름에서 라벨을 추출할 수 없습니다 - {filename}")
        print(f"[Info] {split_name} 데이터 복사 및 라벨 생성 완료: {success_count}개")
        return labels_txt_path

    train_labels = process_split("Train", train_files, train_raw_dir)
    valid_labels = process_split("Valid", valid_files, valid_raw_dir)

    # Locate deep-text-recognition-benchmark
    dtrb_path = None
    for i in range(4):
        possible_path = service_dir.parents[i-1] if i > 0 else service_dir
        if (possible_path / "deep-text-recognition-benchmark" / "create_lmdb_dataset.py").exists():
            dtrb_path = possible_path / "deep-text-recognition-benchmark"
            break

    if not dtrb_path:
        print("\n[오류] deep-text-recognition-benchmark 폴더를 찾을 수 없습니다.")
        print("git clone https://github.com/clovaai/deep-text-recognition-benchmark 을 실행하여 다운로드 해주세요.")
        return

    create_lmdb_script = dtrb_path / "create_lmdb_dataset.py"

    def create_lmdb(split_name, raw_dir, lmdb_dir, labels_file):
        print(f"\n[Info] {split_name} LMDB 데이터 생성 중...")
        cmd = [
            sys.executable, str(create_lmdb_script),
            "--inputPath", str(raw_dir),
            "--gtFile", str(labels_file),
            "--outputPath", str(lmdb_dir)
        ]
        try:
            subprocess.run(cmd, check=True)
            print(f"[Info] {split_name} LMDB 생성 완료!")
        except subprocess.CalledProcessError as e:
            print(f"[오류] {split_name} LMDB 생성 중 문제가 발생했습니다: {e}")
            sys.exit(1)

    create_lmdb("Train", train_raw_dir, train_lmdb_dir, train_labels)
    create_lmdb("Valid", valid_raw_dir, valid_lmdb_dir, valid_labels)

    print("\n==========================================================")
    print(" 데이터 준비가 모두 완료되었습니다!")
    print(" 이제 학습을 시작할 수 있습니다.")
    print("==========================================================")

if __name__ == "__main__":
    main()
