using System.Windows;
using System.Windows.Controls;

namespace ZLFileRelay.ConfigTool.Views
{
    /// <summary>
    /// Interaction logic for WebPortalView.xaml
    /// </summary>
    public partial class WebPortalView : UserControl
    {
        public WebPortalView()
        {
            InitializeComponent();
            
            // Wire up PasswordBox to ViewModel
            // PasswordBox doesn't support direct binding for security reasons
            ClientSecretBox.PasswordChanged += (s, e) =>
            {
                if (DataContext is ViewModels.WebPortalViewModel vm)
                {
                    vm.EntraIdClientSecret = ClientSecretBox.Password;
                }
            };
            
            // Store reference in ViewModel for bidirectional updates
            Loaded += (s, e) =>
            {
                if (DataContext is ViewModels.WebPortalViewModel vm)
                {
                    vm.SetPasswordBoxReference(ClientSecretBox);
                }
            };
        }

        private void LogoPathTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = LogoPathTextBox;
            if (textBox.Text == "Path to your company logo (relative to web portal root, e.g., 'Assets/logo.png') - Optional")
            {
                textBox.Text = "";
            }
        }

        private void LogoPathTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = LogoPathTextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (textBox.Tag is string placeholder)
                {
                    textBox.Text = placeholder;
                }
            }
        }
    }
}
