@echo off
chcp 65001 >nul
echo ==========================================================
echo EasyOCR (deep-text-recognition-benchmark) 학습 시작
echo ==========================================================
echo.
echo [학습이 잘 돌아가는지 확인하는 방법]
echo 1. 화면에 'loss' 값이 주기적으로 출력되며 점점 작아지는지 확인하세요.
echo 2. 주기적으로 검증(validation) 결과가 출력되며 정확도(accuracy)가 나오는지 확인하세요.
echo 3. 학습 과정에서 최고 정확도를 갱신하면 'saved_models' 폴더에 best_accuracy.pth 파일이 저장됩니다.
echo.
echo ※ 만약 out of memory (메모리 부족) 에러가 발생하면 명령어 끝에 있는 --batch_size 32 를 16이나 8로 줄이세요.
echo.
echo 3초 후 학습을 시작합니다...
timeout /t 3 >nul

REM 가상환경(venv) 활성화
call "%~dp0venv\Scripts\activate.bat"

REM 데이터셋 절대 경로 설정 (이 배치 파일이 있는 폴더 안의 data_lmdb)
set "DATA_PATH=%~dp0data_lmdb"

REM 형제 폴더인 deep-text-recognition-benchmark 로 이동
REM %~dp0 는 현재 배치파일이 위치한 폴더(Notifier\python_service\)입니다.
cd /d "%~dp0..\..\deep-text-recognition-benchmark"

REM 만약 폴더가 없으면 에러 메시지 출력 후 종료
if not exist "train.py" (
    echo.
    echo [오류] deep-text-recognition-benchmark 폴더를 찾을 수 없거나 train.py가 없습니다.
    echo 폴더 위치가 Notifier 폴더와 같은 위치에 있는지 확인해 주세요.
    pause
    exit /b 1
)

REM 학습 명령어
REM 만약 기존 EasyOCR 모델 파일(.pth)을 다운받으셨다면,
REM 아래 명령어 맨 끝에 --saved_model ./korean_g2.pth 와 같은 옵션을 추가하시면 더 빨리 학습(Fine-tuning)이 됩니다.
python train.py --train_data "%DATA_PATH%" --valid_data "%DATA_PATH%" --select_data / --batch_ratio 1.0 --Transformation TPS --FeatureExtraction ResNet --SequenceModeling BiLSTM --Prediction CTC --batch_size 32 --valInterval 500

pause