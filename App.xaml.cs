using System.Windows;
using System.Runtime.InteropServices;

namespace TheGriddler;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private MainController? _controller;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private System.Drawing.Icon? _currentIcon;
    private MainWindow? _settingsWindow;
    private System.Windows.Forms.Form? _dummyForm;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Logger.Log("App OnStartup started.");
        _controller = new MainController();
        Logger.Log("MainController initialized.");

        _notifyIcon = new System.Windows.Forms.NotifyIcon();
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "The Griddler";
        Logger.Log("NotifyIcon made visible.");
        _notifyIcon.MouseDoubleClick += (s, a) => ShowSettings();

        // Load Icon
        try
        {
            var resourceUri = new Uri("pack://application:,,,/Resources/icon.png");
            var streamInfo = System.Windows.Application.GetResourceStream(resourceUri);
            if (streamInfo != null)
            {
                using var stream = streamInfo.Stream;
                using var bitmap = new System.Drawing.Bitmap(stream);
                using var smallBitmap = new System.Drawing.Bitmap(bitmap, new System.Drawing.Size(32, 32));
                IntPtr hIcon = smallBitmap.GetHicon();
                _currentIcon = System.Drawing.Icon.FromHandle(hIcon);
                _notifyIcon.Icon = _currentIcon;
                Logger.Log("Custom icon assigned.");
            }
            else
            {
                string? location = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(location))
                {
                    _currentIcon = System.Drawing.Icon.ExtractAssociatedIcon(location);
                    _notifyIcon.Icon = _currentIcon;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error loading icon: {ex.Message}");
        }

        _dummyForm = new System.Windows.Forms.Form();
        // Force handle creation
        IntPtr dummyHandle = _dummyForm.Handle;

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Exit", null, (s, a) => Shutdown());

        _notifyIcon.MouseUp += (s, a) =>
        {
            if (a.Button == System.Windows.Forms.MouseButtons.Right && _dummyForm != null)
            {
                SetForegroundWindow(_dummyForm.Handle);
                contextMenu.Show(System.Windows.Forms.Cursor.Position);
            }
        };
    }

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    extern static bool DestroyIcon(IntPtr handle);

    private void ShowSettings()
    {
        if (_settingsWindow != null && _settingsWindow.IsVisible)
        {
            _settingsWindow.Activate();
            if (_settingsWindow.WindowState == WindowState.Minimized)
                _settingsWindow.WindowState = WindowState.Normal;
        }
        else
        {
            _settingsWindow = new MainWindow();
            _settingsWindow.Closed += (s, a) => _settingsWindow = null;
            _settingsWindow.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        if (_currentIcon != null)
        {
            IntPtr handle = _currentIcon.Handle;
            _currentIcon.Dispose();
            DestroyIcon(handle);
        }
        _dummyForm?.Dispose();
        _controller?.Dispose();
        base.OnExit(e);
    }
}
