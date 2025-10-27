using System.Windows;

namespace ZLFileRelay.ConfigTool.Views;

/// <summary>
/// Dialog for prompting service account credentials when needed for impersonated operations.
/// </summary>
public partial class ServiceAccountCredentialDialog : Window
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;
    public bool RememberForSession { get; private set; }
    public string CurrentServiceAccount { get; set; } = "Loading...";

    public ServiceAccountCredentialDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    public ServiceAccountCredentialDialog(string currentServiceAccount, string? defaultUsername = null)
        : this()
    {
        CurrentServiceAccount = currentServiceAccount;
        Username = defaultUsername ?? string.Empty;
        UsernameTextBox.Text = Username;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Username = UsernameTextBox.Text.Trim();
        Password = PasswordBox.Password;
        RememberForSession = RememberCheckBox.IsChecked ?? false;

        if (string.IsNullOrWhiteSpace(Username))
        {
            ShowError("Username is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ShowError("Password is required.");
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }
}

