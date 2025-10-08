# Phase 4: WinUI 3 Configuration Tool - Complete Implementation Guide

**Date:** October 8, 2025  
**Framework:** WinUI 3 (.NET 8.0)  
**Based On:** Original ZLBridge.ConfigTool (WPF .NET Framework 4.8)  
**Timeline:** 2-3 days  
**Estimated LOC:** ~3,000 lines

---

## 🎯 Features from Original Tool (Complete List)

### ✅ All Features Being Migrated

#### Tab 1: Service Management
- ✅ View service status (Running/Stopped/Not Installed)
- ✅ Refresh status button
- ✅ Install/Uninstall Windows Service
- ✅ Start/Stop/Restart service
- ✅ Configure SMB credentials (DPAPI encrypted)
- ✅ Credentials status display
- ✅ Log output viewer with clear button

#### Tab 2: Configuration
- ✅ Source path configuration
- ✅ Destination path configuration
- ✅ File stability seconds
- ✅ Processing interval seconds
- ✅ Delete source after transfer (checkbox)
- ✅ Max queue size
- ✅ Min free disk space (GB)
- ✅ Check destination disk space (checkbox)
- ✅ Verify file size after copy (checkbox)
- ✅ Create destination directories (checkbox)
- ✅ Include subdirectories (checkbox)
- ✅ File name conflict resolution (Append/Overwrite/Skip)
- ✅ Allow executable files (checkbox)
- ✅ Allow hidden files (checkbox)
- ✅ Max file size (GB)
- ✅ Save/Cancel configuration buttons
- ✅ Test SCADA connection button
- ✅ Open configuration folder button

#### Tab 3: SSH Settings
- ✅ Transfer method selection (SSH/SCP vs SMB radio buttons)
- ✅ SSH host configuration
- ✅ SSH port (default 2225)
- ✅ SSH username
- ✅ Remote destination path
- ✅ Remote server type (Windows/Linux)
- ✅ Enable SSH compression (checkbox)
- ✅ Connection timeout (seconds)
- ✅ Transfer timeout (seconds)
- ✅ Strict host key checking (checkbox)
- ✅ Private key path
- ✅ Public key path
- ✅ **Generate New SSH Keys button** (ED25519)
- ✅ Import existing keys button
- ✅ Test SSH connection button
- ✅ SSH key status display
- ✅ View public key button
- ✅ SSH operations log viewer
- ✅ Save/Cancel SSH config buttons

#### Tab 4: Service Account
- ✅ Current service account display
- ✅ Profile status display
- ✅ Change service account (domain\username)
- ✅ Set service account password
- ✅ Set service account button
- ✅ Grant "Logon as Service" right button
- ✅ Refresh status button
- ✅ Profile creation instructions
- ✅ Fix source folder permissions button
- ✅ Fix service folder permissions button
- ✅ Service account operations log

#### Additional Features
- ✅ Status bar with application version
- ✅ Admin rights detection
- ✅ Multiple log outputs
- ✅ Color-coded buttons
- ✅ Tool tips
- ✅ Input validation

---

## 🆕 Modern WinUI 3 Improvements

### Visual Enhancements
- ✨ Fluent Design System (acrylic backgrounds, reveal effects)
- ✨ NavigationView instead of TabControl (modern navigation)
- ✨ Dark mode support (automatic)
- ✨ Smooth animations
- ✨ Modern InfoBar for status messages (replaces TextBlocks)
- ✨ CommandBar for common actions
- ✨ TeachingTip for contextual help
- ✨ ContentDialog for modals (SSH key generation, etc.)
- ✨ ProgressRing for operations
- ✨ Segmented controls for radio button groups

### UX Improvements
- ✨ Real-time validation with inline error messages
- ✨ Path AutoSuggestBox with directory suggestions
- ✨ Number validation on NumberBox controls
- ✨ Password visibility toggle
- ✨ Async operations don't block UI
- ✨ Toast notifications for important actions
- ✨ Better keyboard navigation
- ✨ Accessibility improvements

### Technical Improvements
- ✨ MVVM with CommunityToolkit.Mvvm
- ✨ Dependency injection container
- ✨ Async/await throughout
- ✨ ICommand for all actions
- ✨ ObservableCollection for lists
- ✨ Property change notifications
- ✨ Unit testable ViewModels
- ✨ Configuration auto-save (optional)
- ✨ Undo/Redo support

---

## 🏗️ Complete Project Structure

```
ZLFileRelay.ConfigTool/
├── App.xaml                          # Application entry point
├── App.xaml.cs                       # Dependency injection setup
├── Package.appxmanifest              # WinUI 3 manifest
├── MainWindow.xaml                   # Shell window with NavigationView
├── MainWindow.xaml.cs                # Shell code-behind
│
├── Views/                            # All pages (8 pages)
│   ├── ServiceManagementPage.xaml   # Service control, status, logs
│   ├── ConfigurationPage.xaml       # File transfer configuration
│   ├── SshSettingsPage.xaml         # SSH configuration
│   ├── ServiceAccountPage.xaml      # Service account management
│   ├── GeneralPage.xaml             # New: Branding, theme (future)
│   ├── PathsPage.xaml               # New: Path management (future)
│   ├── SecurityPage.xaml            # New: Security settings (future)
│   └── AboutPage.xaml               # New: About, help, documentation
│
├── ViewModels/                       # MVVM ViewModels
│   ├── MainViewModel.cs             # Shell navigation
│   ├── ServiceManagementViewModel.cs
│   ├── ConfigurationViewModel.cs
│   ├── SshSettingsViewModel.cs
│   ├── ServiceAccountViewModel.cs
│   ├── GeneralViewModel.cs
│   ├── PathsViewModel.cs
│   ├── SecurityViewModel.cs
│   └── AboutViewModel.cs
│
├── Services/                         # Business logic
│   ├── ConfigurationService.cs      # Load/save configuration
│   ├── ServiceManager.cs            # Windows Service management
│   ├── SshKeyGenerator.cs           # SSH key generation
│   ├── ConnectionTester.cs          # Test SSH/SMB connections
│   ├── CredentialManager.cs         # DPAPI credential encryption
│   ├── ServiceAccountManager.cs     # Service account operations
│   ├── PermissionManager.cs         # Folder permissions
│   ├── ValidationService.cs         # Input validation
│   └── NavigationService.cs         # Page navigation
│
├── Dialogs/                          # ContentDialogs
│   ├── SshKeyGenerationDialog.xaml  # Generate SSH keys
│   ├── CredentialDialog.xaml        # SMB credentials
│   ├── ConnectionTestDialog.xaml    # Test connection progress
│   ├── PublicKeyDialog.xaml         # Show public key
│   └── ConfirmationDialog.xaml      # Generic confirmation
│
├── Controls/                         # Custom user controls
│   ├── PathBrowserControl.xaml      # TextBox + browse button
│   ├── PasswordControl.xaml         # Password with show/hide
│   ├── StatusIndicatorControl.xaml  # Service status badge
│   ├── LogViewerControl.xaml        # Reusable log viewer
│   └── SettingsCardControl.xaml     # Settings card layout
│
├── Converters/                       # XAML value converters
│   ├── BoolToVisibilityConverter.cs
│   ├── InverseBoolConverter.cs
│   ├── ServiceStatusToBrushConverter.cs
│   ├── FileSizeConverter.cs
│   └── BoolToStringConverter.cs
│
├── Helpers/                          # Utility classes
│   ├── AdminHelper.cs               # Check/request admin rights
│   ├── ProcessHelper.cs             # Run external processes (sc.exe, etc.)
│   ├── FileDialogHelper.cs          # File/folder picker helpers
│   ├── PathHelper.cs                # Path validation/manipulation
│   └── LogHelper.cs                 # Logging utilities
│
├── Models/                           # Data models
│   ├── ServiceStatus.cs             # Service status enum/class
│   ├── ConnectionTestResult.cs      # Connection test result
│   ├── SshKeyPair.cs                # SSH key pair info
│   └── ValidationResult.cs          # Validation result
│
└── Assets/                           # Resources
    ├── icon.ico                     # Application icon
    ├── Strings/                     # Localization resources
    └── Themes/                      # Custom themes
```

---

## 📋 Implementation Checklist

### Phase 1: Project Setup & Infrastructure (4-6 hours)

#### 1.1 Create New WinUI 3 Project
- [ ] Remove old WPF project from solution
- [ ] Create new WinUI 3 project
- [ ] Add to solution
- [ ] Configure target framework (net8.0-windows10.0.19041.0)
- [ ] Add reference to ZLFileRelay.Core

#### 1.2 Add NuGet Packages
```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
<PackageReference Include="CommunityToolkit.WinUI.UI" Version="7.1.2" />
<PackageReference Include="CommunityToolkit.WinUI.UI.Controls" Version="7.1.2" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
<PackageReference Include="SSH.NET" Version="2023.0.0" />
<PackageReference Include="System.Security.Cryptography.ProtectedData" Version="8.0.0" />
```

#### 1.3 Set Up Dependency Injection
- [ ] Configure service container in App.xaml.cs
- [ ] Register all services
- [ ] Register ViewModels
- [ ] Configure navigation service

#### 1.4 Create Project Structure
- [ ] Create all folders (Views, ViewModels, Services, etc.)
- [ ] Add base ViewModel class
- [ ] Add base dialog class
- [ ] Add common interfaces

### Phase 2: Core Services (6-8 hours)

#### 2.1 ConfigurationService
- [ ] Load configuration from appsettings.json
- [ ] Save configuration with validation
- [ ] Backup/restore functionality
- [ ] Get default configuration
- [ ] Configuration change notifications

#### 2.2 ServiceManager
- [ ] GetServiceStatus()
- [ ] InstallService()
- [ ] UninstallService()
- [ ] StartService()
- [ ] StopService()
- [ ] RestartService()
- [ ] IsRunningAsAdministrator()
- [ ] RequestAdminElevation()

#### 2.3 SshKeyGenerator
- [ ] GenerateKeyPair(ED25519/RSA)
- [ ] SaveKeyPair()
- [ ] LoadPublicKey()
- [ ] ValidateKeyPair()
- [ ] Use ssh-keygen.exe or SSH.NET

#### 2.4 ConnectionTester
- [ ] TestSshConnection()
- [ ] TestSmbConnection()
- [ ] TestFileTransfer()
- [ ] Return detailed ConnectionTestResult

#### 2.5 CredentialManager
- [ ] StoreCredentials() with DPAPI
- [ ] RetrieveCredentials()
- [ ] DeleteCredentials()
- [ ] TestCredentials()

#### 2.6 ServiceAccountManager
- [ ] GetCurrentServiceAccount()
- [ ] SetServiceAccount()
- [ ] GrantLogonAsServiceRight()
- [ ] CheckProfileExists()
- [ ] GetProfilePath()

#### 2.7 PermissionManager
- [ ] GrantFolderPermissions()
- [ ] CheckFolderPermissions()
- [ ] FixSourceFolderPermissions()
- [ ] FixServiceFolderPermissions()

### Phase 3: Main Window & Navigation (2-4 hours)

#### 3.1 Main Window Shell
- [ ] NavigationView with menu items
- [ ] Page frame for navigation
- [ ] Title bar customization
- [ ] CommandBar with Save button
- [ ] Status bar

#### 3.2 Navigation Service
- [ ] Navigate to page
- [ ] Back navigation
- [ ] Navigation history
- [ ] Pass parameters

### Phase 4: Service Management Page (4-6 hours)

#### 4.1 UI Layout
- [ ] Service status display (StatusIndicator control)
- [ ] Service control buttons (Install, Uninstall, Start, Stop)
- [ ] SMB credentials section
- [ ] Log viewer (LogViewer control)

#### 4.2 ViewModel
- [ ] ServiceStatus property
- [ ] InstallServiceCommand
- [ ] UninstallServiceCommand
- [ ] StartServiceCommand
- [ ] StopServiceCommand
- [ ] ConfigureCredentialsCommand
- [ ] RefreshStatusCommand
- [ ] Log output ObservableCollection

#### 4.3 Dialogs
- [ ] Credential dialog (SMB username/password/domain)
- [ ] Test connection before saving
- [ ] Save with DPAPI encryption

### Phase 5: Configuration Page (4-6 hours)

#### 5.1 UI Layout
- [ ] Source path (PathBrowser control)
- [ ] Destination path (PathBrowser control)
- [ ] File stability (NumberBox)
- [ ] Processing interval (NumberBox)
- [ ] Delete source (ToggleSwitch)
- [ ] Max queue size (NumberBox)
- [ ] Min free disk space (NumberBox)
- [ ] Advanced settings expander
- [ ] Security settings expander
- [ ] Save/Cancel buttons
- [ ] Test connection button

#### 5.2 ViewModel
- [ ] All configuration properties
- [ ] SaveConfigurationCommand
- [ ] CancelChangesCommand
- [ ] TestConnectionCommand
- [ ] OpenConfigFolderCommand
- [ ] Real-time validation
- [ ] IsDirty tracking

### Phase 6: SSH Settings Page (6-8 hours)

#### 6.1 UI Layout
- [ ] Transfer method selection (Segmented control: SSH vs SMB)
- [ ] SSH configuration section
  - [ ] Host, Port, Username
  - [ ] Remote destination path
  - [ ] Server type (Windows/Linux)
  - [ ] Compression, timeouts
  - [ ] Strict host key checking
- [ ] SSH key management section
  - [ ] Private/public key paths
  - [ ] Generate keys button
  - [ ] Import keys button
  - [ ] View public key button
  - [ ] Key status indicator
- [ ] Test SSH connection button
- [ ] SSH operations log
- [ ] Save/Cancel buttons

#### 6.2 ViewModel
- [ ] All SSH properties
- [ ] Transfer method (SSH/SMB)
- [ ] GenerateSshKeysCommand
- [ ] ImportSshKeysCommand
- [ ] TestSshConnectionCommand
- [ ] ViewPublicKeyCommand
- [ ] SaveSshConfigCommand
- [ ] SSH key status
- [ ] Log output

#### 6.3 Dialogs
- [ ] SSH key generation dialog
  - [ ] Key type selection (ED25519 recommended, RSA)
  - [ ] Key size (for RSA)
  - [ ] Passphrase (optional)
  - [ ] Output path selection
  - [ ] Progress indicator
  - [ ] Show generated public key
  - [ ] Copy public key button
- [ ] Public key viewer dialog
  - [ ] Display public key
  - [ ] Copy to clipboard
  - [ ] Instructions for adding to server
- [ ] Connection test dialog
  - [ ] Progress indicator
  - [ ] Real-time status updates
  - [ ] Success/failure message
  - [ ] Error details

### Phase 7: Service Account Page (4-6 hours)

#### 7.1 UI Layout
- [ ] Current service account display
- [ ] Profile status display
- [ ] Change service account section
  - [ ] Domain\Username
  - [ ] Password
  - [ ] Set account button
  - [ ] Grant logon right button
- [ ] Profile creation instructions
- [ ] Refresh status button
- [ ] Folder permissions section
  - [ ] Fix source permissions button
  - [ ] Fix service permissions button
- [ ] Operations log

#### 7.2 ViewModel
- [ ] CurrentServiceAccount property
- [ ] ProfileStatus property
- [ ] ServiceAccountUsername property
- [ ] ServiceAccountPassword property
- [ ] SetServiceAccountCommand
- [ ] GrantLogonRightCommand
- [ ] RefreshStatusCommand
- [ ] FixSourcePermissionsCommand
- [ ] FixServicePermissionsCommand
- [ ] Log output

### Phase 8: Additional Pages (2-4 hours each)

#### 8.1 General Page (Future - Optional)
- [ ] Company branding
- [ ] Theme customization
- [ ] Application settings

#### 8.2 Paths Page (Future - Optional)
- [ ] All path configurations
- [ ] Disk space indicators
- [ ] Create directories

#### 8.3 Security Page (Future - Optional)
- [ ] Security settings
- [ ] File type restrictions
- [ ] TLS configuration

#### 8.4 About Page
- [ ] Application version
- [ ] License information
- [ ] Documentation links
- [ ] Check for updates (future)

### Phase 9: Custom Controls (4-6 hours)

#### 9.1 PathBrowserControl
- [ ] TextBox for path
- [ ] Browse button
- [ ] Validation indicator
- [ ] Create directory option

#### 9.2 PasswordControl
- [ ] PasswordBox
- [ ] Show/hide toggle button
- [ ] Validation indicator

#### 9.3 StatusIndicatorControl
- [ ] Service status badge
- [ ] Color-coded (Running=Green, Stopped=Red, etc.)
- [ ] Icon

#### 9.4 LogViewerControl
- [ ] Read-only TextBox
- [ ] Auto-scroll option
- [ ] Clear button
- [ ] Search/filter (future)
- [ ] Export to file (future)

#### 9.5 SettingsCardControl
- [ ] Modern settings card layout
- [ ] Header, description
- [ ] Content area
- [ ] Action button area

### Phase 10: Polish & Testing (6-8 hours)

#### 10.1 Visual Polish
- [ ] Consistent spacing (8px grid)
- [ ] Proper margins/padding
- [ ] Tab order
- [ ] Keyboard shortcuts
- [ ] Tool tips
- [ ] Help text

#### 10.2 Validation
- [ ] All fields validated
- [ ] Real-time feedback
- [ ] Error messages
- [ ] Prevent invalid saves

#### 10.3 Error Handling
- [ ] Try/catch all operations
- [ ] User-friendly error messages
- [ ] Log all errors
- [ ] Graceful degradation

#### 10.4 Testing
- [ ] Test all service operations
- [ ] Test configuration save/load
- [ ] Test SSH key generation
- [ ] Test connection testing
- [ ] Test on clean machine
- [ ] Test without admin rights
- [ ] Test with invalid config

#### 10.5 Documentation
- [ ] User guide
- [ ] Screenshots
- [ ] Troubleshooting guide
- [ ] Admin guide

---

## 🚀 Quick Start Commands

### Step 1: Remove Old WPF Project & Create WinUI 3

```powershell
cd C:\Users\mbecker\GitHub\ZLFileRelay

# Remove old WPF project
dotnet sln remove src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj
Remove-Item src\ZLFileRelay.ConfigTool -Recurse -Force

# Create new WinUI 3 project
cd src
dotnet new winui -n ZLFileRelay.ConfigTool -f net8.0-windows10.0.19041.0

# Add to solution
cd ..
dotnet sln add src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj

# Add references and packages
cd src/ZLFileRelay.ConfigTool
dotnet add reference ../ZLFileRelay.Core/ZLFileRelay.Core.csproj

dotnet add package CommunityToolkit.Mvvm --version 8.2.2
dotnet add package CommunityToolkit.WinUI.UI --version 7.1.2
dotnet add package CommunityToolkit.WinUI.UI.Controls --version 7.1.2
dotnet add package Microsoft.Extensions.DependencyInjection --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration.Json --version 8.0.0
dotnet add package SSH.NET --version 2023.0.0
dotnet add package System.Security.Cryptography.ProtectedData --version 8.0.0

# Build
cd ..\..
dotnet build
```

---

## 📝 Development Notes

### Admin Rights Handling
- Detect admin rights on startup
- Show warning if not running as admin
- Disable admin-required features
- Provide "Run as Administrator" button

### SSH Key Generation
- Support ED25519 (recommended, smaller keys)
- Support RSA 4096 (legacy compatibility)
- Use Windows SSH (ssh-keygen.exe) if available
- Fall back to SSH.NET library
- Save to configurable location
- Display public key for copying

### Service Management
- Use `sc.exe` for service operations
- Handle service not installed
- Handle service running
- Proper error messages
- Restart required notifications

### Configuration Management
- Auto-save option (configurable)
- Dirty tracking
- Confirm before discarding changes
- Backup before save
- Validation before save

### Logging
- Multiple log outputs for different areas
- Auto-scroll option
- Max lines limit (performance)
- Color-coded messages (future)
- Export to file (future)

---

## 🎨 WinUI 3 Design Guidelines

### Colors
- Use system accent color
- Use semantic colors (Success, Warning, Error)
- Support light and dark themes
- Consistent color usage

### Typography
- Title: 28px
- Subtitle: 20px
- Body: 14px
- Caption: 12px

### Spacing
- Use 8px grid system
- Consistent margins
- Proper grouping

### Icons
- Use Segoe Fluent Icons
- Consistent size (16px or 20px)
- Meaningful icons

### Animations
- Subtle transitions
- Don't overuse
- Respect accessibility settings

---

## 📚 Key WinUI 3 Controls to Use

### Layout
- `NavigationView` - Main navigation
- `Grid` - Layout grid
- `StackPanel` - Vertical/horizontal stacking
- `Expander` - Collapsible sections
- `ScrollViewer` - Scrollable content

### Input
- `TextBox` - Text input
- `NumberBox` - Number input with validation
- `ComboBox` - Dropdown selection
- `ToggleSwitch` - On/off toggle
- `RadioButtons` - Mutually exclusive options
- `CheckBox` - Boolean option
- `PasswordBox` - Password input
- `AutoSuggestBox` - Auto-complete input

### Display
- `TextBlock` - Read-only text
- `InfoBar` - Status messages
- `ProgressRing` - Loading indicator
- `ProgressBar` - Progress indicator
- `Image` - Images

### Actions
- `Button` - Primary actions
- `HyperlinkButton` - Navigation links
- `ToggleButton` - Toggle state button
- `CommandBar` - Action toolbar

### Dialogs
- `ContentDialog` - Modal dialogs
- `TeachingTip` - Contextual help
- `Flyout` - Popup menus

---

## 🔮 Future Enhancements

### Phase 4+: Remote Administration
- Add REST API to service
- Web-based admin portal (Blazor)
- Remote service management
- Remote configuration updates

### Additional Features
- Configuration templates
- Multi-site management
- Health monitoring
- Statistics/analytics
- Email notifications
- Scheduled tasks

---

**Ready to start implementation?** 🚀

The plan is comprehensive but manageable. We'll build it page by page, starting with the core infrastructure and service management, then moving through each page methodically.


