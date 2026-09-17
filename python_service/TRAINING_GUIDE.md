# EasyOCR 커스텀 데이터셋 학습(Fine-tuning) 가이드

데이터셋이 LMDB 형태로 성공적으로 변환되었습니다! 이제 실제 AI 모델을 학습시킬 준비가 끝났습니다.

## 1. 사전 준비 사항 (중요!)

이 학습 과정은 우리가 만든 Python 서비스가 아니라, EasyOCR의 원천 기술을 만든 **Clova AI의 학습 레포지토리**에서 진행해야 합니다.
만약 아직 받지 않으셨다면 터미널에서 다음 명령어를 실행하여 학습용 코드를 다운로드 받아주세요.

```cmd
git clone https://github.com/clovaai/deep-text-recognition-benchmark
cd deep-text-recognition-benchmark
```

## 2. 학습 실행하기

우리가 만든 배치 파일(`run_training.bat`)을 다운받은 `deep-text-recognition-benchmark` 폴더 안으로 복사하거나 이동시킨 뒤, 더블클릭하여 실행하세요. (또는 터미널에서 실행하셔도 됩니다.)

```cmd
:: 위에서 다운로드 받은 폴더 안으로 이동하여 실행
copy ..\python_service\run_training.bat .
run_training.bat
```

## 3. 학습이 잘 진행되고 있는지 어떻게 알 수 있나요?

명령어가 실행되면 터미널 창에 실시간으로 로그가 주르륵 올라갑니다. 다음 세 가지를 확인하시면 됩니다:

1. **Loss 감소**:
   출력되는 로그 중에 `loss: 2.345` 와 같은 수치가 보일 것입니다. 이 수치(에러율)가 점진적으로 작아진다면(예: 2.3 -> 1.5 -> 0.8) 모델이 제대로 학습하고 있다는 뜻입니다.
2. **정확도(Accuracy) 출력**:
   기본 설정상 500번 학습할 때마다 한 번씩 검증(Validation)을 수행합니다. 이때 `Current_accuracy: 85.5%` 와 같이 현재 성능이 출력됩니다.
3. **가중치(모델) 저장**:
   최고 정확도를 갱신할 때마다 `saved_models/` 폴더 내부에 `best_accuracy.pth` 파일이 갱신되어 저장됩니다. 이 파일이 갱신되고 있다면 아주 잘 되고 있는 것입니다.

## 4. 사전 학습 모델(Pre-trained weights)에 관하여

현재는 바닥부터(from scratch) 학습을 시작하도록 배치파일이 작성되어 있습니다.
하지만 이미 한글을 어느 정도 읽을 줄 아는 기존 EasyOCR 가중치를 불러와서 **우리의 데이터만 추가로 가르치면(Fine-tuning)** 훨씬 빠르고 정확하게 학습됩니다.

**기존 모델을 적용하는 방법:**
1. 기존에 사용하던 `.pth` 파일 (예: `korean_g2.pth` 또는 `custom_example.pth`)을 준비합니다.
2. `run_training.bat` 파일을 메모장 등으로 엽니다.
3. 맨 마지막 줄 `python train.py ...` 명령어 맨 끝에 띄어쓰기를 한 칸 하고 아래 내용을 덧붙여줍니다.
   ```
   --saved_model ./korean_g2.pth
   ```
   (경로는 실제 `.pth` 파일이 있는 위치를 적어주시면 됩니다.)

## 5. 트러블슈팅

* **CUDA Out of Memory 에러가 날 경우**: 사용 중인 그래픽카드의 램(VRAM)이 부족해서 생기는 현상입니다. `run_training.bat` 파일을 열고 명령어 뒷부분에 있는 `--batch_size 32` 숫자를 `16`이나 `8`로 줄여서 다시 실행해 보세요.
* **moduleNotFoundError가 뜰 경우**: 학습에 필요한 패키지가 부족한 것입니다. 터미널에서 `pip install lmdb pillow nltk natsort` 등을 실행하여 필요한 라이브러리를 설치해 주세요 (오류 메시지에 나오는 모듈을 설치하면 됩니다).
