# ZL File Relay Config Tool - Tab Reference Guide

Quick reference for developers working with the simplified 6-tab structure.

---

## 📋 Tab Overview

| # | Tab | Icon | View | ViewModel | Purpose |
|---|-----|------|------|-----------|---------|
| 1 | Dashboard | 🏠 `&#xE80F;` | `DashboardView.xaml` | `DashboardViewModel` | System overview & health |
| 2 | Service | ⚙️ `&#xE950;` | `ServiceView.xaml` | `ServiceViewModel` | Service & account management |
| 3 | File Transfer | 📁 `&#xE8B5;` | `FileTransferView.xaml` | `FileTransferViewModel` | Transfer config & security |
| 4 | Web Portal | 🌐 `&#xE774;` | `WebPortalView.xaml` | `WebPortalViewModel` | Web upload interface config |
| 5 | Advanced | 🔧 `&#xE713;` | `RemoteServerView.xaml` | `RemoteServerViewModel` | Remote mgmt & audit logging |
| 6 | About | ℹ️ `&#xE946;` | `AboutView.xaml` | (standalone) | Version, branding, info |

---

## 🗂️ File Locations

```
src/ZLFileRelay.ConfigTool/
├── MainWindow.xaml                    ← Main window (6 tabs)
├── MainWindow.xaml.cs                 ← View initialization & wiring
├── Views/
│   ├── DashboardView.xaml            ← Tab 1: Dashboard
│   ├── ServiceView.xaml              ← Tab 2: Service ⭐ NEW
│   ├── FileTransferView.xaml         ← Tab 3: File Transfer ⭐ NEW
│   ├── WebPortalView.xaml            ← Tab 4: Web Portal ⭐ NEW
│   ├── RemoteServerView.xaml         ← Tab 5: Advanced (enhanced)
│   └── AboutView.xaml                ← Tab 6: About
└── ViewModels/
    ├── DashboardViewModel.cs
    ├── ServiceViewModel.cs           ← Tab 2 ViewModel ⭐ NEW
    ├── FileTransferViewModel.cs      ← Tab 3 ViewModel ⭐ NEW
    ├── WebPortalViewModel.cs         ← Tab 4 ViewModel (enhanced)
    ├── RemoteServerViewModel.cs      ← Tab 5 ViewModel (enhanced)
    └── MainViewModel.cs
```

---

## 🎯 What's In Each Tab

### 1️⃣ Dashboard
**Purpose:** System health, statistics, recent activity

**Content:**
- Overall system health (Healthy/Warning/Error)
- Service status card
- Transfer statistics
- Recent activity log
- Quick action buttons

**ViewModel:** `DashboardViewModel`  
**Key Methods:** `AddActivity()`, `RefreshStatistics()`

---

### 2️⃣ Service (⭐ NEW - Merged)
**Purpose:** Service management + service account

**Previously:** "Service Management" + "Service Account" tabs  
**Now:** Unified in one tab

**Sections:**
1. **Service Status** - Current state with auto-refresh
2. **Service Controls** - Install, Uninstall, Start, Stop
3. **Service Account** - View and change account
4. **Change Service Account** - Domain, username, password
5. **Folder Permissions** - Fix upload/log/all permissions
6. **SMB Credentials** - Configure SMB authentication
7. **Activity Log** - Real-time operations log

**ViewModel:** `ServiceViewModel` (NEW)  
**Merges:** `ServiceManagementViewModel` + `ServiceAccountViewModel`

**Key Commands:**
- `InstallServiceCommand`
- `StartServiceCommand` / `StopServiceCommand`
- `ApplyAccountChangeCommand`
- `GrantLogonRightsCommand`
- `FixUploadPermissionsCommand` / `FixLogPermissionsCommand` / `FixAllPermissionsCommand`
- `ConfigureSmbCredentialsCommand`

---

### 3️⃣ File Transfer (⭐ NEW - Unified)
**Purpose:** Transfer configuration + SSH/SMB + security policies

**Previously:** "Configuration Settings" + "SSH/Transfer" + "Security" tabs  
**Now:** Unified with contextual UI

**Sections:**
1. **Transfer Method** - Radio buttons: SSH or SMB
2. **Directories** - Watch directory, archive directory
3. **SSH Configuration** *(shows when SSH selected)*
   - Host, port, username, destination
   - Key management (generate, view, copy)
   - Connection test
4. **SMB Configuration** *(shows when SMB selected)*
   - Server, share path
   - Connection test
5. **File Handling** - Archive, delete, verify options
6. **Security Policies** *(always visible)*
   - Allow executable files (with warning)
   - Allow hidden files
   - Max file size (1-100 GB slider)
   - Security posture summary
7. **Save Configuration** button

**ViewModel:** `FileTransferViewModel` (NEW)  
**Merges:** `ConfigurationViewModel` + `SshSettingsViewModel` + Security settings

**Key Properties:**
- `IsSshMethod` / `IsSmbMethod` - Controls contextual visibility
- `WatchDirectory`, `ArchiveDirectory`
- `SshHost`, `SshPort`, `SshUsername`, `SshKeyPath`, `SshDestinationPath`
- `SmbServer`, `SmbSharePath`
- `AllowExecutableFiles`, `AllowHiddenFiles`, `MaxFileSizeGB`

**Key Commands:**
- `GenerateSshKeyCommand`
- `TestSshConnectionCommand`
- `TestSmbConnectionCommand`
- `SaveConfigurationCommand`

---

### 4️⃣ Web Portal (Enhanced)
**Purpose:** Web upload interface configuration

**Sections:**
1. **Server Configuration** - HTTP/HTTPS ports
2. **SSL Certificate** - Path, password, test
3. **Authentication** - Windows auth, allowed groups
4. **Upload Settings** - Upload directory, max size
5. **Branding** - Preview (configured in About tab)
6. **Actions** - Save & Restart buttons

**ViewModel:** `WebPortalViewModel` (Enhanced)

**Key Properties:**
- `HttpPort`, `HttpsPort`, `EnableHttps`
- `CertificatePath`, `CertificateStatus`
- `RequireAuthentication`, `AllowedGroups`
- `UploadDirectory`, `MaxUploadSizeGB`

**Key Commands:**
- `BrowseCertificateCommand`
- `TestCertificateCommand`
- `BrowseUploadDirectoryCommand`
- `SaveConfigurationCommand`
- `RestartWebServiceCommand`

---

### 5️⃣ Advanced (Enhanced)
**Purpose:** Remote management + audit logging + diagnostics

**Sections:**
1. **Connection Mode** - Local vs Remote
2. **Remote Server Settings** - Server name, credentials
3. **Connection Test** - Verify connectivity
4. **Connection Status** - Connect/Disconnect
5. **Audit & Compliance** ⭐ NEW
   - Enable audit logging
   - Audit log path
   - Retention days
6. **Requirements Info** - Documentation

**ViewModel:** `RemoteServerViewModel` (Enhanced)

**New Properties:**
- `EnableAuditLog`
- `AuditLogPath`
- `AuditLogRetentionDays`

**New Commands:**
- `BrowseAuditLogCommand`
- `SaveAuditSettingsCommand`

---

### 6️⃣ About
**Purpose:** Version info, branding, system info

**Content:**
- Product name and version
- Company branding configuration
- System information
- Links to documentation
- Support contact

**ViewModel:** None (standalone view)

---

## 🔧 Adding a New Setting

### Example: Add "Enable Notifications" to File Transfer Tab

1. **Add Property to ViewModel**
```csharp
// FileTransferViewModel.cs
[ObservableProperty] 
private bool _enableNotifications = true;
```

2. **Add UI Element to View**
```xml
<!-- FileTransferView.xaml -->
<CheckBox Content="Enable transfer notifications"
          IsChecked="{Binding EnableNotifications}"/>
```

3. **Update Configuration Loading**
```csharp
// FileTransferViewModel.cs - LoadFromConfiguration()
EnableNotifications = _config.Service.EnableNotifications;
```

4. **Update Configuration Saving**
```csharp
// FileTransferViewModel.cs - UpdateConfiguration()
_config.Service.EnableNotifications = EnableNotifications;
```

---

## 🎨 Contextual UI Pattern

### Example: Show/Hide Based on Selection

**XAML:**
```xml
<!-- Show this section only when SSH is selected -->
<ui:SimpleStackPanel Visibility="{Binding IsSshMethod, Converter={StaticResource BoolToVisibilityConverter}}">
    <!-- SSH-specific UI here -->
</ui:SimpleStackPanel>
```

**ViewModel:**
```csharp
[ObservableProperty] 
private bool _isSshMethod = true;

[ObservableProperty] 
private bool _isSmbMethod = false;

partial void OnIsSshMethodChanged(bool value)
{
    if (value) IsSmbMethod = false;
}
```

---

## 🧪 Testing a View

```csharp
// In MainWindow.xaml.cs InitializeViews()

// Your new view
ServiceContent.Content = new ServiceView
{
    DataContext = _serviceViewModel  // ViewModel injected via DI
};
```

To test in isolation:
```csharp
var window = new Window
{
    Content = new ServiceView
    {
        DataContext = new ServiceViewModel(...)
    }
};
window.ShowDialog();
```

---

## 📚 Common Patterns

### Command with Parameter
```xml
<!-- XAML -->
<Button Command="{Binding ApplyAccountChangeCommand}"
        CommandParameter="{Binding ElementName=PasswordBox}"/>
```

```csharp
// ViewModel
[RelayCommand]
private async Task ApplyAccountChangeAsync(PasswordBox passwordBox)
{
    var password = passwordBox?.Password;
    // ...
}
```

### Browse for File/Folder
```csharp
[RelayCommand]
private void BrowseFolder()
{
    var dialog = new CommonOpenFileDialog
    {
        IsFolderPicker = true,
        Title = "Select Directory"
    };
    
    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
    {
        MyDirectory = dialog.FileName;
    }
}
```

### Validation
```csharp
[RelayCommand]
private async Task SaveConfigurationAsync()
{
    var validationResult = await _configurationService.ValidateAsync(_config);
    
    if (!validationResult.IsValid)
    {
        StatusMessage = $"❌ Validation failed:\n{string.Join("\n", validationResult.Errors)}";
        return;
    }
    
    // Save...
}
```

---

## 🚀 Performance Tips

1. **Auto-Refresh Timers:** Use 5-second intervals, dispose on window close
2. **Log Limits:** Keep activity logs to 100-200 items max
3. **Lazy Loading:** Load heavy operations only when tab is activated
4. **Async Everything:** All I/O operations should be async

---

## 📞 Need Help?

- **Documentation:** `docs/UX_SIMPLIFICATION_PLAN.md`
- **Implementation Summary:** `docs/UX_SIMPLIFICATION_COMPLETE.md`
- **Code Questions:** Review existing view models for patterns
- **MVVM Basics:** CommunityToolkit.Mvvm documentation

---

**Last Updated:** After UX Simplification (2024)  
**Maintainer:** ZL File Relay Team

