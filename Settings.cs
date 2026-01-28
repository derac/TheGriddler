using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace TheGriddler;

public class MonitorConfig : INotifyPropertyChanged
{
    private int _rows = 2;
    private int _columns = 2;

    public string DeviceName { get; set; } = "";
    public string FriendlyName { get; set; } = "";

    public string DisplayName => string.IsNullOrEmpty(FriendlyName) ? DeviceName : FriendlyName;

    public int Rows
    {
        get => _rows;
        set
        {
            if (_rows != value)
            {
                _rows = value;
                OnPropertyChanged();
                if (!Settings.IsLoading) Settings.Instance?.Save();
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
                if (!Settings.IsLoading) Settings.Instance?.Save();
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
    private static Settings? _instance;
    public static Settings Instance => _instance ??= Load();

    public static bool IsLoading { get; private set; }

    public List<MonitorConfig> MonitorConfigs { get; set; } = new List<MonitorConfig>();

    public const string DefaultGridColor = "#22FFFFFF";
    public const string DefaultSelectionColor = "#2200FFFF";
    public const string DefaultSelectionBorderColor = "#8800FFFF";

    private string _gridColor = DefaultGridColor;
    public string GridColor
    {
        get => _gridColor;
        set
        {
            if (_gridColor != value)
            {
                _gridColor = value;
                OnPropertyChanged();
                if (!IsLoading) Save();
            }
        }
    }

    private string _selectionColor = DefaultSelectionColor;
    public string SelectionColor
    {
        get => _selectionColor;
        set
        {
            if (_selectionColor != value)
            {
                _selectionColor = value;
                OnPropertyChanged();
                if (!IsLoading) Save();
            }
        }
    }

    private string _selectionBorderColor = DefaultSelectionBorderColor;
    public string SelectionBorderColor
    {
        get => _selectionBorderColor;
        set
        {
            if (_selectionBorderColor != value)
            {
                _selectionBorderColor = value;
                OnPropertyChanged();
                if (!IsLoading) Save();
            }
        }
    }

    private bool _isDarkMode = true;
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnPropertyChanged();
                if (!IsLoading) Save();
            }
        }
    }

    public MonitorConfig GetOrCreateMonitorConfig(string deviceName, string friendlyName = "")
    {
        var config = MonitorConfigs.Find(m => m.DeviceName == deviceName);
        if (config == null)
        {
            config = new MonitorConfig { DeviceName = deviceName, FriendlyName = friendlyName };
            MonitorConfigs.Add(config);
            Save();
        }
        else if (!string.IsNullOrEmpty(friendlyName) && config.FriendlyName != friendlyName)
        {
            config.FriendlyName = friendlyName;
            Save();
        }
        return config;
    }


    public void ResetColors()
    {
        GridColor = DefaultGridColor;
        SelectionColor = DefaultSelectionColor;
        SelectionBorderColor = DefaultSelectionBorderColor;
    }

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
                if (!IsLoading) Save();
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

    // Constructor must be public for System.Text.Json deserialization
    public Settings()
    {
        _runOnStartup = File.Exists(StartupPath);
    }

    public static Settings Load()
    {
        if (IsLoading) return _instance ?? new Settings();
        IsLoading = true;
        try
        {
            if (File.Exists(SettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsPath);
                    _instance = JsonSerializer.Deserialize<Settings>(json);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error deserializing settings: {ex.Message}");
                    _instance = null;
                }
            }
            
            if (_instance == null)
            {
                _instance = new Settings();
                _instance.Save(); // Save defaults if no file exists
            }
            
            _instance._runOnStartup = File.Exists(StartupPath);
            return _instance;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Save()
    {
        if (IsLoading) return;

        try
        {
            string? directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
            Logger.Log($"Settings saved to {SettingsPath}");
        }
        catch (Exception ex)
        {
            Logger.Log($"Error saving settings: {ex.Message}");
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
