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

namespace WindowGridRedux;

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
        this.DataContext = _settings;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.Save();
        this.Close();
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        _settings = Settings.Load();
        this.DataContext = _settings;
    }
}