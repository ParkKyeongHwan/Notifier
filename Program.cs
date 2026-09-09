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

// 일반 습격전 중복 알림 방지를 위한 HashSet
HashSet<string> todayAnnouncedRaids = new HashSet<string>();

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
            "2. 24시간 동작"
        }));

bool run24Hours = modeSelection == "2. 24시간 동작";
AnsiConsole.MarkupLine($"선택된 모드: [green]{modeSelection}[/]");

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

        // 대기 모드 진입 시(즉, 하루의 습격전 사이클이 끝났을 때) HashSet 초기화
        if (todayAnnouncedRaids.Count > 0)
        {
            Console.WriteLine($"[{now:HH:mm:ss}] 습격전 사이클 종료. 기록된 일반 습격전 내역을 초기화합니다.");
            todayAnnouncedRaids.Clear();
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

        Console.WriteLine(extractedText);

        if (category != "None" && lastMessage != extractedText)
        {
            if (category == "Unknown")
            {
                Match match = Regex.Match(rawText, @"상위\s*(\d+)");
                if (match.Success)
                {
                    string playerNum = match.Groups[1].Value; // 추출된 숫자
                    extractedText = $"알 수 없는 습격전 (상위 {playerNum}명)";
                }

                if ((DateTime.Now - lastUnknownRaidTime).TotalMinutes < 5)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 이미 5분 이내에 알 수 없는 습격전을 알렸습니다. (알람 스킵)");

                    // 주의: 스킵하더라도 화면에 텍스트가 떠있는 동안 콘솔이 도배되는 것을 막기 위해 똑같이 2분간 재웁니다.
                    if (capturedImage != null) capturedImage.Dispose();
                    await Task.Delay(TimeSpan.FromMinutes(2));
                    continue;
                }

                // 30분이 지났다면 마지막 발생 시간을 현재로 갱신
                lastUnknownRaidTime = DateTime.Now;
            }
            else // "알 수 없는 습격전"이 아닌 일반 습격전이거나 어설트일 경우
            {
                // HashSet에 이미 등록되어 있는지 확인
                if (todayAnnouncedRaids.Contains(extractedText))
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 오늘 이미 알림이 전송된 습격전/어설트입니다: {extractedText} (알람 스킵)");
                    if (capturedImage != null) capturedImage.Dispose();
                    await Task.Delay(1000);
                    continue;
                }

                // 처음 발생한 습격전/어설트이면 HashSet에 추가
                todayAnnouncedRaids.Add(extractedText);
            }

            lastMessage = extractedText;
            lastMessageTime = DateTime.Now;

            // 이미지 저장 로직 추가
            if (capturedImage != null)
            {
                try
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string targetFolder = Path.Combine(desktopPath, category);

                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);
                    }

                    // 파일명에 사용할 수 없는 문자 제거
                    string safeFileName = string.Join("_", extractedText.Split(Path.GetInvalidFileNameChars()));
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string filePath = Path.Combine(targetFolder, $"{timestamp}_{safeFileName}.png");

                    capturedImage.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 이미지 저장 완료: {filePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 이미지 저장 실패: {ex.Message}");
                }
            }

            Console.WriteLine("잠시 후에 " + extractedText + "(이)가 시작됩니다.");
            // 디스코드로 보내지 않고 콘솔에만 출력
            // await DiscordNotifier.SendMessageAsync("잠시 후에 " + extractedText + "(이)가 시작됩니다.");

            //Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 알람 발송 완료! 2분 동안 OCR 감지를 일시 중지합니다.");
            if (capturedImage != null) capturedImage.Dispose();
            //await Task.Delay(TimeSpan.FromMinutes(2));
            continue;
        }

        if (capturedImage != null) capturedImage.Dispose();
    }

    // 동작 시간 중에는 2초마다 캡처를 수행합니다. (1초보다 전력 소모가 적고 안정적입니다)
    await Task.Delay(1000);
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