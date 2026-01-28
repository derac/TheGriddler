using System;
using System.IO;
using System.Text.Json;

namespace WindowGridRedux
{
    public class Binding
    {
        public string Key { get; set; } = "";
        public string Command { get; set; } = "";
    }

    public class Settings
    {
        public int GridWidth { get; set; } = 3;
        public int GridHeight { get; set; } = 2;
        public double Opacity { get; set; } = 0.75;
        public double BlurRadius { get; set; } = 3;
        public bool ShowSplashScreen { get; set; } = false;
        public string Theme { get; set; } = "default";
        public bool FillWindow { get; set; } = true;
        public bool RadialGradient { get; set; } = true;

        public List<Binding> Bindings { get; set; } = new List<Binding>
        {
            new Binding { Key = "KEY_SPACE", Command = "Resize" },
            new Binding { Key = "KEY_LCONTROL", Command = "Move" },
            new Binding { Key = "MOUSE_RBUTTON", Command = "Resize" },
            new Binding { Key = "MOUSE_MBUTTON", Command = "Move" }
        };

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowGridRedux",
            "settings.json"
        );

        public static Settings Load()
        {
            if (File.Exists(SettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                }
                catch
                {
                    return new Settings();
                }
            }
            return new Settings();
        }

        public void Save()
        {
            try
            {
                string? directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Log error or ignore
            }
        }
    }
}
