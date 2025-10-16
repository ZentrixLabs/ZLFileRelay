using System.Windows;

namespace ZLFileRelay.ConfigTool.Dialogs;

public partial class CredentialDialog : Window
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;

    public CredentialDialog()
    {
        InitializeComponent();
        DataContext = this;
        
        // Pre-fill with current user's domain
        var currentUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        var parts = currentUser.Split('\\');
        if (parts.Length > 1)
        {
            Username = parts[0] + "\\";
        }
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        Password = PasswordBox.Password;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorText.Text = "Please enter a username.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorText.Text = "Please enter a password.";
            ErrorText.Visibility = Visibility.Visible;
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
}

