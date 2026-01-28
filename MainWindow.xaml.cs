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
            _settings.GetOrCreateMonitorConfig(screen.DeviceName);
        }

        this.DataContext = _settings;
    }


}