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
        _settings = Settings.Load();
        
        // Sync Monitors
        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
        {
            var existing = _settings.MonitorConfigs.Find(m => m.DeviceName == screen.DeviceName);
            if (existing == null)
            {
                _settings.MonitorConfigs.Add(new MonitorConfig
                {
                    DeviceName = screen.DeviceName,
                    Rows = 2,
                    Columns = 2
                });
            }
        }

        this.DataContext = _settings;
    }


}