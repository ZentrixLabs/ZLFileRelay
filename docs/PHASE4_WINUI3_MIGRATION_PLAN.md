# Phase 4: WinUI 3 Configuration Tool - Migration Plan

**Date:** October 8, 2025  
**Framework:** WinUI 3 (Microsoft.WindowsAppSDK)  
**Based On:** Legacy WPF ConfigTool + Modern Improvements  
**Timeline:** 2-3 days

---

## 🎯 Goal

Create a modern WinUI 3 configuration tool that:
1. Provides all functionality from the legacy WPF tool
2. Adds modern Fluent Design aesthetics
3. Improves UX with better validation and feedback
4. Lays groundwork for future remote administration

---

## 📋 Features from Legacy Tool (To Keep/Improve)

### Core Configuration Management
- ✅ Edit all appsettings.json values
- ✅ Save/Load configuration
- ✅ Validate configuration before saving
- ✅ Reset to defaults

### Service Management
- ✅ Start/Stop/Restart Windows Service
- ✅ Install/Uninstall Windows Service
- ✅ View current service status
- ✅ Check if running as administrator

### SSH Key Management
- ✅ Generate SSH key pairs (ED25519, RSA)
- ✅ View/copy public key
- ✅ Test SSH connection
- ✅ Browse for key file

### Connection Testing
- ✅ Test SSH/SCP connection
- ✅ Test SMB/CIFS connection
- ✅ Display connection results
- ✅ Save successful configurations

### Path Management
- ✅ Browse for directories
- ✅ Create directories if needed
- ✅ Validate paths exist
- ✅ Check disk space

---

## 🆕 Modern Improvements (WinUI 3 Advantages)

### Visual Design
- ✨ Fluent Design System (acrylic, reveal effects)
- ✨ Modern controls (NavigationView, InfoBar, etc.)
- ✨ Dark mode support
- ✨ Smooth animations
- ✨ Better responsive layout

### User Experience
- ✨ Real-time validation feedback
- ✨ In-place editing with undo
- ✨ Progress indicators for long operations
- ✨ Toast notifications
- ✨ Better error messages with suggestions
- ✨ Inline help/documentation

### Technical
- ✨ MVVM with CommunityToolkit.Mvvm
- ✨ Async/await throughout
- ✨ Better exception handling
- ✨ Dependency injection
- ✨ Unit testable ViewModels

---

## 🏗️ Project Structure

```
ZLFileRelay.ConfigTool.WinUI/
├── App.xaml
├── App.xaml.cs
├── Package.appxmanifest              # WinUI 3 manifest
├── MainWindow.xaml                   # Shell with NavigationView
├── MainWindow.xaml.cs
│
├── Views/                            # All UI pages
│   ├── GeneralPage.xaml
│   ├── PathsPage.xaml
│   ├── ServicePage.xaml
│   ├── WebPortalPage.xaml
│   ├── SshPage.xaml
│   ├── SmbPage.xaml
│   ├── SecurityPage.xaml
│   └── ServiceControlPage.xaml
│
├── ViewModels/                       # MVVM ViewModels
│   ├── MainViewModel.cs
│   ├── GeneralViewModel.cs
│   ├── PathsViewModel.cs
│   ├── ServiceViewModel.cs
│   ├── WebPortalViewModel.cs
│   ├── SshViewModel.cs
│   ├── SmbViewModel.cs
│   ├── SecurityViewModel.cs
│   └── ServiceControlViewModel.cs
│
├── Services/                         # Business logic
│   ├── ConfigurationService.cs       # Load/save config
│   ├── ServiceManager.cs             # Windows Service control
│   ├── SshKeyGenerator.cs            # SSH key generation
│   ├── ConnectionTester.cs           # Test connections
│   ├── ValidationService.cs          # Validate settings
│   └── NavigationService.cs          # Page navigation
│
├── Dialogs/                          # Modal dialogs
│   ├── SshKeyDialog.xaml
│   ├── ConnectionTestDialog.xaml
│   ├── LogViewerDialog.xaml
│   └── AboutDialog.xaml
│
├── Controls/                         # Custom controls
│   ├── PathBrowserControl.xaml       # Path with browse button
│   ├── PasswordControl.xaml          # Password with show/hide
│   └── StatusIndicator.xaml          # Status badge
│
├── Converters/                       # XAML converters
│   ├── BoolToVisibilityConverter.cs
│   ├── InverseBoolConverter.cs
│   ├── FileSizeConverter.cs
│   └── ServiceStatusConverter.cs
│
├── Helpers/                          # Utility classes
│   ├── AdminHelper.cs                # Check admin rights
│   ├── ProcessHelper.cs              # Run external processes
│   └── FileDialogHelper.cs           # File/folder dialogs
│
└── Assets/                           # Resources
    ├── Icons/                        # App icons
    ├── Images/                       # Images
    └── Strings/                      # Localization (future)
```

---

## 🎨 Main Window Design (NavigationView)

```
┌──────────────────────────────────────────────────────────────┐
│ ☰  ZL File Relay Configuration                    🔄 Save _ □ X │
├─────┬────────────────────────────────────────────────────────┤
│     │                                                        │
│  ⚙  │  General Settings                                     │
│     │  ───────────────────────────────────────────────────  │
│  📁 │                                                        │
│     │  Company Information                                  │
│  🚀 │  ┌────────────────────────────────────────────────┐  │
│     │  │ Company Name  [Your Company Name            ]  │  │
│  🌐 │  │ Product Name  [ZL File Relay                ]  │  │
│     │  │ Site Name     [Your Site Name               ]  │  │
│  🔐 │  │ Support Email [support@example.com          ]  │  │
│     │  └────────────────────────────────────────────────┘  │
│  🛡️ │                                                        │
│     │  Theme Colors                                         │
│  🔌 │  ┌────────────────────────────────────────────────┐  │
│     │  │ Primary   ■ #0066CC   [Choose]                 │  │
│     │  │ Secondary ■ #003366   [Choose]                 │  │
│     │  │ Accent    ■ #FF6600   [Choose]                 │  │
│     │  └────────────────────────────────────────────────┘  │
│     │                                                        │
│     │                                                        │
│  ℹ️  │  [Apply]  [Reset to Defaults]                       │
│     │                                                        │
└─────┴────────────────────────────────────────────────────────┘
```

### Navigation Items:
1. **⚙️ General** - Branding, theme, basic settings
2. **📁 Paths** - Directory configuration
3. **🚀 Service** - Service behavior settings
4. **🌐 Web Portal** - Web portal configuration
5. **🔐 SSH/SCP** - SSH transfer settings
6. **🛡️ SMB/CIFS** - SMB transfer settings
7. **🔌 Service Control** - Start/stop service, view logs
8. **ℹ️ About** - Version, license, help

---

## 🔧 Key Services Implementation

### ConfigurationService.cs
```csharp
public class ConfigurationService
{
    private readonly string _configPath;
    private ZLFileRelayConfiguration? _currentConfig;

    public async Task<ZLFileRelayConfiguration> LoadAsync()
    {
        // Load from appsettings.json
        // Handle errors gracefully
        // Return default if not found
    }

    public async Task<bool> SaveAsync(ZLFileRelayConfiguration config)
    {
        // Validate configuration
        // Backup existing config
        // Save to appsettings.json
        // Notify services to reload
    }

    public async Task<ValidationResult> ValidateAsync(ZLFileRelayConfiguration config)
    {
        // Validate all paths exist
        // Check SSH key file exists
        // Validate network paths
        // Check port availability
        // Return detailed validation result
    }

    public async Task<bool> BackupAsync(string backupPath)
    {
        // Create timestamped backup
    }

    public async Task<bool> RestoreAsync(string backupPath)
    {
        // Restore from backup
    }

    public ZLFileRelayConfiguration GetDefaults()
    {
        // Return default configuration
    }
}
```

### ServiceManager.cs
```csharp
public class ServiceManager
{
    private const string ServiceName = "ZLFileRelay";

    public async Task<ServiceStatus> GetStatusAsync()
    {
        // Query Windows Service status
        // Return: Running, Stopped, NotInstalled, etc.
    }

    public async Task<bool> StartAsync()
    {
        // Start the service
        // Wait for running state
        // Return success/failure
    }

    public async Task<bool> StopAsync()
    {
        // Stop the service
        // Wait for stopped state
        // Return success/failure
    }

    public async Task<bool> RestartAsync()
    {
        // Stop then start
    }

    public async Task<bool> InstallAsync(string servicePath)
    {
        // Check admin rights
        // Run sc.exe to install
        // Configure startup type
        // Return success/failure
    }

    public async Task<bool> UninstallAsync()
    {
        // Check admin rights
        // Stop service if running
        // Run sc.exe to uninstall
        // Return success/failure
    }

    public bool IsRunningAsAdministrator()
    {
        // Check current process elevation
    }

    public async Task OpenServiceLogsAsync()
    {
        // Open logs directory in Explorer
    }
}
```

### SshKeyGenerator.cs
```csharp
public class SshKeyGenerator
{
    public async Task<SshKeyPair> GenerateAsync(
        SshKeyType type,  // ED25519 or RSA
        string outputPath,
        string? passphrase = null)
    {
        // Use ssh-keygen.exe or SSH.NET library
        // Generate private and public key
        // Save to specified path
        // Return key pair info
    }

    public async Task<string> GetPublicKeyAsync(string privateKeyPath)
    {
        // Read public key from .pub file
        // Format for display/copying
    }

    public async Task<bool> ValidateKeyAsync(string privateKeyPath)
    {
        // Check if key file is valid
        // Try to load with SSH.NET
    }
}
```

### ConnectionTester.cs
```csharp
public class ConnectionTester
{
    public async Task<ConnectionTestResult> TestSshAsync(SshSettings settings)
    {
        // Attempt SSH connection
        // Try to list directory
        // Return detailed result with error messages
    }

    public async Task<ConnectionTestResult> TestSmbAsync(SmbSettings settings)
    {
        // Attempt network connection
        // Try to access share
        // Return detailed result
    }

    public async Task<ConnectionTestResult> TestFileTransferAsync(TransferSettings settings)
    {
        // Create test file
        // Attempt actual transfer
        // Verify file on destination
        // Clean up test file
        // Return detailed result
    }
}
```

---

## 🎬 Implementation Phases

### Phase 1: Project Setup (2-4 hours)
1. **Remove old WPF project**
   ```powershell
   dotnet sln remove src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj
   Remove-Item src/ZLFileRelay.ConfigTool -Recurse
   ```

2. **Create WinUI 3 project**
   ```powershell
   cd src
   dotnet new winui -n ZLFileRelay.ConfigTool -f net8.0-windows10.0.19041.0
   cd ..
   dotnet sln add src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj
   ```

3. **Add NuGet packages**
   ```powershell
   cd src/ZLFileRelay.ConfigTool
   dotnet add reference ../ZLFileRelay.Core/ZLFileRelay.Core.csproj
   dotnet add package CommunityToolkit.Mvvm
   dotnet add package CommunityToolkit.WinUI.UI
   dotnet add package CommunityToolkit.WinUI.UI.Controls
   dotnet add package Microsoft.Extensions.DependencyInjection
   dotnet add package SSH.NET
   ```

4. **Set up project structure**
   - Create folder structure
   - Add base classes
   - Configure DI container

### Phase 2: Core Infrastructure (4-6 hours)
1. **Implement services**
   - ConfigurationService
   - ServiceManager
   - SshKeyGenerator
   - ConnectionTester
   - ValidationService

2. **Set up MVVM**
   - Base ViewModel class
   - Navigation service
   - Message passing

3. **Create main window**
   - NavigationView shell
   - Page routing
   - Title bar customization

### Phase 3: Configuration Pages (8-12 hours)
Implement each page with its ViewModel:

1. **General Page** (1-2 hours)
   - Company branding fields
   - Theme color pickers
   - Logo file browser

2. **Paths Page** (1-2 hours)
   - Directory browsers
   - Create directory buttons
   - Path validation
   - Disk space indicators

3. **Service Page** (1-2 hours)
   - Transfer method selection
   - Retry configuration
   - File filter patterns
   - Archive options

4. **Web Portal Page** (1-2 hours)
   - Authentication settings
   - User/group management
   - File size limits
   - Extension blocking

5. **SSH Page** (2-3 hours)
   - Connection settings
   - Key file browser
   - **[Generate Key]** button → dialog
   - **[Test Connection]** button → dialog
   - Public key display

6. **SMB Page** (1-2 hours)
   - Network path settings
   - Credential management
   - **[Test Connection]** button

7. **Security Page** (1 hour)
   - Security settings
   - TLS configuration
   - Audit logging

8. **Service Control Page** (2-3 hours)
   - Service status display
   - Start/Stop/Restart buttons
   - Install/Uninstall buttons
   - Log viewer
   - Open logs directory

### Phase 4: Dialogs & Advanced Features (4-6 hours)
1. **SSH Key Generation Dialog**
   - Key type selection (ED25519 / RSA)
   - Key size options
   - Passphrase (optional)
   - Output path
   - Progress indicator
   - Show public key on completion

2. **Connection Test Dialog**
   - Progress indicator
   - Real-time status updates
   - Detailed error messages
   - **[Try Again]** button
   - **[Save Configuration]** button

3. **Log Viewer Dialog**
   - Read-only text display
   - Auto-refresh option
   - Search/filter
   - Export to file

4. **About Dialog**
   - Version information
   - License information
   - Links to documentation
   - Check for updates (future)

### Phase 5: Polish & Testing (4-6 hours)
1. **Visual polish**
   - Consistent spacing
   - Proper tab order
   - Keyboard shortcuts
   - Tool tips

2. **Validation**
   - Real-time field validation
   - Clear error messages
   - Prevent invalid saves

3. **Error handling**
   - Graceful error displays
   - User-friendly messages
   - Logging all errors

4. **Testing**
   - Test all configuration changes
   - Test service management
   - Test SSH key generation
   - Test connection testing
   - Test on clean machine

---

## 📦 Sample XAML (Main Window)

```xml
<Window
    x:Class="ZLFileRelay.ConfigTool.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">

    <Grid>
        <NavigationView
            x:Name="NavView"
            IsBackButtonVisible="Collapsed"
            IsSettingsVisible="False"
            PaneDisplayMode="Left"
            SelectionChanged="NavView_SelectionChanged">
            
            <NavigationView.MenuItems>
                <NavigationViewItem Icon="Settings" Content="General" Tag="General"/>
                <NavigationViewItem Icon="Folder" Content="Paths" Tag="Paths"/>
                <NavigationViewItem Icon="Play" Content="Service" Tag="Service"/>
                <NavigationViewItem Icon="Globe" Content="Web Portal" Tag="WebPortal"/>
                <NavigationViewItem Icon="ProtectedDocument" Content="SSH/SCP" Tag="Ssh"/>
                <NavigationViewItem Icon="Permissions" Content="SMB/CIFS" Tag="Smb"/>
                <NavigationViewItem Icon="Lock" Content="Security" Tag="Security"/>
                <NavigationViewItemSeparator/>
                <NavigationViewItem Icon="Admin" Content="Service Control" Tag="Control"/>
            </NavigationView.MenuItems>

            <NavigationView.PaneFooter>
                <StackPanel Orientation="Horizontal" Margin="12">
                    <Button Content="Save" Click="SaveButton_Click" Style="{StaticResource AccentButtonStyle}"/>
                </StackPanel>
            </NavigationView.PaneFooter>

            <Frame x:Name="ContentFrame"/>
        </NavigationView>
    </Grid>
</Window>
```

---

## 🎨 Sample Page (SSH Settings)

```xml
<Page
    x:Class="ZLFileRelay.ConfigTool.Views.SshPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">

    <ScrollViewer>
        <StackPanel Spacing="16" Margin="32">
            
            <!-- Page Header -->
            <TextBlock Text="SSH/SCP Configuration" Style="{StaticResource TitleTextBlockStyle}"/>
            <TextBlock Text="Configure SSH connection settings for secure file transfer" 
                       Style="{StaticResource BodyTextBlockStyle}" Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>

            <!-- Connection Settings -->
            <Expander Header="Connection Settings" IsExpanded="True">
                <StackPanel Spacing="12" Margin="0,12,0,0">
                    
                    <TextBox Header="SSH Host" PlaceholderText="server.example.com" 
                             Text="{x:Bind ViewModel.SshHost, Mode=TwoWay}"/>
                    
                    <NumberBox Header="Port" Value="{x:Bind ViewModel.SshPort, Mode=TwoWay}" 
                               Minimum="1" Maximum="65535" SpinButtonPlacementMode="Inline"/>
                    
                    <TextBox Header="Username" PlaceholderText="username" 
                             Text="{x:Bind ViewModel.SshUsername, Mode=TwoWay}"/>
                    
                    <ComboBox Header="Authentication Method" SelectedIndex="{x:Bind ViewModel.AuthMethodIndex, Mode=TwoWay}">
                        <ComboBoxItem Content="Public Key (Recommended)"/>
                        <ComboBoxItem Content="Password"/>
                    </ComboBox>
                </StackPanel>
            </Expander>

            <!-- SSH Key Settings -->
            <Expander Header="SSH Key" IsExpanded="True" 
                      Visibility="{x:Bind ViewModel.IsPublicKeyAuth, Mode=OneWay}">
                <StackPanel Spacing="12" Margin="0,12,0,0">
                    
                    <Grid ColumnSpacing="8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <TextBox Grid.Column="0" Header="Private Key Path" 
                                 Text="{x:Bind ViewModel.PrivateKeyPath, Mode=TwoWay}"/>
                        <Button Grid.Column="1" Content="Browse..." VerticalAlignment="Bottom" 
                                Click="{x:Bind ViewModel.BrowsePrivateKey}"/>
                    </Grid>
                    
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Content="Generate New Key Pair" Style="{StaticResource AccentButtonStyle}" 
                                Click="{x:Bind ViewModel.GenerateSshKey}"/>
                        <Button Content="View Public Key" Click="{x:Bind ViewModel.ViewPublicKey}"/>
                    </StackPanel>

                    <!-- Public Key Display (collapsible) -->
                    <InfoBar x:Name="PublicKeyInfo" IsOpen="False" Severity="Informational" Title="Public Key">
                        <InfoBar.Content>
                            <StackPanel Spacing="8">
                                <TextBlock Text="Copy this public key to your server's authorized_keys file:" TextWrapping="Wrap"/>
                                <TextBox IsReadOnly="True" Text="{x:Bind ViewModel.PublicKey, Mode=OneWay}" 
                                         FontFamily="Consolas" TextWrapping="Wrap"/>
                                <Button Content="Copy to Clipboard" Click="{x:Bind ViewModel.CopyPublicKey}"/>
                            </StackPanel>
                        </InfoBar.Content>
                    </InfoBar>
                </StackPanel>
            </Expander>

            <!-- Destination Settings -->
            <Expander Header="Destination" IsExpanded="True">
                <StackPanel Spacing="12" Margin="0,12,0,0">
                    <TextBox Header="Remote Path" PlaceholderText="/data/incoming" 
                             Text="{x:Bind ViewModel.RemotePath, Mode=TwoWay}"/>
                    
                    <CheckBox Content="Create destination directory if it doesn't exist" 
                              IsChecked="{x:Bind ViewModel.CreateRemoteDirectory, Mode=TwoWay}"/>
                    
                    <CheckBox Content="Preserve file timestamps" 
                              IsChecked="{x:Bind ViewModel.PreserveTimestamps, Mode=TwoWay}"/>
                </StackPanel>
            </Expander>

            <!-- Connection Testing -->
            <Expander Header="Connection Test" IsExpanded="False">
                <StackPanel Spacing="12" Margin="0,12,0,0">
                    <TextBlock Text="Test your SSH connection to verify settings" TextWrapping="Wrap"/>
                    
                    <Button Content="Test Connection" Style="{StaticResource AccentButtonStyle}" 
                            Click="{x:Bind ViewModel.TestConnection}"/>
                    
                    <!-- Test Result -->
                    <InfoBar x:Name="TestResult" IsOpen="{x:Bind ViewModel.ShowTestResult, Mode=OneWay}" 
                             Severity="{x:Bind ViewModel.TestResultSeverity, Mode=OneWay}" 
                             Message="{x:Bind ViewModel.TestResultMessage, Mode=OneWay}"/>
                </StackPanel>
            </Expander>

        </StackPanel>
    </ScrollViewer>
</Page>
```

---

## 🚀 Quick Start Commands

### Step 1: Remove Old WPF Project
```powershell
cd C:\Users\mbecker\GitHub\ZLFileRelay

# Remove from solution
dotnet sln remove src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj

# Delete directory
Remove-Item src\ZLFileRelay.ConfigTool -Recurse -Force
```

### Step 2: Create WinUI 3 Project
```powershell
# Install templates if not already installed
dotnet new install Microsoft.WindowsAppSDK.Templates

# Create new project
cd src
dotnet new winui -n ZLFileRelay.ConfigTool -f net8.0-windows10.0.19041.0

# Add to solution
cd ..
dotnet sln add src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj

# Add dependencies
cd src/ZLFileRelay.ConfigTool
dotnet add reference ../ZLFileRelay.Core/ZLFileRelay.Core.csproj
dotnet add package CommunityToolkit.Mvvm --version 8.2.2
dotnet add package CommunityToolkit.WinUI.UI --version 7.1.2
dotnet add package CommunityToolkit.WinUI.UI.Controls --version 7.1.2
dotnet add package Microsoft.Extensions.DependencyInjection --version 8.0.0
dotnet add package SSH.NET --version 2023.0.0

# Build
dotnet build
```

---

## 📝 Development Checklist

### Project Setup
- [ ] Remove old WPF project
- [ ] Create WinUI 3 project
- [ ] Add NuGet packages
- [ ] Add reference to Core library
- [ ] Create folder structure
- [ ] Configure DI container

### Core Services
- [ ] ConfigurationService
- [ ] ServiceManager
- [ ] SshKeyGenerator
- [ ] ConnectionTester
- [ ] ValidationService
- [ ] NavigationService

### Main Window
- [ ] NavigationView shell
- [ ] Page routing
- [ ] Save button
- [ ] Status bar

### Pages
- [ ] General Settings Page
- [ ] Paths Page
- [ ] Service Page
- [ ] Web Portal Page
- [ ] SSH Page
- [ ] SMB Page
- [ ] Security Page
- [ ] Service Control Page

### Dialogs
- [ ] SSH Key Generation Dialog
- [ ] Connection Test Dialog
- [ ] Log Viewer Dialog
- [ ] About Dialog

### Custom Controls
- [ ] Path Browser Control
- [ ] Password Control
- [ ] Status Indicator

### Testing
- [ ] Manual testing all features
- [ ] Test with invalid configurations
- [ ] Test without admin rights
- [ ] Test service management
- [ ] Test SSH key generation
- [ ] Test connection testing

### Documentation
- [ ] User guide
- [ ] Screenshots
- [ ] Troubleshooting guide

---

## 🔮 Future: Remote Administration

Once the base WinUI 3 tool is complete, we can add remote administration:

### Phase 5.1: Web-Based Admin (Future)
- Create Blazor Server admin portal
- Reuse configuration services
- Add authentication/authorization
- Enable remote service management
- Add audit logging

### Phase 5.2: API for Remote Management (Future)
- Add REST API to service
- Secure with API keys
- Enable remote configuration updates
- Add health monitoring endpoints

---

## 💡 Tips for WinUI 3 Development

### Performance
- Use `x:Bind` instead of `Binding` (compiled, faster)
- Implement virtualization for large lists
- Async all file/network operations

### Design
- Follow Fluent Design principles
- Use system colors/brushes (dark mode support)
- Consistent spacing (4px grid)
- Clear visual hierarchy

### Accessibility
- Set AutomationProperties
- Support keyboard navigation
- Test with Narrator
- High contrast support

### Debugging
- Use Hot Reload for rapid development
- Enable XAML diagnostics
- Check Output window for binding errors

---

## 📚 Resources

- [WinUI 3 Gallery](https://apps.microsoft.com/store/detail/winui-3-gallery/9P3JFPWWDZRC) - Example controls
- [Windows App SDK Docs](https://learn.microsoft.com/windows/apps/windows-app-sdk/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [CommunityToolkit.WinUI](https://learn.microsoft.com/dotnet/communitytoolkit/windows/)

---

**Ready to start?** Let me know and I'll begin with Phase 1: Project Setup! 🚀


