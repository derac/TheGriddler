using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace TheGriddler
{
    public class WindowManager
    {
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(Point point, uint dwFlags);

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        public static string GetFriendlyMonitorName(string deviceName)
        {
            DISPLAY_DEVICE dd = new DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf(dd);

            uint i = 0;
            while (EnumDisplayDevices(null, i, ref dd, 0))
            {
                if (dd.DeviceName == deviceName)
                {
                    DISPLAY_DEVICE monitorDd = new DISPLAY_DEVICE();
                    monitorDd.cb = Marshal.SizeOf(monitorDd);
                    if (EnumDisplayDevices(dd.DeviceName, 0, ref monitorDd, 0))
                    {
                        return monitorDd.DeviceString;
                    }
                    break;
                }
                i++;
            }

            return deviceName;
        }

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(Point point);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        private const uint GA_ROOT = 2;

        public static IntPtr GetTargetWindow(Point point)
        {
            IntPtr foregroundHWnd = GetForegroundWindow();
            if (foregroundHWnd != IntPtr.Zero)
            {
                uint currentPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
                GetWindowThreadProcessId(foregroundHWnd, out uint windowPid);
                
                if (windowPid != currentPid)
                {
                    return GetAncestor(foregroundHWnd, GA_ROOT);
                }
            }

            IntPtr hWnd = WindowFromPoint(point);
            if (hWnd == IntPtr.Zero) return IntPtr.Zero;

            hWnd = GetAncestor(hWnd, GA_ROOT);

            uint currentPid2 = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            GetWindowThreadProcessId(hWnd, out uint windowPid2);
            
            if (windowPid2 == currentPid2)
            {
                return IntPtr.Zero;
            }

            return hWnd;
        }

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsZoomed(IntPtr hWnd);

        private const int SW_RESTORE = 9;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_CANCELMODE = 0x001F;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOCOPYBITS = 0x0100;



        public static void EnsureRestored(IntPtr hWnd)
        {
            ShowWindow(hWnd, SW_RESTORE);
        }

        public static void SetWindowBounds(IntPtr hWnd, Rectangle targetBounds)
        {
            if (hWnd == IntPtr.Zero) return;

            int winX = targetBounds.X;
            int winY = targetBounds.Y;
            int winW = targetBounds.Width;
            int winH = targetBounds.Height;

            uint flags = SWP_NOZORDER | SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_FRAMECHANGED | SWP_NOCOPYBITS;

            Logger.Log($"DEBUG: SetWindowPos {hWnd:X}: target={targetBounds.X},{targetBounds.Y} {targetBounds.Width}x{targetBounds.Height} | result={winX},{winY} {winW}x{winH}");

            SetWindowPos(hWnd, IntPtr.Zero, winX, winY, winW, winH, flags);
        }

        // Maintaining the old method for backward compatibility if needed, but implementation is redirected or deprecated.
        // For now, removing the old MoveAndResize to force upgrade to proper DPI handling check.


        public static void BreakDragLoop(IntPtr hWnd)
        {
            // Force the window to cancel any internal modes (sizing/moving)
            SendMessage(hWnd, WM_CANCELMODE, IntPtr.Zero, IntPtr.Zero);
            // Simulate a mouse up to be sure
            SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, IntPtr.Zero);
            
            ReleaseCapture();
        }
    }
}
