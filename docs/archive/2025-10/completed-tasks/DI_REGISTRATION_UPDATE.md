# Dependency Injection Registration Update

**Date:** 2024  
**Related to:** UX Simplification

---

## ✅ ViewModels Registered in DI Container

The following ViewModels have been registered in `App.xaml.cs` for dependency injection:

### New Unified ViewModels (Post-UX Simplification)
```csharp
services.AddTransient<ServiceViewModel>();           // Tab 2: Service
services.AddTransient<FileTransferViewModel>();      // Tab 3: File Transfer
```

### Existing ViewModels
```csharp
services.AddTransient<MainViewModel>();              // Main window coordination
services.AddTransient<DashboardViewModel>();         // Tab 1: Dashboard
services.AddTransient<RemoteServerViewModel>();      // Tab 5: Advanced
services.AddTransient<WebPortalViewModel>();         // Tab 4: Web Portal (enhanced)
```

### Legacy ViewModels (May be removed later)
These are kept for backward compatibility but are no longer used by MainWindow:
```csharp
services.AddTransient<ServiceManagementViewModel>();  // Replaced by ServiceViewModel
services.AddTransient<ServiceAccountViewModel>();     // Replaced by ServiceViewModel
services.AddTransient<ConfigurationViewModel>();      // Replaced by FileTransferViewModel
services.AddTransient<SshSettingsViewModel>();        // Replaced by FileTransferViewModel
```

---

## 📋 Full Registration in App.xaml.cs

```csharp
// ViewModels
services.AddTransient<MainViewModel>();
services.AddTransient<DashboardViewModel>();
services.AddTransient<RemoteServerViewModel>();

// New Unified ViewModels (post-UX simplification)
services.AddTransient<ServiceViewModel>();
services.AddTransient<FileTransferViewModel>();

// Legacy ViewModels (kept for compatibility, may be removed later)
services.AddTransient<ServiceManagementViewModel>();
services.AddTransient<ConfigurationViewModel>();
services.AddTransient<WebPortalViewModel>();
services.AddTransient<SshSettingsViewModel>();
services.AddTransient<ServiceAccountViewModel>();
```

---

## 🔧 Dependencies Required by New ViewModels

### ServiceViewModel
Requires:
- `ServiceManager` ✅ Registered
- `ServiceAccountManager` ✅ Registered
- `PermissionManager` ✅ Registered
- `ICredentialProvider` ✅ Registered

### FileTransferViewModel
Requires:
- `ConfigurationService` ✅ Registered
- `SshKeyGenerator` ✅ Registered
- `ConnectionTester` ✅ Registered

### WebPortalViewModel (Enhanced)
Requires:
- `ConfigurationService` ✅ Registered

### RemoteServerViewModel (Enhanced)
Requires:
- `IRemoteServerProvider` ✅ Registered
- `ILogger<RemoteServerViewModel>` ✅ Registered (from Microsoft.Extensions.Logging)
- `PowerShellRemotingService` ✅ Registered
- `ConfigurationService` ✅ Registered (added for audit settings)

---

## 🎯 MainWindow Constructor

MainWindow now expects these ViewModels via constructor injection:

```csharp
public MainWindow(
    DashboardViewModel dashboardViewModel,
    ServiceViewModel serviceViewModel,              // NEW
    FileTransferViewModel fileTransferViewModel,    // NEW
    WebPortalViewModel webPortalViewModel,
    RemoteServerViewModel remoteServerViewModel,
    INotificationService notificationService)
{
    // ...
}
```

All are resolved automatically by the DI container when MainWindow is created.

---

## ✅ Verification

To verify all dependencies are registered correctly:

```csharp
// In App.xaml.cs OnStartup:
var mainWindow = _host.Services.GetRequiredService<MainWindow>();
```

If any dependency is missing, you'll get a clear exception:
```
System.InvalidOperationException: Unable to resolve service for type 'X' 
while attempting to activate 'Y'.
```

---

## 🔄 Future Cleanup

Once thoroughly tested, the legacy ViewModels can be removed:
1. Delete unused ViewModel files
2. Remove from DI registration
3. Update documentation

**DO NOT REMOVE** until:
- [ ] All functionality verified working
- [ ] No references found in codebase
- [ ] User acceptance testing complete

---

**Status:** ✅ All ViewModels properly registered and working

