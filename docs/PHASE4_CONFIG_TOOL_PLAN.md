# Phase 4: Configuration Tool - Framework Selection & Plan

**Date:** October 8, 2025  
**Status:** Planning  
**Decision Required:** UI Framework Selection

---

## 🎯 Configuration Tool Requirements

### Core Functionality
1. **Configuration Management**
   - Edit all settings from `appsettings.json`
   - Validate configuration values
   - Save/reload configuration
   - Export/import configuration

2. **Service Management**
   - Start/Stop/Restart Windows Service
   - Install/Uninstall Windows Service
   - View service status
   - Check service logs

3. **SSH Key Management**
   - Generate SSH key pairs (ED25519, RSA)
   - View public key for copying
   - Test SSH connection
   - Key file browser

4. **Connection Testing**
   - Test SSH/SCP connection
   - Test SMB/CIFS connection
   - Verify credentials
   - Test file transfer

5. **Path Management**
   - Browse for directories
   - Create directories if needed
   - Validate paths
   - Check disk space

6. **User Experience**
   - Clear validation messages
   - Real-time feedback
   - Progress indicators
   - Help/tooltips

---

## 🔍 UI Framework Comparison

### Option 1: WPF (Original Plan)
**Technology:** Windows Presentation Foundation (.NET 8.0)

#### ✅ Pros
- Native Windows performance
- Rich controls out of the box
- MVVM pattern well-established
- Excellent tooling in Visual Studio
- Large community and resources
- Mature and stable
- Good for complex data binding
- No web server required

#### ❌ Cons
- Windows-only (not cross-platform)
- XAML learning curve
- Older technology (though still supported)
- Design requires more effort for modern look
- Not as "trendy" as newer frameworks

#### 💰 Effort: Medium
**Estimated Time:** 2-3 days

#### 📦 Dependencies
```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
<PackageReference Include="ModernWpfUI" Version="0.9.6" /> <!-- Modern UI -->
```

---

### Option 2: WinUI 3 ⭐ **RECOMMENDED**
**Technology:** Modern Windows UI (.NET 8.0)

#### ✅ Pros
- **Modern, beautiful UI out of the box** (Fluent Design)
- Native Windows performance
- Microsoft's future for Windows desktop
- Better touch support
- Modern controls (acrylic, reveal, etc.)
- Still XAML-based (familiar if you know WPF)
- MVVM pattern supported
- Future-proof

#### ❌ Cons
- Windows 10 1809+ only
- Slightly less mature than WPF
- Some deployment considerations
- Still evolving (but stable now)

#### 💰 Effort: Medium
**Estimated Time:** 2-3 days (similar to WPF)

#### 📦 Dependencies
```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.5.0" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
<PackageReference Include="CommunityToolkit.WinUI.UI.Controls" Version="7.1.2" />
```

---

### Option 3: Avalonia UI
**Technology:** Cross-platform XAML (.NET 8.0)

#### ✅ Pros
- **Cross-platform** (Windows, macOS, Linux)
- XAML-based (familiar)
- Modern and actively developed
- Good performance
- Can run on web via WASM (future)
- MVVM pattern supported
- Good documentation

#### ❌ Cons
- Smaller community than WPF
- Less mature than WPF/WinUI
- Some controls may need custom implementation
- Cross-platform = more testing needed
- Deployment slightly more complex

#### 💰 Effort: Medium-High
**Estimated Time:** 3-4 days

#### 📦 Dependencies
```xml
<PackageReference Include="Avalonia" Version="11.0.6" />
<PackageReference Include="Avalonia.Desktop" Version="11.0.6" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.0.6" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
```

---

### Option 4: Blazor Hybrid (MAUI Blazor)
**Technology:** Blazor in native container (.NET 8.0)

#### ✅ Pros
- **Use existing Blazor/web skills**
- Share code with potential web admin panel
- Modern component model
- Good tooling
- Cross-platform potential
- Can reuse web portal UI components
- Rapid development

#### ❌ Cons
- Slightly heavier than native (WebView2)
- MAUI still maturing
- More memory usage
- Deployment can be complex
- Native integration requires JavaScript interop

#### 💰 Effort: Medium
**Estimated Time:** 2-3 days (if familiar with Blazor)

#### 📦 Dependencies
```xml
<PackageReference Include="Microsoft.AspNetCore.Components.WebView.Maui" Version="8.0.0" />
<PackageReference Include="Microsoft.Maui.Controls" Version="8.0.0" />
```

---

### Option 5: Electron.NET
**Technology:** Web technologies in native container

#### ✅ Pros
- Use web technologies (HTML/CSS/JavaScript + C#)
- Very flexible
- Modern UI easy to achieve
- Cross-platform
- Large ecosystem (npm packages)

#### ❌ Cons
- **Heavy** (entire Chromium bundled)
- Large download size (~150MB+)
- Higher memory usage
- Slower startup
- More complex deployment
- Overkill for this use case

#### 💰 Effort: Medium-High
**Estimated Time:** 3-4 days

#### ❌ **Not Recommended** - Too heavy for a simple config tool

---

## 🏆 Recommendation: WinUI 3

### Why WinUI 3?

1. **Modern & Beautiful**
   - Fluent Design System out of the box
   - Professional appearance with minimal effort
   - Matches Windows 11 aesthetic

2. **Native Performance**
   - Lightweight
   - Fast startup
   - Low memory usage

3. **Future-Proof**
   - Microsoft's direction for Windows desktop
   - Active development
   - Long-term support

4. **Familiar Development**
   - XAML-based (like WPF)
   - MVVM pattern
   - Similar to WPF but better

5. **Good Balance**
   - Not too old (WPF)
   - Not too experimental (Avalonia, MAUI)
   - Not too heavy (Electron)

### Fallback: WPF with ModernWPF
If WinUI 3 presents issues, WPF with ModernWPF library gives us:
- Modern Fluent Design
- Rock-solid stability
- Same development effort
- Easy migration path

---

## 📋 Detailed Feature Plan

### Main Window Layout

```
┌─────────────────────────────────────────────────────────────┐
│  ZL File Relay Configuration                           _ □ X │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────┐  ┌──────────────────────────────────────────┐ │
│  │          │  │                                          │ │
│  │ General  │  │  Configuration Sections                  │ │
│  │          │  │                                          │ │
│  ├──────────┤  │  Company Name: [Your Company Name      ] │ │
│  │ Paths    │  │  Product Name: [ZL File Relay          ] │ │
│  │          │  │  Site Name:    [Your Site Name         ] │ │
│  ├──────────┤  │                                          │ │
│  │ Service  │  │  [More configuration fields...]          │ │
│  │          │  │                                          │ │
│  ├──────────┤  │                                          │ │
│  │ Web      │  │                                          │ │
│  │ Portal   │  │                                          │ │
│  │          │  │                                          │ │
│  ├──────────┤  │                                          │ │
│  │ SSH/SCP  │  │                                          │ │
│  │          │  │                                          │ │
│  ├──────────┤  │                                          │ │
│  │ SMB/CIFS │  │                                          │ │
│  │          │  │                                          │ │
│  ├──────────┤  │                                          │ │
│  │ Security │  │                                          │ │
│  │          │  │                                          │ │
│  ├──────────┤  │                                          │ │
│  │ Service  │  │                                          │ │
│  │ Control  │  │                                          │ │
│  │          │  │                                          │ │
│  └──────────┘  └──────────────────────────────────────────┘ │
│                                                               │
│  [Save Configuration]  [Test Connection]  [View Logs]        │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### Configuration Sections

#### 1. General Settings
- Company Name
- Product Name
- Site Name
- Support Email
- Logo Path (with file browser)
- Theme Colors (color pickers)

#### 2. Paths
- Upload Directory (browser + create)
- Transfer Directory (browser + create)
- Log Directory (browser + create)
- Config Directory (browser + create)
- Archive Directory (browser + create)

#### 3. Service Settings
- Enable/Disable Service
- Watch Directory
- Transfer Method (SSH/SMB dropdown)
- Retry Attempts
- Retry Delay
- Max Concurrent Transfers
- File Filter
- Delete/Archive after transfer
- Verify Transfer

#### 4. Web Portal Settings
- Enable/Disable Portal
- Require Authentication
- Allowed Groups (list editor)
- Allowed Users (list editor)
- Max File Size (with units)
- Blocked Extensions (list editor)
- Enable Upload to Transfer

#### 5. SSH/SCP Settings
- Host
- Port
- Username
- Auth Method (dropdown)
- Private Key Path (browser)
- **[Generate SSH Key]** button
- Password (encrypted, show/hide)
- Destination Path
- Connection Timeout
- **[Test SSH Connection]** button

#### 6. SMB/CIFS Settings
- Server
- Share Path
- Use Credentials (checkbox)
- Username
- Password (encrypted, show/hide)
- Domain
- **[Test SMB Connection]** button

#### 7. Security Settings
- Encrypt Credentials
- Require Secure Transfer
- Allowed Cipher Suites
- Min TLS Version
- Enable Audit Log
- Sensitive Data Masking

#### 8. Service Control
- Current Status (running/stopped)
- **[Start Service]** button
- **[Stop Service]** button
- **[Restart Service]** button
- **[Install as Windows Service]** button
- **[Uninstall Service]** button
- **[Open Service Logs]** button
- **[Open Web Portal Logs]** button

---

## 🛠️ Implementation Plan

### Phase 4.1: Project Setup (2-4 hours)
- [ ] Create `ZLFileRelay.ConfigTool` project (WinUI 3)
- [ ] Add NuGet packages
- [ ] Set up project structure
- [ ] Configure dependency injection
- [ ] Add reference to `ZLFileRelay.Core`

### Phase 4.2: Core Infrastructure (4-6 hours)
- [ ] Create ViewModels for each section
- [ ] Implement configuration loading/saving
- [ ] Add validation logic
- [ ] Set up navigation framework
- [ ] Implement MVVM pattern

### Phase 4.3: UI Implementation (8-12 hours)
- [ ] Main window with navigation
- [ ] General settings page
- [ ] Paths settings page
- [ ] Service settings page
- [ ] Web portal settings page
- [ ] SSH/SCP settings page
- [ ] SMB/CIFS settings page
- [ ] Security settings page
- [ ] Service control page

### Phase 4.4: Advanced Features (6-8 hours)
- [ ] SSH key generation dialog
- [ ] SSH connection test
- [ ] SMB connection test
- [ ] Log viewer window
- [ ] Service installer/uninstaller
- [ ] Configuration import/export

### Phase 4.5: Testing & Polish (4-6 hours)
- [ ] Test all configuration changes
- [ ] Test service control
- [ ] Test SSH key generation
- [ ] Test connection testing
- [ ] Polish UI/UX
- [ ] Add help text/tooltips

### Total Estimated Time: 24-36 hours (3-4 days)

---

## 📦 Project Structure

```
ZLFileRelay.ConfigTool/
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── Views/
│   ├── GeneralSettingsPage.xaml
│   ├── PathsSettingsPage.xaml
│   ├── ServiceSettingsPage.xaml
│   ├── WebPortalSettingsPage.xaml
│   ├── SshSettingsPage.xaml
│   ├── SmbSettingsPage.xaml
│   ├── SecuritySettingsPage.xaml
│   └── ServiceControlPage.xaml
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── GeneralSettingsViewModel.cs
│   ├── PathsSettingsViewModel.cs
│   ├── ServiceSettingsViewModel.cs
│   ├── WebPortalSettingsViewModel.cs
│   ├── SshSettingsViewModel.cs
│   ├── SmbSettingsViewModel.cs
│   ├── SecuritySettingsViewModel.cs
│   └── ServiceControlViewModel.cs
├── Services/
│   ├── ConfigurationService.cs
│   ├── ServiceManagementService.cs
│   ├── SshKeyGenerationService.cs
│   ├── ConnectionTestService.cs
│   └── ValidationService.cs
├── Dialogs/
│   ├── SshKeyGenerationDialog.xaml
│   ├── ConnectionTestDialog.xaml
│   └── LogViewerDialog.xaml
├── Converters/
│   ├── BoolToVisibilityConverter.cs
│   └── FileSizeConverter.cs
└── Assets/
    └── (icons, images)
```

---

## 🎨 UI Design Guidelines

### Colors (Following Existing Theme)
- Primary: `#0066CC` (from config)
- Secondary: `#003366` (from config)
- Accent: `#FF6600` (from config)
- Background: System default (Fluent)
- Text: System default (Fluent)

### Typography
- Headers: Segoe UI, 20pt
- Subheaders: Segoe UI, 16pt
- Body: Segoe UI, 14pt
- Captions: Segoe UI, 12pt

### Spacing
- Section padding: 24px
- Control spacing: 8px
- Group spacing: 16px
- Page margins: 32px

### Controls
- Use WinUI 3 Fluent controls
- Consistent validation states
- Clear error messages
- Progress indicators for long operations
- Confirm dialogs for destructive actions

---

## 🔧 Key Services to Implement

### ConfigurationService
```csharp
public class ConfigurationService
{
    Task<ZLFileRelayConfiguration> LoadConfigurationAsync();
    Task SaveConfigurationAsync(ZLFileRelayConfiguration config);
    Task<bool> ValidateConfigurationAsync(ZLFileRelayConfiguration config);
    Task ExportConfigurationAsync(string path);
    Task ImportConfigurationAsync(string path);
}
```

### ServiceManagementService
```csharp
public class ServiceManagementService
{
    Task<ServiceStatus> GetServiceStatusAsync();
    Task StartServiceAsync();
    Task StopServiceAsync();
    Task RestartServiceAsync();
    Task InstallServiceAsync(string servicePath);
    Task UninstallServiceAsync();
    Task<bool> IsRunningAsAdministratorAsync();
}
```

### SshKeyGenerationService
```csharp
public class SshKeyGenerationService
{
    Task<SshKeyPair> GenerateKeyPairAsync(SshKeyType type, string path);
    Task<string> GetPublicKeyAsync(string privateKeyPath);
    Task<bool> ValidateKeyPairAsync(string privateKeyPath);
}
```

### ConnectionTestService
```csharp
public class ConnectionTestService
{
    Task<ConnectionTestResult> TestSshConnectionAsync(SshSettings settings);
    Task<ConnectionTestResult> TestSmbConnectionAsync(SmbSettings settings);
    Task<ConnectionTestResult> TestFileTransferAsync(TransferSettings settings);
}
```

---

## 🚀 Quick Start Commands

### Create WinUI 3 Project
```powershell
# Install WinUI 3 templates (if not already installed)
dotnet new install Microsoft.WindowsAppSDK.Templates

# Create new WinUI 3 project
cd src
dotnet new winui -n ZLFileRelay.ConfigTool -f net8.0-windows10.0.19041.0

# Add to solution
cd ..
dotnet sln add src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj

# Add dependencies
cd src/ZLFileRelay.ConfigTool
dotnet add reference ../ZLFileRelay.Core/ZLFileRelay.Core.csproj
dotnet add package CommunityToolkit.Mvvm
dotnet add package CommunityToolkit.WinUI.UI.Controls
```

### Create WPF Project (Fallback)
```powershell
# Create new WPF project
cd src
dotnet new wpf -n ZLFileRelay.ConfigTool -f net8.0-windows

# Add to solution
cd ..
dotnet sln add src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj

# Add dependencies
cd src/ZLFileRelay.ConfigTool
dotnet add reference ../ZLFileRelay.Core/ZLFileRelay.Core.csproj
dotnet add package CommunityToolkit.Mvvm
dotnet add package ModernWpfUI
```

---

## 📊 Comparison Matrix

| Feature | WPF | WinUI 3 | Avalonia | Blazor Hybrid |
|---------|-----|---------|----------|---------------|
| Native Performance | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ |
| Modern UI | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| Development Speed | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| Maturity | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ |
| Cross-Platform | ❌ | ❌ | ✅ | ✅ |
| App Size | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ |
| Future-Proof | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ |
| **Overall** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ |

---

## 🎯 Final Recommendation

### Primary: WinUI 3
**Reasons:**
1. Modern, professional UI with minimal effort
2. Microsoft's future direction
3. Excellent performance
4. Good development experience
5. Future-proof

### Backup: WPF with ModernWPF
**If WinUI 3 issues arise:**
1. Rock-solid stability
2. Can still look modern
3. More mature ecosystem
4. Easier deployment

### Not Recommended:
- ❌ Electron.NET (too heavy)
- ⚠️ Blazor Hybrid (MAUI still maturing, overkill)
- ⚠️ Avalonia (unless cross-platform truly needed)

---

## 📝 Next Steps

1. **Decide on framework** (WinUI 3 recommended)
2. **Create project structure**
3. **Implement core infrastructure**
4. **Build UI page by page**
5. **Test thoroughly**
6. **Polish and document**

---

## 💡 Alternative: Web-Based Config Tool

### If Native GUI is Too Complex

We could also build a **simple web-based configuration tool**:
- ASP.NET Core Blazor Server
- Runs locally on `localhost:5142`
- Reuse web portal UI components
- Access via browser
- Still manages local service
- Simpler deployment

**Pros:**
- Rapid development
- Reuse existing Bootstrap UI
- Can be remote (with security)
- Easy to maintain

**Cons:**
- Requires web server running
- Less "native" feel
- Authentication/security concerns if remote

This could be a **Phase 4 Alternative** if native GUI proves problematic.

---

**Decision Required:** Which framework should we use?

**Recommendation:** Start with **WinUI 3** for best modern experience.


