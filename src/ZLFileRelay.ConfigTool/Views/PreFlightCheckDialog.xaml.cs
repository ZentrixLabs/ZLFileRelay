using System.Windows;
using ZLFileRelay.ConfigTool.Services;

namespace ZLFileRelay.ConfigTool.Views;

public partial class PreFlightCheckDialog : Window
{
    private readonly PreFlightResult _result;
    public bool ShouldProceed { get; private set; }

    public PreFlightCheckDialog(PreFlightResult result)
    {
        InitializeComponent();
        _result = result;
        
        DisplayResults();
    }

    private void DisplayResults()
    {
        // Bind checks to UI
        ChecksItemsControl.ItemsSource = _result.Checks;
        
        // Update summary
        SummaryText.Text = $"Completed {_result.Checks.Count} checks at {_result.CheckTime:HH:mm:ss}";
        
        // Update status summary
        if (_result.HasErrors)
        {
            StatusIcon.Text = "\uE711"; // ErrorBadge
            StatusIcon.Foreground = System.Windows.Media.Brushes.Red;
            StatusSummary.Text = $"{_result.ErrorCount} error(s) found - cannot start service";
            StatusSummary.Foreground = System.Windows.Media.Brushes.Red;
            ProceedButton.IsEnabled = false;
            ProceedButton.Content = "Cannot Proceed";
        }
        else if (_result.HasWarnings)
        {
            StatusIcon.Text = "\uE7BA"; // Warning
            StatusIcon.Foreground = System.Windows.Media.Brushes.Orange;
            StatusSummary.Text = $"{_result.WarningCount} warning(s) - proceed with caution";
            StatusSummary.Foreground = System.Windows.Media.Brushes.Orange;
            ProceedButton.Content = "Proceed Anyway";
        }
        else
        {
            StatusIcon.Text = "\uE73E"; // CheckMark
            StatusIcon.Foreground = System.Windows.Media.Brushes.Green;
            StatusSummary.Text = $"All checks passed ({_result.PassCount}/{_result.Checks.Count})";
            StatusSummary.Foreground = System.Windows.Media.Brushes.Green;
        }
    }

    private void AutoFixButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button) return;
        if (button.Tag is not PreFlightCheck check) return;
        
        if (check.AutoFixAction == null) return;

        try
        {
            button.IsEnabled = false;
            button.Content = "Fixing...";
            
            var result = check.AutoFixAction();
            
            MessageBox.Show(result, "Auto-Fix Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Refresh would require re-running checks
            button.Content = "Fixed";
            
            // Update check status
            check.Status = CheckStatus.Pass;
            check.Message = result;
            
            // Refresh the display
            ChecksItemsControl.ItemsSource = null;
            ChecksItemsControl.ItemsSource = _result.Checks;
            DisplayResults();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Auto-fix failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            button.IsEnabled = true;
            button.Content = "Fix";
        }
    }

    private void ProceedButton_Click(object sender, RoutedEventArgs e)
    {
        ShouldProceed = true;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ShouldProceed = false;
        DialogResult = false;
        Close();
    }
}

