using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace TheGriddler
{
    public class WindowManager
    {
        public static double DpiScale { get; private set; } = 1.0;

        public static void UpdateDpiScale(System.Windows.Media.Visual visual)
        {
            var source = System.Windows.PresentationSource.FromVisual(visual);
            if (source != null && source.CompositionTarget != null)
            {
                DpiScale = source.CompositionTarget.TransformToDevice.M11;
            }
        }
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int GetDpiForSystem();

        [DllImport("shcore.dll")]
        public static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        private const int MDT_EFFECTIVE_DPI = 0;

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        public static double GetScaleForWindow(IntPtr hWnd)
        {
            try
            {
                int dpi = GetDpiForWindow(hWnd);
                if (dpi != 0) return dpi / 96.0;

                // Fallback to monitor DPI
                IntPtr hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
                if (hMonitor != IntPtr.Zero)
                {
                    if (GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out uint dX, out uint dY) == 0)
                    {
                        return dX / 96.0;
                    }
                }

                // Fallback to system DPI
                return GetDpiForSystem() / 96.0;
            }
            catch
            {
                return 1.0;
            }
        }

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(Point point, uint dwFlags);

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        public static double GetScaleForMonitor(IntPtr hMonitor)
        {
            if (hMonitor == IntPtr.Zero) return 1.0;
            if (GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out uint dX, out uint dY) == 0)
            {
                return dX / 96.0;
            }
            return 1.0;
        }

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

            // First call finds the adapter for the device name (e.g., \\.\DISPLAY1)
            // But to get the monitor name, we need to call it again with that device name.
            uint i = 0;
            while (EnumDisplayDevices(null, i, ref dd, 0))
            {
                if (dd.DeviceName == deviceName)
                {
                    // Found the adapter, now get the monitor attached to it
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

            return deviceName; // Fallback
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
            // First check the foreground window, as it's likely the one being dragged
            IntPtr foregroundHWnd = GetForegroundWindow();
            if (foregroundHWnd != IntPtr.Zero)
            {
                uint currentPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
                GetWindowThreadProcessId(foregroundHWnd, out uint windowPid);
                
                if (windowPid != currentPid)
                {
                    // Ensure we get the root window if it's a child
                    return GetAncestor(foregroundHWnd, GA_ROOT);
                }
            }

            // Fallback to window under point
            IntPtr hWnd = WindowFromPoint(point);
            if (hWnd == IntPtr.Zero) return IntPtr.Zero;

            // Find the root window
            hWnd = GetAncestor(hWnd, GA_ROOT);

            // Filter out our own windows
            uint currentPid2 = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            GetWindowThreadProcessId(hWnd, out uint windowPid2);
            
            if (windowPid2 == currentPid2)
            {
                return IntPtr.Zero;
            }

            return hWnd;
        }

        public static Rectangle GetWindowRectangle(IntPtr hWnd)
        {
            // Use DWM attribute to get the actual visible bounds (ignoring invisible resize borders)
            if (DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT rect, Marshal.SizeOf<RECT>()) == 0)
            {
                return new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
            }
            
            // Fallback to standard GetWindowRect
            if (GetWindowRect(hWnd, out rect))
            {
                return new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
            }
            return Rectangle.Empty;
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

        public struct WindowBorders
        {
            public int OffsetX;
            public int OffsetY;
            public int OffsetWidth;
            public int OffsetHeight;
            public double ScaleFactor;
        }

        public static void EnsureRestored(IntPtr hWnd)
        {
            ShowWindow(hWnd, SW_RESTORE);
        }

        public static WindowBorders GetWindowBorders(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return new WindowBorders { ScaleFactor = 1.0 };

            IntPtr oldContext = SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            try
            {
                GetWindowRect(hWnd, out RECT windowRect);
                int result = DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT frameRect, Marshal.SizeOf<RECT>());
                
                if (result != 0)
                {
                    Logger.Log($"DwmGetWindowAttribute failed for HWND {hWnd:X} with result {result}.");
                    return new WindowBorders { ScaleFactor = 1.0 };
                }

                // In PerMonitorV2 thread, both Rects are physical.
                // We don't try to guess ScaleFactor here anymore, we let the feedback loop in SetWindowBounds handle it.
                var borders = new WindowBorders
                {
                    OffsetX = frameRect.Left - windowRect.Left,
                    OffsetY = frameRect.Top - windowRect.Top,
                    OffsetWidth = windowRect.Width - frameRect.Width,
                    OffsetHeight = windowRect.Height - frameRect.Height,
                    ScaleFactor = 1.0 
                };

                Logger.Log($"GetWindowBorders {hWnd:X}: wRect={windowRect.Left},{windowRect.Top},{windowRect.Width}x{windowRect.Height} | fRect={frameRect.Left},{frameRect.Top},{frameRect.Width}x{frameRect.Height} | finalBorders={borders.OffsetX},{borders.OffsetY},{borders.OffsetWidth},{borders.OffsetHeight}");

                return borders;
            }
            finally
            {
                if (oldContext != IntPtr.Zero) SetThreadDpiAwarenessContext(oldContext);
            }
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

        public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        public static void SetWindowBoundsPhysical(IntPtr hWnd, Rectangle physicalBounds, WindowBorders? cachedBorders = null)
        {
            if (hWnd == IntPtr.Zero) return;

            IntPtr oldContext = SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            try
            {
                Logger.Log($"=== SetWindowBoundsPhysical START {hWnd:X} ===");
                Logger.Log($"Target Physical: {physicalBounds}");

                uint flags = SWP_NOZORDER | SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_FRAMECHANGED | SWP_NOCOPYBITS;

                WindowBorders borders = GetWindowBorders(hWnd);
                
                // Track our logical dimensions
                int curL_X = physicalBounds.X - borders.OffsetX;
                int curL_Y = physicalBounds.Y - borders.OffsetY;
                int curL_W = physicalBounds.Width + borders.OffsetWidth;
                int curL_H = physicalBounds.Height + borders.OffsetHeight;

                for (int pass = 1; pass <= 2; pass++)
                {
                    Logger.Log($"Pass {pass}: Setting logical {curL_X},{curL_Y} {curL_W}x{curL_H}");
                    SetWindowPos(hWnd, IntPtr.Zero, curL_X, curL_Y, curL_W, curL_H, flags);

                    if (DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT actualFrame, Marshal.SizeOf<RECT>()) == 0)
                    {
                        int errorX = actualFrame.Left - physicalBounds.Left;
                        int errorY = actualFrame.Top - physicalBounds.Top;
                        int errorW = actualFrame.Width - physicalBounds.Width;
                        int errorH = actualFrame.Height - physicalBounds.Height;

                        Logger.Log($"Verification {pass}: Visual={actualFrame.Left},{actualFrame.Top} {actualFrame.Width}x{actualFrame.Height} | Error={errorX},{errorY} {errorW}x{errorH}");

                        if (errorX == 0 && errorY == 0 && errorW == 0 && errorH == 0) break;

                        if (pass == 1)
                        {
                            // Improved Nudge Strategy:
                            // We determine the effective scale the OS used for this window on this pass.
                            // EffectiveScale = (PhysicalResult + InternalBorders) / LogicalRequested
                            // Then targetLogical = (TargetPhysical + InternalBorders) / EffectiveScale
                            
                            double effectiveScaleW = (double)(actualFrame.Width + borders.OffsetWidth) / curL_W;
                            double effectiveScaleH = (double)(actualFrame.Height + borders.OffsetHeight) / curL_H;
                            
                            // For X and Y, we use the average scale or just fallback to 1.0 if we can't tell.
                            // But usually X/Y scale matches W/H scale.
                            double effectiveScaleX = effectiveScaleW;
                            double effectiveScaleY = effectiveScaleH;

                            if (effectiveScaleW > 0.1) curL_W = (int)Math.Round((physicalBounds.Width + borders.OffsetWidth) / effectiveScaleW);
                            if (effectiveScaleH > 0.1) curL_H = (int)Math.Round((physicalBounds.Height + borders.OffsetHeight) / effectiveScaleH);
                            
                            // For position, we nudge based on the error divided by the measured scale.
                            if (effectiveScaleX > 0.1) curL_X -= (int)Math.Round(errorX / effectiveScaleX);
                            if (effectiveScaleY > 0.1) curL_Y -= (int)Math.Round(errorY / effectiveScaleY);
                        }
                    }
                }
                
                Logger.Log($"=== SetWindowBoundsPhysical END ===");
            }
            finally
            {
                if (oldContext != IntPtr.Zero) SetThreadDpiAwarenessContext(oldContext);
            }
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
