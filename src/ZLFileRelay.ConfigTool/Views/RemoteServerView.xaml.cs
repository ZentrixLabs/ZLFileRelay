using System.Windows.Controls;
using ZLFileRelay.ConfigTool.ViewModels;

namespace ZLFileRelay.ConfigTool.Views;

/// <summary>
/// Interaction logic for RemoteServerView.xaml
/// </summary>
public partial class RemoteServerView : UserControl
{
    public RemoteServerView()
    {
        InitializeComponent();
        
        // Wire up PasswordBox to ViewModel
        // PasswordBox doesn't support direct binding for security reasons, so we handle it in code-behind
        AdminPasswordBox.PasswordChanged += (s, e) =>
        {
            if (DataContext is RemoteServerViewModel vm)
            {
                vm.Password = AdminPasswordBox.Password;
            }
        };
    }
}

