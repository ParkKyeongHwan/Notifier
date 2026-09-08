using System.Runtime.InteropServices;

namespace Notifier
{
    // 윈도우 OS의 네이티브 API 호출부만 따로 모아둔 클래스
    // internal static으로 선언하여 외부에서 쉽게 가져다 쓸 수 있게 합니다.
    internal static class NativeMethods
    {
        public enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        public delegate bool HandlerRoutine(CtrlType CtrlType);

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        [DllImport("user32.dll")]
        internal static extern uint MapVirtualKey(uint uCode, uint uMapType);

        internal const uint MAPVK_VK_TO_VSC = 0x00;
        internal const uint KEYEVENTF_SCANCODE = 0x0008; // 하드웨어 스캔 코드 플래그
                                                         // Windows 시스템 상수 영역에 추가
        internal const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        internal const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

        // [기존 상수 및 구조체 유지, 아래 INPUT과 MOUSEINPUT 부분 교체]
        internal const uint INPUT_MOUSE = 0;
        internal const uint INPUT_KEYBOARD = 1;

        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // --------------------------------------------------------
        // 1. Windows API (user32.dll) 가져오기
        // --------------------------------------------------------
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        internal static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        internal static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        // --------------------------------------------------------
        // 2. Windows 시스템 상수
        // --------------------------------------------------------
        internal const uint MOUSEEVENTF_MOVE = 0x0001;
        internal const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        internal const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        internal const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        internal const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        internal const uint MOUSEEVENTF_LEFTUP = 0x0004;

        internal const int SM_CXSCREEN = 0;
        internal const int SM_CYSCREEN = 1;

        // --------------------------------------------------------
        // 3. C++ 메모리 레이아웃과 호환되는 구조체
        // --------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            public int dx; public int dy; public uint mouseData;
            public uint dwFlags; public uint time; public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        internal static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        internal const uint KEYEVENTF_KEYUP = 0x0002;

        // 1. 사용할 영문자 키코드 (대문자 기준)
        internal const byte VK_C = 0x43; // 'C' 키
        internal const byte VK_F = 0x46; // 'F' 키
        internal const byte VK_V = 0x56; // 'V' 키
        internal const byte VK_X = 0x58; // 'X' 키
        internal const byte VK_Z = 0x5A; // 'Z' 키

        // 2. 펑션키 (F1 ~ F12)
        internal const byte VK_F1 = 0x70;
        internal const byte VK_F2 = 0x71;
        internal const byte VK_F3 = 0x72;
        internal const byte VK_F4 = 0x73;
        internal const byte VK_F5 = 0x74;
        internal const byte VK_F6 = 0x75;
        internal const byte VK_F7 = 0x76;
        internal const byte VK_F8 = 0x77;
        internal const byte VK_F9 = 0x78;
        internal const byte VK_F10 = 0x79;
        internal const byte VK_F11 = 0x7A;
        internal const byte VK_F12 = 0x7B;

        // 기타 자주 쓰는 키
        internal const byte VK_SPACE = 0x20; // 스페이스바
        internal const byte VK_CAPITAL = 0x14; // Caps Lock 키

        // 일반 숫자 키 (키보드 상단 가로 배열)
        internal const byte VK_0 = 0x30;
        internal const byte VK_1 = 0x31;
        internal const byte VK_2 = 0x32;
        internal const byte VK_3 = 0x33;
        internal const byte VK_4 = 0x34;
        internal const byte VK_5 = 0x35;
        internal const byte VK_6 = 0x36;
        internal const byte VK_7 = 0x37;
        internal const byte VK_8 = 0x38;
        internal const byte VK_9 = 0x39;

        // 우측 숫자 패드 (Numpad)
        internal const byte VK_NUMPAD0 = 0x60;
        internal const byte VK_NUMPAD1 = 0x61;
        internal const byte VK_NUMPAD2 = 0x62;
        internal const byte VK_NUMPAD3 = 0x63;
        internal const byte VK_NUMPAD4 = 0x64;
        internal const byte VK_NUMPAD5 = 0x65;
        internal const byte VK_NUMPAD6 = 0x66;
        internal const byte VK_NUMPAD7 = 0x67;
        internal const byte VK_NUMPAD8 = 0x68;
        internal const byte VK_NUMPAD9 = 0x69;
    }
}
