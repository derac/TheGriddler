using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace WindowGridRedux
{
    public class GlobalHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        private LowLevelProc _mouseProc;
        private LowLevelProc _keyboardProc;
        private IntPtr _mouseHookId = IntPtr.Zero;
        private IntPtr _keyboardHookId = IntPtr.Zero;

        public event Action<System.Drawing.Point>? MouseMoved;
        public event Action<bool>? LeftButtonDown;
        public event Func<bool, bool>? RightButtonDown;
        public event Action<int>? KeyDown;
        public event Action<int>? KeyUp;

        public GlobalHook()
        {
            _mouseProc = MouseHookCallback;
            _keyboardProc = KeyboardHookCallback;
            _mouseHookId = SetHook(_mouseProc, WH_MOUSE_LL);
            _keyboardHookId = SetHook(_keyboardProc, WH_KEYBOARD_LL);
        }

        private IntPtr SetHook(LowLevelProc proc, int id)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule == null) return IntPtr.Zero;
                return SetWindowsHookEx(id, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && lParam != IntPtr.Zero)
            {
                var structure = Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                if (structure != null)
                {
                    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)structure;
                    
                    if (wParam == (IntPtr)WM_MOUSEMOVE)
                        MouseMoved?.Invoke(new System.Drawing.Point(hookStruct.pt.x, hookStruct.pt.y));
                    else if (wParam == (IntPtr)WM_LBUTTONDOWN)
                        LeftButtonDown?.Invoke(true);
                    else if (wParam == (IntPtr)WM_LBUTTONUP)
                        LeftButtonDown?.Invoke(false);
                    else if (wParam == (IntPtr)WM_RBUTTONDOWN)
                    {
                        bool handled = false;
                        if (RightButtonDown != null)
                        {
                            foreach (Func<bool, bool> handler in RightButtonDown.GetInvocationList())
                            {
                                if (handler(true)) handled = true;
                            }
                        }
                        if (handled) return (IntPtr)1;
                    }
                    else if (wParam == (IntPtr)WM_RBUTTONUP)
                    {
                        bool handled = false;
                        if (RightButtonDown != null)
                        {
                            foreach (Func<bool, bool> handler in RightButtonDown.GetInvocationList())
                            {
                                if (handler(false)) handled = true;
                            }
                        }
                        if (handled) return (IntPtr)1;
                    }
                }
            }
            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }
          private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = (int)Marshal.ReadInt32(lParam);
                if (wParam == (IntPtr)WM_KEYDOWN)
                    KeyDown?.Invoke(vkCode);
                else if (wParam == (IntPtr)WM_KEYUP)
                    KeyUp?.Invoke(vkCode);
            }
            return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_mouseHookId != IntPtr.Zero) UnhookWindowsHookEx(_mouseHookId);
            if (_keyboardHookId != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHookId);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);
    }
}
