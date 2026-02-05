using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace TheGriddler.Core;

public class WindowManager
{
    public static string GetFriendlyMonitorName(string deviceName)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        NativeMethods.DISPLAY_DEVICE dd = new NativeMethods.DISPLAY_DEVICE();
        dd.cb = Marshal.SizeOf(dd);

        uint i = 0;
        while (NativeMethods.EnumDisplayDevices(null, i, ref dd, 0))
        {
            if (dd.DeviceName == deviceName)
            {
                NativeMethods.DISPLAY_DEVICE monitorDd = new NativeMethods.DISPLAY_DEVICE();
                monitorDd.cb = Marshal.SizeOf(monitorDd);
                if (NativeMethods.EnumDisplayDevices(dd.DeviceName, 0, ref monitorDd, 0))
                {
                    Logger.Log($"GetFriendlyMonitorName: Found '{monitorDd.DeviceString}' for {deviceName} in {sw.ElapsedMilliseconds}ms (iterations={i})");
                    return monitorDd.DeviceString;
                }
                break;
            }
            i++;
        }

        Logger.Log($"GetFriendlyMonitorName: No match for {deviceName} in {sw.ElapsedMilliseconds}ms (iterations={i})");
        return deviceName;
    }

    public static IntPtr GetTargetWindow(Point point)
    {
        IntPtr foregroundHWnd = NativeMethods.GetForegroundWindow();
        if (foregroundHWnd != IntPtr.Zero)
        {
            uint currentPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            NativeMethods.GetWindowThreadProcessId(foregroundHWnd, out uint windowPid);
            
            if (windowPid != currentPid)
            {
                return NativeMethods.GetAncestor(foregroundHWnd, NativeMethods.GA_ROOT);
            }
        }

        IntPtr hWnd = NativeMethods.WindowFromPoint(point);
        if (hWnd == IntPtr.Zero) return IntPtr.Zero;

        hWnd = NativeMethods.GetAncestor(hWnd, NativeMethods.GA_ROOT);

        uint currentPid2 = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        NativeMethods.GetWindowThreadProcessId(hWnd, out uint windowPid2);
        
        if (windowPid2 == currentPid2)
        {
            return IntPtr.Zero;
        }

        return hWnd;
    }

    public static void EnsureRestored(IntPtr hWnd)
    {
        NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
    }

    public static void SetWindowBounds(IntPtr hWnd, Rectangle targetBounds)
    {
        if (hWnd == IntPtr.Zero) return;

        int winX = targetBounds.X;
        int winY = targetBounds.Y;
        int winW = targetBounds.Width;
        int winH = targetBounds.Height;

        uint flags = NativeMethods.SWP_NOZORDER | NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_NOCOPYBITS;

        Logger.Log($"DEBUG: SetWindowPos {hWnd:X}: target={targetBounds.X},{targetBounds.Y} {targetBounds.Width}x{targetBounds.Height} | result={winX},{winY} {winW}x{winH}");

        NativeMethods.SetWindowPos(hWnd, IntPtr.Zero, winX, winY, winW, winH, flags);
    }

    public static void BreakDragLoop(IntPtr hWnd)
    {
        Logger.Log($"BreakDragLoop: START for hWnd={hWnd:X}");
        
        // Force the window to cancel any internal modes (sizing/moving)
        IntPtr cancelResult = NativeMethods.SendMessage(hWnd, NativeMethods.WM_CANCELMODE, IntPtr.Zero, IntPtr.Zero);
        Logger.Log($"BreakDragLoop: WM_CANCELMODE result={cancelResult}");
        
        // Simulate a mouse up to be sure
        IntPtr lbUpResult = NativeMethods.SendMessage(hWnd, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, IntPtr.Zero);
        Logger.Log($"BreakDragLoop: WM_LBUTTONUP result={lbUpResult}");
        
        bool releaseResult = NativeMethods.ReleaseCapture();
        Logger.Log($"BreakDragLoop: ReleaseCapture result={releaseResult}");
    }
}
