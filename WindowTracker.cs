namespace Notifier
{
    internal class WindowTracker
    {
        public (int X, int Y, int Width, int Height) GetWindowBounds(string windowTitle)
        {
            // 1. Windows API를 통해 창의 고유 ID(Handle)를 찾음
            IntPtr hwnd = NativeMethods.FindWindow(null, windowTitle);

            if (hwnd == IntPtr.Zero)
            {
                throw new Exception($"[{windowTitle}] 프로그램 창을 찾을 수 없습니다. 켜져 있는지 확인하세요.");
            }

            // 2. 창의 좌표(Left, Top, Right, Bottom)를 가져옴
            NativeMethods.GetWindowRect(hwnd, out NativeMethods.RECT rect);

            // 3. 사용하기 편하도록 가로, 세로 길이를 계산해서 반환
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            return (rect.Left, rect.Top, width, height);
        }
    }
}