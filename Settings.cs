using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace TheGriddler
{
    public class Binding
    {
        public string Key { get; set; } = "";
        public string Command { get; set; } = "";
    }

    public class MonitorConfig : INotifyPropertyChanged
    {
        private int _rows = 2;
        private int _columns = 3;

        public string DeviceName { get; set; } = "";

        public int Rows
        {
            get => _rows;
            set
            {
                if (_rows != value)
                {
                    _rows = value;
                    OnPropertyChanged();
                    Settings.Instance?.Save();
                }
            }
        }

        public int Columns
        {
            get => _columns;
            set
            {
                if (_columns != value)
                {
                    _columns = value;
                    OnPropertyChanged();
                    Settings.Instance?.Save();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Settings : INotifyPropertyChanged
    {
        public static Settings? Instance { get; private set; }

        public List<MonitorConfig> MonitorConfigs { get; set; } = new List<MonitorConfig>();

        public List<Binding> Bindings { get; set; } = new List<Binding>
        {
            new Binding { Key = "KEY_SPACE", Command = "Resize" },
            new Binding { Key = "KEY_LCONTROL", Command = "Move" },
            new Binding { Key = "MOUSE_RBUTTON", Command = "Resize" },
            new Binding { Key = "MOUSE_MBUTTON", Command = "Move" }
        };

        private bool _runOnStartup;
        public bool RunOnStartup
        {
            get => _runOnStartup;
            set
            {
                if (_runOnStartup != value)
                {
                    _runOnStartup = value;
                    OnPropertyChanged();
                    SetStartup(value);
                    Save();
                }
            }
        }

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TheGriddler",
            "settings.json"
        );

        private static readonly string StartupPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            "TheGriddler.lnk"
        );

        public Settings()
        {
            _runOnStartup = File.Exists(StartupPath);
            Instance = this;
        }

        public static Settings Load()
        {
            Settings settings;
            if (File.Exists(SettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsPath);
                    settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                }
                catch
                {
                    settings = new Settings();
                }
            }
            else
            {
                settings = new Settings();
            }
            
            Instance = settings;
            // Re-check startup status directly from file system to ensure accuracy
            settings._runOnStartup = File.Exists(StartupPath);
            return settings;
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

        private void SetStartup(bool enable)
        {
            try
            {
                if (enable)
                {
                    string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        // Create shortcut using PowerShell to avoid COM reference
                        string script = $"$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('{StartupPath}'); $s.TargetPath = '{exePath}'; $s.Save()";
                        var psi = new ProcessStartInfo
                        {
                            FileName = "powershell",
                            Arguments = $"-NoProfile -Command \"{script}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        Process.Start(psi);
                    }
                }
                else
                {
                    if (File.Exists(StartupPath))
                    {
                        File.Delete(StartupPath);
                    }
                }
            }
            catch (Exception ex)
            {
                // Simple logging if needed
                System.Diagnostics.Debug.WriteLine($"Error managing startup shortcut: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
