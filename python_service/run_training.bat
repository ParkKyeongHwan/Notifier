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
echo ※ 만약 out of memory (메모리 부족) 에러가 발생하면 start_training.py 안의 --batch_size 를 줄이세요.
echo.

REM 가상환경(venv) 활성화
if exist "%~dp0venv\Scripts\activate.bat" (
    call "%~dp0venv\Scripts\activate.bat"
) else (
    echo [Info] venv 가상환경을 찾지 못했습니다. 시스템 파이썬을 사용합니다.
)

REM 패키지 설치 확인 및 설치 진행
echo [Info] requirements.txt 패키지 설치 확인 및 설치 진행...
pip install -r "%~dp0requirements.txt"

REM 파이썬 스크립트 실행
python "%~dp0start_training.py"

pause
