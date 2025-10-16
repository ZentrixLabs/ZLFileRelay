using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace ZLFileRelay.ConfigTool.Views;

/// <summary>
/// Interaction logic for AboutView.xaml
/// </summary>
public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
        DataContext = new AboutViewModel();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            // Open URL in default browser
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not open link: {ex.Message}", 
                "Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
    }
}

/// <summary>
/// Simple ViewModel for About page data
/// </summary>
public class AboutViewModel
{
    public string Version { get; set; }
    public string BuildDate { get; set; }

    public AboutViewModel()
    {
        // Get version from assembly
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        Version = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        
        // Get build date from executable (works with single-file apps)
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
            {
                var buildDate = System.IO.File.GetLastWriteTime(exePath);
                BuildDate = buildDate.ToString("MMMM dd, yyyy");
            }
            else
            {
                BuildDate = DateTime.Now.ToString("MMMM dd, yyyy");
            }
        }
        catch
        {
            BuildDate = DateTime.Now.ToString("MMMM dd, yyyy");
        }
    }
}

