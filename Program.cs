using Notifier;
using System.Drawing;
using System.Text.RegularExpressions;
using Spectre.Console;

var recognizer = new Recognizer("config.txt");
var windowTracker = new WindowTracker();
string targetWindow = "이터널시티";

const uint SWP_NOSIZE = 0x0001;
const uint SWP_NOZORDER = 0x0004;
int positionX = 1200;
int positionY = 5;

string lastMessage = string.Empty;
DateTime lastMessageTime = DateTime.MinValue;
DateTime lastUnknownRaidTime = DateTime.MinValue;

// ★ 추가: 현재 절전 모드 상태인지 추적하는 변수 (콘솔 도배 방지용)
bool isSleeping = false;
bool isOnline = false;

IntPtr handle = NativeMethods.GetConsoleWindow();
if (handle != IntPtr.Zero)
{
    NativeMethods.SetWindowPos(handle, IntPtr.Zero, positionX, positionY, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
}

Console.SetWindowSize(80, 5);

// Spectre.Console을 이용한 동작 모드 선택
var modeSelection = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("동작 모드를 선택하세요:")
        .PageSize(10)
        .AddChoices(new[] {
            "1. 지정 시간 동작 (19:00 ~ 00:59)",
            "2. 24시간 동작",
            "3. FAIL / Unknown 폴더 이미지 재분석 및 정리"
        }));

bool run24Hours = modeSelection == "2. 24시간 동작";
AnsiConsole.MarkupLine($"선택된 모드: [green]{modeSelection}[/]");

if (modeSelection == "3. FAIL / Unknown 폴더 이미지 재분석 및 정리")
{
    RunFailFolderReanalysis(recognizer);
    Console.WriteLine("재분석이 완료되었습니다. 프로그램을 종료하려면 아무 키나 누르세요.");
    Console.ReadKey();
    return;
}

while (true)
{
    DateTime now = DateTime.Now;

    // 현재 시간이 동작 시간(19시~0시 59분)인지 체크 (24시간 모드면 항상 true)
    bool isActiveTime = run24Hours || (now.Hour >= 19 || now.Hour < 1);

    if (!isActiveTime)
    {
        // 1. 다음 동작 시간(오늘 오후 7시)을 계산
        DateTime nextRunTime = DateTime.Today.AddHours(19);

        // 2. 만약 현재 시간이 이미 오늘 오후 7시를 지났다면(예: 밤 1시~2시), 내일 오후 7시로 설정
        if (now > nextRunTime)
        {
            nextRunTime = nextRunTime.AddDays(1);
        }

        // 3. 다음 오후 7시까지 남은 시간 계산
        TimeSpan sleepDuration = nextRunTime - now;

        if (!isSleeping)
        {
            // TimeSpan에서 시, 분, 초만 각각 뽑아옵니다.
            int hours = sleepDuration.Hours;
            int minutes = sleepDuration.Minutes;
            int seconds = sleepDuration.Seconds;

            Console.WriteLine($"[{now:HH:mm:ss}] 대기 시간입니다. {hours}시간 {minutes}분 {seconds}초 후(오후 7시)에 알림 감지를 재개합니다.");
            //await DiscordNotifier.SendMessageAsync($"습격전 감지 프로그램 [{now:HH:mm:ss}] 대기 시간입니다. {hours}시간 {minutes}분 {seconds}초 후(오후 7시)에 알림 감지를 재개합니다.");
            isSleeping = true;
            isOnline = false;
        }

        // 4. 남은 시간만큼 한방에 통째로 대기
        await Task.Delay(sleepDuration);
        continue;
    }

    //if (!isOnline)
    //{
    //    await DiscordNotifier.SendMessageAsync("습격전 감지 프로그램이 동작합니다");
    //}

    isOnline = true;

    // --- 아래부터는 기존의 캡처 및 OCR 로직이 그대로 들어갑니다 ---

    if (!string.IsNullOrEmpty(lastMessage) && (DateTime.Now - lastMessageTime).TotalMinutes >= 2)
    {
        lastMessage = string.Empty;
    }

    var bounds = windowTracker.GetWindowBounds(targetWindow);

    if (bounds.X == 0 && bounds.Y == 0)
    {
        Console.WriteLine($"[{targetWindow}] 창을 찾을 수 없습니다. 게임이 켜져 있는지 확인하세요.");
    }
    else
    {
        Bitmap capturedImage;
        string rawText = recognizer.ReadSpecificText(bounds.X, bounds.Y, out capturedImage);
        Console.WriteLine(rawText);

        var extractionResult = OcrCorrector.ExtractMissionName(rawText);
        string extractedText = extractionResult.Name;
        string category = extractionResult.Category;
        // Reason is ignored in real-time capture

        Console.WriteLine(extractedText);

        if (category == "Unknown")
        {
            Match match = Regex.Match(rawText, @"상위\s*(\d+)");
            if (match.Success)
            {
                string playerNum = match.Groups[1].Value; // 추출된 숫자
                extractedText = $"알 수 없는 습격전 (상위 {playerNum}명)";
            }
        }
        else if (category == "None")
        {
            category = "FAIL";
            if (string.IsNullOrWhiteSpace(extractedText) || extractedText == "미발견")
            {
                extractedText = string.IsNullOrWhiteSpace(rawText) ? "미발견" : rawText.Trim();
            }
        }

        // 이미지 저장 로직
        if (capturedImage != null)
        {
            try
            {
                if (category != "Assault")
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string targetFolder = Path.Combine(desktopPath, category);

                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);
                    }

                    // 파일명에 사용할 수 없는 문자 제거 및 공백/개행 제거
                    string cleanName = extractedText.Replace("\r", "").Replace("\n", " ");
                    if (cleanName.Length > 50)
                    {
                        cleanName = cleanName.Substring(0, 50); // 너무 길면 자름
                    }
                    string safeFileName = string.Join("_", cleanName.Split(Path.GetInvalidFileNameChars()));
                    if (string.IsNullOrWhiteSpace(safeFileName))
                    {
                        safeFileName = "UnknownText";
                    }

                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string filePath = Path.Combine(targetFolder, $"{timestamp}_{safeFileName}.png");

                    capturedImage.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 이미지 저장 완료: {filePath} (Category: {category})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 이미지 저장 실패: {ex.Message}");
            }
            finally
            {
                capturedImage.Dispose();
            }
        }
    }

    // 항상 10초 대기
    await Task.Delay(10000);
}


void RunFailFolderReanalysis(Recognizer rec)
{
    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    string failFolder = Path.Combine(desktopPath, "FAIL");
    string unknownFolder = Path.Combine(desktopPath, "Unknown");
    string tempFolder = Path.Combine(desktopPath, "Temp");
    string successFolder = Path.Combine(desktopPath, "Success");

    if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);
    if (!Directory.Exists(unknownFolder)) Directory.CreateDirectory(unknownFolder);

    string logFilePath = Path.Combine(desktopPath, $"Reanalysis_Log_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    try
    {
        // CSV 헤더 작성 (BOM 추가하여 엑셀 한글 깨짐 방지)
        File.WriteAllText(logFilePath, "\uFEFFFile Name,Raw Text,Extracted Name,Category,Reason\n", System.Text.Encoding.UTF8);
        Console.WriteLine($"[알림] 로그 파일이 생성되었습니다: {logFilePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[에러] 로그 파일을 생성할 수 없습니다: {ex.Message}");
    }

    // 0. Unknown 및 Temp 폴더 검사: "알 수 없는 습격전_"과 실제 미션 이름이 동시에 포함된 오분류 파일 복구
    string[] foldersToCheck = { unknownFolder, tempFolder };
    foreach (var folder in foldersToCheck)
    {
        if (Directory.Exists(folder))
        {
            Console.WriteLine($"{Path.GetFileName(folder)} 폴더에서 오분류된 '알 수 없는 습격전' 이미지를 찾아 Success 폴더로 이동합니다...");
            var files = Directory.GetFiles(folder, "*.png");
            int movedToSuccess = 0;

            if (!Directory.Exists(successFolder)) Directory.CreateDirectory(successFolder);

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                // "알 수 없는 습격전_" 과 또 다른 "습격전" 이름이 포함된 경우
                if (fileName.Contains("알 수 없는 습격전_") && fileName.LastIndexOf("습격전") > fileName.IndexOf("알 수 없는 습격전_"))
                {
                    string newFileName = fileName.Replace("알 수 없는 습격전_", "");
                    string targetPath = Path.Combine(successFolder, newFileName);
                    try
                    {
                        if (File.Exists(targetPath)) File.Delete(targetPath);
                        File.Move(file, targetPath);
                        movedToSuccess++;
                        Console.WriteLine($"  -> 복구됨: {fileName} -> {newFileName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  -> 복구 실패 ({fileName}): {ex.Message}");
                    }
                }
            }
            if (movedToSuccess > 0)
            {
                Console.WriteLine($"복구 완료: 총 {movedToSuccess}개의 파일을 {Path.GetFileName(folder)}에서 Success 폴더로 이동했습니다.\n");
            }
        }
    }

    // 1. Success 폴더 검사: "알 수 없는 습격전"이 포함된 파일들을 Unknown 폴더로 이동
    if (Directory.Exists(successFolder))
    {
        Console.WriteLine("Success 폴더에서 '알 수 없는 습격전' 이미지를 찾아 Unknown 폴더로 이동합니다...");
        var successFiles = Directory.GetFiles(successFolder, "*.png");
        int movedToUnknown = 0;
        foreach (var sFile in successFiles)
        {
            string fileName = Path.GetFileName(sFile);
            if (fileName.Contains("알 수 없는 습격전") || fileName.Contains("알수없는"))
            {
                string targetPath = Path.Combine(unknownFolder, fileName);
                try
                {
                    // 만약 대상 경로에 이미 파일이 있다면 덮어쓰거나 이름 변경 (여기서는 단순히 덮어씀)
                    if (File.Exists(targetPath)) File.Delete(targetPath);
                    File.Move(sFile, targetPath);
                    movedToUnknown++;
                    Console.WriteLine($"  -> 이동됨: {fileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  -> 이동 실패 ({fileName}): {ex.Message}");
                }
            }
        }
        Console.WriteLine($"정리 완료: 총 {movedToUnknown}개의 파일을 Unknown 폴더로 이동했습니다.\n");
    }

    // 2. FAIL 및 Unknown 폴더 합쳐서 재분석 준비
    List<string> filesToAnalyze = new List<string>();

    if (Directory.Exists(failFolder))
    {
        filesToAnalyze.AddRange(Directory.GetFiles(failFolder, "*.png"));
    }

    if (Directory.Exists(unknownFolder))
    {
        filesToAnalyze.AddRange(Directory.GetFiles(unknownFolder, "*.png"));
    }

    if (filesToAnalyze.Count == 0)
    {
        Console.WriteLine("FAIL 또는 Unknown 폴더에 분석할 이미지(.png)가 없습니다.");
        return;
    }

    Console.WriteLine($"총 {filesToAnalyze.Count}개의 이미지를 재분석합니다...\n");

    int successCount = 0;
    int deletedCount = 0;
    foreach (var file in filesToAnalyze)
    {
        string fileNameOnly = Path.GetFileName(file);
        Console.WriteLine($"[분석 중] {fileNameOnly}");
        string rawText = rec.ReadImageFile(file);

        if (string.IsNullOrWhiteSpace(rawText))
        {
            Console.WriteLine("  -> 텍스트를 읽을 수 없습니다.\n");
            continue;
        }

        Console.WriteLine($"  -> 추출 텍스트: {rawText}");
        // 로깅을 위해 원본 텍스트의 특수문자 제거
        string csvSafeRawText = string.IsNullOrWhiteSpace(rawText) ? "EMPTY" : rawText.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "");

        var result = OcrCorrector.ExtractMissionNameEnhanced(rawText);
        Console.WriteLine($"  -> 매칭 결과: {result.Name} (Category: {result.Category}, Reason: {result.Reason})");

        string csvSafeName = string.IsNullOrWhiteSpace(result.Name) ? "EMPTY" : result.Name.Replace("\"", "\"\"");
        string csvSafeReason = string.IsNullOrWhiteSpace(result.Reason) ? "NONE" : result.Reason.Replace("\"", "\"\"");

        try
        {
            File.AppendAllText(logFilePath, $"\"{fileNameOnly}\",\"{csvSafeRawText}\",\"{csvSafeName}\",\"{result.Category}\",\"{csvSafeReason}\"\n", System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  -> [에러] 로그 기록 실패: {ex.Message}");
        }

        string cleanName = result.Name.Replace("\r", "").Replace("\n", " ");
        if (cleanName.Length > 50)
        {
            cleanName = cleanName.Substring(0, 50);
        }
        string safeFileName = string.Join("_", cleanName.Split(Path.GetInvalidFileNameChars()));

        // 기존 파일명에서 맨 앞의 타임스탬프(예: 20240513_193818)만 추출 시도
        string oldFileName = Path.GetFileNameWithoutExtension(file);
        string timestamp = "";

        // 정규식을 사용하여 8자리_6자리 패턴을 찾음
        Match tsMatch = Regex.Match(oldFileName, @"^(\d{8}_\d{6})");
        if (tsMatch.Success)
        {
            timestamp = tsMatch.Groups[1].Value + "_";
        }
        else
        {
            // 타임스탬프 패턴이 없으면 현재 시간 사용
            timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_";
        }

        string newFileName = $"{timestamp}{safeFileName}.png";

        if (result.Category == "Success" && !result.Name.Contains("알 수 없는") && !result.Name.Contains("알수없는"))
        {
            string targetPath = Path.Combine(tempFolder, newFileName);
            try
            {
                if (File.Exists(targetPath)) File.Delete(targetPath);
                File.Move(file, targetPath);
                Console.WriteLine($"  -> [성공] Temp 폴더로 이동됨: {newFileName}\n");
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  -> [에러] 파일 이동 실패: {ex.Message}\n");
            }
        }
        else if (result.Category == "Unknown" || result.Name.Contains("알 수 없는") || result.Name.Contains("알수없는"))
        {
            string targetPath = Path.Combine(unknownFolder, newFileName);
            try
            {
                if (file != targetPath)
                {
                    if (File.Exists(targetPath)) File.Delete(targetPath);
                    File.Move(file, targetPath);
                    Console.WriteLine($"  -> [Unknown] Unknown 폴더로 이동됨: {newFileName}\n");
                }
                else
                {
                    Console.WriteLine("  -> [Unknown] 이미 Unknown 폴더에 있습니다.\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  -> [에러] 파일 이동 실패: {ex.Message}\n");
            }
        }
        else if (result.Category == "None")
        {
            try
            {
                File.Delete(file);
                deletedCount++;
                Console.WriteLine($"  -> [삭제] 가비지 데이터(None)로 판별되어 삭제되었습니다.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  -> [에러] 파일 삭제 실패: {ex.Message}\n");
            }
        }
        else
        {
            Console.WriteLine("  -> [실패] 매칭되지 않아 현재 폴더에 남겨둡니다.\n");
        }
    }

    Console.WriteLine($"\n재분석 완료: 총 {filesToAnalyze.Count}개 중 {successCount}개 성공, {deletedCount}개 삭제.");
}


void Spoide(string targetWindow, Recognizer recognizer)
{
    while (true)
    {
        var window = new WindowTracker();
        var mouse = new WindowsMouseDriver();
        var pos = mouse.GetCursorPosition();
        var bounds = window.GetWindowBounds(targetWindow);

        // 핵심: 모니터 좌표에서 창의 시작 위치를 빼면 '창 내부에서의 진짜 위치'가 나옴!
        int relativeX = pos.X - bounds.X;
        int relativeY = pos.Y - bounds.Y;

        using (Bitmap bmp = recognizer.CaptureCoordinateArea(pos.X, pos.Y, 1, 1))
        {
            System.Drawing.Color c = bmp.GetPixel(0, 0);
            Console.WriteLine($"[상대좌표] 상대X: {relativeX}, 상대Y: {relativeY} | R:{c.R}, G:{c.G}, B:{c.B}");
        }
        Thread.Sleep(500);
    }
}