using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace TheGriddler.Core;

public class WindowManager
{
    public static string GetFriendlyMonitorName(string deviceName)
    {
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
                    return monitorDd.DeviceString;
                }
                break;
            }
            i++;
        }

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
        // Force the window to cancel any internal modes (sizing/moving)
        NativeMethods.SendMessage(hWnd, NativeMethods.WM_CANCELMODE, IntPtr.Zero, IntPtr.Zero);
        // Simulate a mouse up to be sure
        NativeMethods.SendMessage(hWnd, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, IntPtr.Zero);
        
        NativeMethods.ReleaseCapture();
    }
}

