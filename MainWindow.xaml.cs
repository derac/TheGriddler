using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TheGriddler;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Settings _settings;

    public MainWindow()
    {
        InitializeComponent();
        _settings = Settings.Instance;
        
        // Sync Monitors
        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
        {
            string friendlyName = WindowManager.GetFriendlyMonitorName(screen.DeviceName);
            _settings.GetOrCreateMonitorConfig(screen.DeviceName, friendlyName);
        }

        this.DataContext = _settings;
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        Settings.Instance.IsDarkMode = !Settings.Instance.IsDarkMode;
    }

    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string propertyName)
        {
            var dialog = new System.Windows.Forms.ColorDialog();
            
            string? currentValue = typeof(Settings).GetProperty(propertyName)?.GetValue(Settings.Instance) as string;
            if (currentValue != null)
            {
                try {
                    var color = (System.Drawing.Color)System.Drawing.ColorTranslator.FromHtml(currentValue);
                    dialog.Color = color;
                } catch { }
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string hex = "#" + dialog.Color.A.ToString("X2") + dialog.Color.R.ToString("X2") + dialog.Color.G.ToString("X2") + dialog.Color.B.ToString("X2");
                typeof(Settings).GetProperty(propertyName)?.SetValue(Settings.Instance, hex);
            }
        }
    }
}