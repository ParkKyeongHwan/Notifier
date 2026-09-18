@echo off
chcp 65001 >nul
echo ==========================================================
echo EasyOCR (deep-text-recognition-benchmark) 학습 원클릭 시스템
echo ==========================================================
echo.
echo [1단계] 바탕화면의 Success 폴더에서 데이터를 가져와 분할하고 LMDB를 생성합니다.
echo [2단계] 자동으로 학습(train.py)을 시작합니다.
echo.

REM 가상환경(venv) 활성화
if exist "%~dp0venv\Scripts\activate.bat" (
    call "%~dp0venv\Scripts\activate.bat"
) else (
    echo [Info] venv 가상환경을 찾지 못했습니다. 시스템 파이썬을 사용합니다.
)

REM 패키지 설치 확인 및 설치 진행
echo [Info] 필요한 패키지를 설치합니다...
pip install -r "%~dp0requirements.txt"
pip install fire lmdb

REM 1. 데이터 준비 (auto_prepare_lmdb.py)
python "%~dp0auto_prepare_lmdb.py"
if %ERRORLEVEL% neq 0 (
    echo.
    echo [오류] 데이터 준비 중 오류가 발생했습니다. 학습을 진행하지 않습니다.
    pause
    exit /b %ERRORLEVEL%
)

REM 2. 학습 시작 (start_training.py)
python "%~dp0start_training.py"

pause
