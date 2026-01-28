using System;
using System.IO;

namespace WindowGridRedux
{
    public static class Logger
    {
        private static string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");

        static Logger()
        {
            try
            {
                if (File.Exists(_logPath)) File.Delete(_logPath);
            }
            catch { }
        }

        public static void Log(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                File.AppendAllText(_logPath, $"[{timestamp}] {message}{Environment.NewLine}");
            }
            catch { }
        }
    }
}
