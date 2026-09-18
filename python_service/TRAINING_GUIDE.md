# EasyOCR 커스텀 데이터셋 학습(Fine-tuning) 가이드

바탕화면의 `Success` 폴더에 모인 이미지들을 바탕으로 자동으로 데이터셋을 만들고 AI 모델을 학습할 수 있습니다!

## 1. 사전 준비 사항 (중요!)

학습을 위해서는 EasyOCR 원천 기술을 만든 **Clova AI의 학습 레포지토리**가 필요합니다.
만약 아직 받지 않으셨다면 `Notifier` 폴더와 같은 위치에 학습용 코드를 다운로드 받아주세요.

```cmd
# Notifier 폴더의 바깥쪽(Project 폴더 등)에서 실행하세요.
git clone https://github.com/clovaai/deep-text-recognition-benchmark
```

또한 바탕화면에 `Success` 폴더가 있고, 학습할 이미지들이 들어있어야 합니다. 이미지 이름은 `[어떤내용]_[라벨].png` 형태로 되어 있어야 합니다. (예: `screenshot_계곡길 개미굴 습격전.png`)

## 2. 1-Click 원클릭 학습 실행하기

복잡한 명령어들을 직접 칠 필요 없이 **원클릭 배치 파일**을 준비했습니다.
`Notifier\python_service` 폴더 안에 있는 `1_prepare_and_train.bat`을 더블클릭하여 실행하세요.

```cmd
# Notifier\python_service 폴더 안에서 실행
1_prepare_and_train.bat
```

이 배치 파일은 다음을 자동으로 수행합니다:
1. 바탕화면 `Success` 폴더의 이미지를 읽어옵니다.
2. 90%는 학습용(`train`), 10%는 검증용(`valid`)으로 자동 분할합니다.
3. LMDB 형식의 데이터셋을 자동으로 생성합니다. (`data_lmdb/train`, `data_lmdb/valid`)
4. 즉시 EasyOCR Fine-tuning 학습을 시작합니다!

*(주의: 배치 파일을 다른 폴더로 옮기면 경로가 꼬여서 에러가 날 수 있으니 절대 옮기지 마세요!)*

## 3. 학습이 잘 진행되고 있는지 어떻게 알 수 있나요?

명령어가 실행되면 터미널 창에 실시간으로 로그가 주르륵 올라갑니다. 다음 세 가지를 확인하시면 됩니다:

1. **Loss 감소**:
   출력되는 로그 중에 `loss: 2.345` 와 같은 수치가 보일 것입니다. 이 수치(에러율)가 점진적으로 작아진다면(예: 2.3 -> 1.5 -> 0.8) 모델이 제대로 학습하고 있다는 뜻입니다.
2. **정확도(Accuracy) 출력**:
   기본 설정상 500번 학습할 때마다 한 번씩 검증(Validation)을 수행합니다. 이때 `Current_accuracy: 85.5%` 와 같이 현재 성능이 출력됩니다.
3. **가중치(모델) 저장**:
   최고 정확도를 갱신할 때마다 `saved_models/` 폴더 내부에 `best_accuracy.pth` 파일이 갱신되어 저장됩니다. 이 파일이 갱신되고 있다면 아주 잘 되고 있는 것입니다.

## 4. 트러블슈팅

* **CUDA Out of Memory 에러가 날 경우**: 사용 중인 그래픽카드의 램(VRAM)이 부족해서 생기는 현상입니다. `start_training.py` 파일을 열고 명령어 뒷부분에 있는 `--batch_size 32` 숫자를 `16`이나 `8`로 줄여서 다시 실행해 보세요.
* **moduleNotFoundError가 뜰 경우**: 학습에 필요한 패키지가 부족한 것입니다. `fire`, `lmdb` 등을 설치해 주시면 됩니다. (배치 파일이 자동으로 설치를 시도합니다.)
