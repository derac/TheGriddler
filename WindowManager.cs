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

                // DIAGNOSIS: If the window belongs to a non-DPI-aware process, GetWindowRect might return
                // logical pixels even if our thread is PerMonitorV2. DwmGetWindowAttribute always returns physical.
                // We compare them to see if we need to scale up the windowRect.
                double windowW = windowRect.Right - windowRect.Left;
                double windowH = windowRect.Bottom - windowRect.Top;
                double frameW = frameRect.Right - frameRect.Left;
                double frameH = frameRect.Bottom - frameRect.Top;

                double scaleX = 1.0;
                double scaleY = 1.0;

                // If windowRect is significantly smaller than frameRect, it's likely logical pixels.
                // Note: Standard borders are tiny (usually < 20px), so if diff is e.g. > 50px, it's scaling.
                if (windowW > 0 && frameW > windowW + 50)
                {
                    scaleX = frameW / windowW;
                    scaleY = frameH / windowH;
                    Logger.Log($"Detected virtualized window. Estimated internal scale: {scaleX:F2}x{scaleY:F2}");
                }

                var borders = new WindowBorders
                {
                    OffsetX = (int)Math.Round(frameRect.Left - (windowRect.Left * scaleX)),
                    OffsetY = (int)Math.Round(frameRect.Top - (windowRect.Top * scaleY)),
                    OffsetWidth = (int)Math.Round((windowRect.Right - windowRect.Left) * scaleX - (frameRect.Right - frameRect.Left)),
                    OffsetHeight = (int)Math.Round((windowRect.Bottom - windowRect.Top) * scaleY - (frameRect.Bottom - frameRect.Top)),
                    ScaleFactor = scaleX
                };

                Logger.Log($"GetWindowBorders {hWnd:X}: wRect={windowRect.Left},{windowRect.Top},{windowRect.Width}x{windowRect.Height} | fRect={frameRect.Left},{frameRect.Top},{frameRect.Width}x{frameRect.Height} | scale={scaleX:F2} | finalBorders={borders.OffsetX},{borders.OffsetY},{borders.OffsetWidth},{borders.OffsetHeight}");

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
                Logger.Log($"SetWindowBoundsPhysical {hWnd:X}: Target={physicalBounds}");

                uint flags = SWP_NOZORDER | SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_FRAMECHANGED | SWP_NOCOPYBITS;

                // Pass 1: Move to target monitor to trigger OS DPI transition
                SetWindowPos(hWnd, IntPtr.Zero, physicalBounds.X, physicalBounds.Y, physicalBounds.Width, physicalBounds.Height, flags);

                // Pass 2: Re-measure borders and scale
                WindowBorders actualBorders = GetWindowBorders(hWnd);
                
                // GetWindowRect might still report old logical coords or new physical coords depending on OS lag.
                GetWindowRect(hWnd, out RECT currentRect);
                double actualW = currentRect.Right - currentRect.Left;
                double actualH = currentRect.Bottom - currentRect.Top;

                // Calculate valid Scale X which seems to be the culprit.
                double inputScaleX = (physicalBounds.Width > 0) ? (actualW / physicalBounds.Width) : 1.0;
                double inputScaleY = (physicalBounds.Height > 0) ? (actualH / physicalBounds.Height) : 1.0;

                // Heuristic: If we detected virtualization (ScaleFactor > 1.1) but inputScale is near 1.0,
                // it means GetWindowRect returned physical-like sizing (temporarily) but WILL act virtualized.
                // We should preemptively correct using the detected ScaleFactor.
                
                if (actualBorders.ScaleFactor > 1.1 && inputScaleX < 1.1)
                {
                     Logger.Log($"Lag Detection: InputScale {inputScaleX:F2} vs Detected {actualBorders.ScaleFactor:F2}. Preempting lag.");
                     inputScaleX = actualBorders.ScaleFactor;
                     inputScaleY = actualBorders.ScaleFactor; // Assume uniform scaling
                }

                // Fix for "First draw incorrect" on DPI change:
                // If the app is native (ScaleFactor ~ 1.0) but sizes mismatch, assume transient resizing.
                if (actualBorders.ScaleFactor < 1.1 && (inputScaleX > 1.1 || inputScaleX < 0.9)) 
                {
                     Logger.Log($"Transient DPI Reaction: InputScale {inputScaleX:F2} vs Detected {actualBorders.ScaleFactor:F2}. Enforcing physical bounds.");
                     inputScaleX = 1.0;
                     inputScaleY = 1.0;
                }

                if (Math.Abs(inputScaleX - 1.0) < 0.01) inputScaleX = 1.0;
                if (Math.Abs(inputScaleY - 1.0) < 0.01) inputScaleY = 1.0;
                
                // Adjustment logic:
                // We want to place the window at a specific PHYSICAL location (X, Y).
                // SetWindowPos from a PMv2 thread expects physical desktop coordinates for position.
                // However, the SIZE (cx, cy) determines the window size. If the window is virtualized,
                // the OS will multiply the size we give it by the scale factor.
                // To get a final physical size of P_Size, we must pass P_Size / Scale.
                
                // Position (X, Y): use physical coordinates directly. Match the Visual Frame (physicalBounds)
                // adjusted by the physical border offset.
                int finalX = physicalBounds.X - actualBorders.OffsetX;
                int finalY = physicalBounds.Y - actualBorders.OffsetY;
                
                // Size (W, H): Scale down so the OS scales it back up to our desired physical size.
                int finalW = (int)Math.Round((physicalBounds.Width + actualBorders.OffsetWidth) / inputScaleX);
                int finalH = (int)Math.Round((physicalBounds.Height + actualBorders.OffsetHeight) / inputScaleY);

                if (inputScaleX != 1.0 || actualBorders.OffsetWidth != 0 || actualBorders.ScaleFactor > 1.1)
                {
                    Logger.Log($"Pass 2 Adjustment: InputScale={inputScaleX:F2} (Detected={actualBorders.ScaleFactor:F2}) | CurrentPhysical={actualW}x{actualH} | FinalLogical={finalX},{finalY},{finalW}x{finalH}");
                    SetWindowPos(hWnd, IntPtr.Zero, finalX, finalY, finalW, finalH, flags);
                }
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
