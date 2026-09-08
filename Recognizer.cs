using System.Drawing;
using Tesseract;

namespace Notifier
{
    public class Recognizer
    {
        // 설정 파일에서 읽어올 변수들
        private int relativeStartX = 10;
        private int relativeStartY = 102;
        private int relativeEndX = 500;
        private int relativeEndY = 128;

        // ★ 새로 추가된 색상 설정 변수 (기본값 R:18, G:27, B:45)
        private int targetR = 18;
        private int targetG = 27;
        private int targetB = 45;
        private int colorTolerance = 70; // 오차 범위도 설정에서 바꿀 수 있도록 추가

        private string tessdataPath = "";
        private string debugImagePath = "";

        public Recognizer(string configFileName = "config.txt")
        {
            LoadConfiguration(configFileName);
        }

        // 1. 화면 캡처 함수
        public Bitmap CaptureCoordinateArea(int startX, int startY, int width, int height)
        {
            try
            {
                Bitmap bmp = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(startX, startY, 0, 0, bmp.Size);
                }
                return bmp;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // 화면 잠금, 절전 모드 등으로 캡처를 할 수 없을 때 에러를 무시하고 null 반환
                return null;
            }
            catch (Exception ex)
            {
                // 기타 예상치 못한 에러 처리
                Console.WriteLine($"[캡처 에러] {ex.Message}");
                return null;
            }
        }

        public string ReadSpecificText(int windowBoundsX, int windowBoundsY)
        {
            int width = relativeEndX - relativeStartX;
            int height = relativeEndY - relativeStartY;
            int absoluteX = windowBoundsX + relativeStartX;
            int absoluteY = windowBoundsY + relativeStartY;

            using (Bitmap rawImage = CaptureCoordinateArea(absoluteX, absoluteY, width, height))
            {
                if (rawImage == null) return string.Empty;

                // 플랜 A 시도: 픽셀을 또렷하게 유지하는 방식 (NearestNeighbor)
                string resultA = ProcessOcrWithStrategy(rawImage, System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor);

                // 결과가 충분히 길고 유의미한지 확인 (예: 5글자 이상이면 성공으로 간주)
                if (!string.IsNullOrEmpty(resultA) && resultA.Length > 5)
                {
                    return resultA;
                }

                // 플랜 B 시도: 플랜 A가 실패했거나 글자가 다 깨졌다면, 예전 방식인 부드러운 확대(Bicubic)로 재시도
                Console.WriteLine("[OCR] 플랜 A 실패. 플랜 B(Bicubic)로 재시도합니다.");
                string resultB = ProcessOcrWithStrategy(rawImage, System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic);

                return resultB;
            }
        }

        // 중복되는 OCR 처리 로직을 별도의 메서드로 분리 (전략 패턴 적용)
        private string ProcessOcrWithStrategy(Bitmap rawImage, System.Drawing.Drawing2D.InterpolationMode mode)
        {
            int scale = 3;
            int padding = 40; // 사방에 40픽셀의 넉넉한 흰색 여백을 줍니다.

            // 1. 1배수에서 흑백 이진화
            using (Bitmap bwOriginal = new Bitmap(rawImage.Width, rawImage.Height))
            {
                for (int y = 0; y < rawImage.Height; y++)
                {
                    for (int x = 0; x < rawImage.Width; x++)
                    {
                        Color c = rawImage.GetPixel(x, y);

                        // ★ 수정됨: 설정 파일에서 불러온 Target RGB와 오차범위(Tolerance)를 적용
                        if (Math.Abs(c.R - targetR) < colorTolerance &&
                            Math.Abs(c.G - targetG) < colorTolerance &&
                            Math.Abs(c.B - targetB) < colorTolerance)
                        {
                            bwOriginal.SetPixel(x, y, Color.Black);
                        }
                        else
                        {
                            bwOriginal.SetPixel(x, y, Color.White);
                        }
                    }
                }

                // 2. 확대 및 여백(Padding) 추가
                int newWidth = (bwOriginal.Width * scale) + (padding * 2);
                int newHeight = (bwOriginal.Height * scale) + (padding * 2);

                using (Bitmap scaledBwImage = new Bitmap(newWidth, newHeight))
                {
                    using (Graphics g = Graphics.FromImage(scaledBwImage))
                    {
                        // 배경을 전부 흰색으로 칠해서 캔버스를 만듭니다.
                        g.Clear(Color.White);
                        g.InterpolationMode = mode;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                        // 여백만큼 띄운 위치(정중앙)에 확대된 글자를 그립니다.
                        g.DrawImage(bwOriginal, padding, padding, bwOriginal.Width * scale, bwOriginal.Height * scale);
                    }

                    // [디버그] 설정 파일에서 불러온 경로로 이미지 저장
                    try
                    {
                        string directory = Path.GetDirectoryName(debugImagePath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        scaledBwImage.Save(debugImagePath, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[디버그 이미지 저장 실패] {ex.Message}");
                    }

                    // 3. OCR 인식
                    if (string.IsNullOrWhiteSpace(tessdataPath))
                    {
                        tessdataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "tessdata");
                    }

                    Environment.SetEnvironmentVariable("TESSDATA_PREFIX", tessdataPath);

                    using (var engine = new TesseractEngine(tessdataPath, "kor", EngineMode.Default))
                    {
                        engine.SetVariable("tessedit_pageseg_mode", "13");
                        engine.SetVariable("preserve_interword_spaces", "1");

                        using (MemoryStream ms = new MemoryStream())
                        {
                            scaledBwImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            using (Pix pixImage = Pix.LoadFromMemory(ms.ToArray()))
                            using (var page = engine.Process(pixImage))
                            {
                                return page.GetText().Trim().Replace(" ", "");
                            }
                        }
                    }
                }
            }
        }

        private void LoadConfiguration(string fileName)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string configFilePath = Path.Combine(desktopPath, fileName);

            // 설정 파일이 없으면 기본값으로 생성합니다.
            if (!File.Exists(configFilePath))
            {
                CreateDefaultConfig(configFilePath);
                Console.WriteLine($"[알림] 바탕화면에 설정 파일({fileName})이 생성되었습니다.");
            }

            // 텍스트 파일 읽기 (Key=Value 형식)
            string[] lines = File.ReadAllLines(configFilePath);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                string[] parts = line.Split('=');
                if (parts.Length >= 2)
                {
                    string key = parts[0].Trim();
                    string value = string.Join("=", parts[1..]).Trim();

                    switch (key)
                    {
                        case "StartX": int.TryParse(value, out relativeStartX); break;
                        case "StartY": int.TryParse(value, out relativeStartY); break;
                        case "EndX": int.TryParse(value, out relativeEndX); break;
                        case "EndY": int.TryParse(value, out relativeEndY); break;

                        // ★ 새로 추가된 키 처리
                        case "TargetR": int.TryParse(value, out targetR); break;
                        case "TargetG": int.TryParse(value, out targetG); break;
                        case "TargetB": int.TryParse(value, out targetB); break;
                        case "Tolerance": int.TryParse(value, out colorTolerance); break;

                        case "TessDataPath": tessdataPath = value; break;
                        case "DebugImagePath": debugImagePath = value; break;
                    }
                }
            }

            if (string.IsNullOrEmpty(tessdataPath))
            {
                tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            }

            if (string.IsNullOrEmpty(debugImagePath))
            {
                debugImagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TargetTextDebug_Padded.png");
            }
        }

        private void CreateDefaultConfig(string path)
        {
            // ★ 수정됨: 기본 설정 파일 생성 시 RGB와 Tolerance 값 추가
            string defaultContent = @"# 이터널시티 캡처 좌표 설정
StartX=10
StartY=102
EndX=500
EndY=128

# 텍스트 인식 색상 (RGB) 및 오차 범위
TargetR=18
TargetG=27
TargetB=45
Tolerance=70

# Tesseract(OCR) 언어 데이터 폴더 경로 (비워두면 실행 파일 폴더 내 tessdata 사용)
TessDataPath=

# 디버그 이미지 저장 경로 (예: C:\MyFolder\debug.png / 비워두면 바탕화면 저장)
DebugImagePath=
";
            File.WriteAllText(path, defaultContent);
        }
    }
}