using System.Runtime.InteropServices;

namespace Notifier
{
    public class WindowsMouseDriver
    {
        private readonly int _screenWidth = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
        private readonly int _screenHeight = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
        private readonly Random _rnd = new Random();

        public void MoveAbsolute(int x, int y)
        {
            var input = CreateMouseInput(x, y, NativeMethods.MOUSEEVENTF_MOVE | NativeMethods.MOUSEEVENTF_ABSOLUTE);
            NativeMethods.SendInput(1, ref input, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }

        public void LeftClick()
        {
            // 좌클릭 상수(LEFTDOWN, LEFTUP) 사용
            var downInput = CreateMouseInput(0, 0, NativeMethods.MOUSEEVENTF_LEFTDOWN);
            var upInput = CreateMouseInput(0, 0, NativeMethods.MOUSEEVENTF_LEFTUP);

            // 좌클릭 누르기
            NativeMethods.SendInput(1, ref downInput, Marshal.SizeOf(typeof(NativeMethods.INPUT)));

            // 사람처럼 짧게 누르고 있는 시간 (30ms ~ 60ms)
            Thread.Sleep(_rnd.Next(30, 60));

            // 좌클릭 떼기
            NativeMethods.SendInput(1, ref upInput, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }
        public void RightClick()
        {
            var downInput = CreateMouseInput(0, 0, NativeMethods.MOUSEEVENTF_RIGHTDOWN);
            var upInput = CreateMouseInput(0, 0, NativeMethods.MOUSEEVENTF_RIGHTUP);

            NativeMethods.SendInput(1, ref downInput, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
            Thread.Sleep(_rnd.Next(30, 60));
            NativeMethods.SendInput(1, ref upInput, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }
        public void RightClickAndHold(int durationMs = 4000)
        {
            var downInput = CreateMouseInput(0, 0, NativeMethods.MOUSEEVENTF_RIGHTDOWN);
            var upInput = CreateMouseInput(0, 0, NativeMethods.MOUSEEVENTF_RIGHTUP);

            // 우클릭 누르기
            NativeMethods.SendInput(1, ref downInput, Marshal.SizeOf(typeof(NativeMethods.INPUT)));

            // 지정된 시간(기본 5초) + 인간적인 미세 오차(-500ms ~ +500ms) 대기
            Thread.Sleep(durationMs + _rnd.Next(-300, 300));

            // 우클릭 떼기
            NativeMethods.SendInput(1, ref upInput, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }

        public (int X, int Y) GetCursorPosition()
        {
            NativeMethods.GetCursorPos(out NativeMethods.POINT point);
            return (point.X, point.Y);
        }

        // 특정 영역(사각형) 내에서 무작위 위치를 클릭합니다.
        public void ClickInsideArea(int x, int y, int width, int height)
        {
            int randomX = _rnd.Next(x, x + width);
            int randomY = _rnd.Next(y, y + height);

            MoveAbsolute(randomX, randomY);
            // 이동 후 게임이 인식할 찰나의 대기시간
            Thread.Sleep(_rnd.Next(30, 60));
            LeftClick();
        }

        // Left/Right/Top/Bottom 테두리 영역별 클릭 함수
        public void ClickLeftBorder() => ClickInsideArea(0, 0, 50, _screenHeight);
        public void ClickRightBorder() => ClickInsideArea(_screenWidth - 50, 0, 50, _screenHeight);
        public void ClickTopBorder() => ClickInsideArea(0, 0, _screenWidth, 50);
        public void ClickBottomBorder() => ClickInsideArea(0, _screenHeight - 50, _screenWidth, 50);

        private NativeMethods.INPUT CreateMouseInput(int x, int y, uint flags)
        {
            return new NativeMethods.INPUT
            {
                // type = 0 은 NativeMethods.INPUT_MOUSE 와 같습니다.
                type = NativeMethods.INPUT_MOUSE,

                // 구조체가 바뀌어서 u(InputUnion)를 통해서 mi에 접근해야 합니다.
                u = new NativeMethods.InputUnion
                {
                    mi = new NativeMethods.MOUSEINPUT
                    {
                        dx = (x * 65535) / _screenWidth,
                        dy = (y * 65535) / _screenHeight,
                        dwFlags = flags
                    }
                }
            };
        }
    }
}