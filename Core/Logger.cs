using System;
using System.IO;

namespace TheGriddler.Core;

public static class Logger
{
#if DEBUG
    private static string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
#endif

    static Logger()
    {
#if DEBUG
        try
        {
            if (File.Exists(_logPath)) File.Delete(_logPath);
        }
        catch { }
#endif
    }

    public static void Log(string message)
    {
#if DEBUG
        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            File.AppendAllText(_logPath, $"[{timestamp}] {message}{Environment.NewLine}");
        }
        catch { }
#endif
    }
}
