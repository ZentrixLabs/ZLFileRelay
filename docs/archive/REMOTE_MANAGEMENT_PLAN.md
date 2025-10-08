# ZL File Relay - Remote Management Capability Plan

## 🎯 Objective

Enable the ConfigTool to manage ZL File Relay installations on remote servers, particularly Windows Server Core installations where GUI applications cannot run locally.

---

## 📋 Current State Analysis

### What Works Locally ✅
- ConfigurationService - Reads/writes appsettings.json
- ServiceManager - Uses sc.exe and ServiceController
- SshKeyGenerator - Uses ssh-keygen.exe
- ConnectionTester - Tests SSH connections (already remote)
- ServiceAccountManager - Manages service accounts
- PermissionManager - Manages NTFS permissions

### What Needs Remote Support 🔧
- ✅ ConnectionTester - Already tests remote SSH servers
- ❌ ConfigurationService - Currently only local file access
- ❌ ServiceManager - Currently only local service control
- ❌ ServiceAccountManager - Currently only local service accounts
- ❌ PermissionManager - Currently only local file permissions
- ❌ SshKeyGenerator - Currently only local key generation

---

## 🏗️ Architecture Options

### Option 1: File Share + Remote Service Control (Simplest) ⭐ **RECOMMENDED**

**Pros:**
- Simple implementation
- No additional services required
- Uses existing Windows infrastructure
- Works with Server Core out of the box

**Cons:**
- Requires file share access
- Requires admin credentials
- Some features limited remotely

**Implementation:**
```
ConfigTool (Workstation)
    ↓
UNC Path: \\server\c$\path\to\appsettings.json  (Configuration)
    ↓
sc.exe \\server ...                              (Service Control)
    ↓
Remote SSH Test                                  (Connection Testing)
```

### Option 2: REST API in WebPortal (Most Scalable)

**Pros:**
- Clean API design
- Secure authentication
- Works across network segments
- No file share required

**Cons:**
- Requires WebPortal to be running
- More complex implementation
- Additional security considerations

**Implementation:**
```
ConfigTool (Workstation)
    ↓
HTTPS API: https://server:8443/api/config       (Configuration)
    ↓
HTTPS API: https://server:8443/api/service      (Service Control)
    ↓
HTTPS API: https://server:8443/api/ssh          (SSH Testing)
```

### Option 3: PowerShell Remoting (Most Powerful)

**Pros:**
- Full remote capabilities
- Native Windows feature
- Secure (Kerberos/NTLM)

**Cons:**
- Requires PS Remoting enabled
- Complex credential management
- May be blocked by firewalls

---

## 📊 Recommended Approach: Hybrid Solution

**Phase 1: File Share + Remote Services** (Quick Win)
- Support remote configuration via UNC paths
- Support remote service control via sc.exe \\server
- Keep local features for everything else

**Phase 2: REST API** (Future Enhancement)
- Add config API to WebPortal
- Enable full remote management
- Support multi-server management

---

## 🔧 Implementation Plan - Phase 1

### 1. Add Remote Server Connection UI

**New ViewModel:** `RemoteServerViewModel.cs`

```csharp
public partial class RemoteServerViewModel : ObservableObject
{
    [ObservableProperty] private string _serverName = "localhost";
    [ObservableProperty] private bool _isRemote = false;
    [ObservableProperty] private string _remoteUsername = string.Empty;
    [ObservableProperty] private string _remotePassword = string.Empty;
    [ObservableProperty] private bool _useCurrentCredentials = true;
    [ObservableProperty] private string _connectionStatus = "Not Connected";
    
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        // Test: Can we access \\server\c$?
        // Test: Can we query service on server?
        // Test: Can we access remote registry?
    }
    
    [RelayCommand]
    private async Task ConnectAsync()
    {
        // Establish connection to remote server
        // Store credentials if needed
        // Update all services with server name
    }
}
```

**New UI Tab:** "Remote Server" (first tab)
```
┌─────────────────────────────────────────┐
│ Connect to Remote Server               │
├─────────────────────────────────────────┤
│ Server Name: [_____________]            │
│ ○ Local Machine (localhost)            │
│ ● Remote Server                         │
│                                         │
│ ☑ Use current Windows credentials      │
│ Username: [___________]                 │
│ Password: [___________]                 │
│                                         │
│ [Test Connection] [Connect]             │
│                                         │
│ Status: ✅ Connected to SERVER01        │
└─────────────────────────────────────────┘
```

---

### 2. Update ConfigurationService for Remote Support

**Changes to `ConfigurationService.cs`:**

```csharp
public class ConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private string _configPath;
    private string? _remoteServer;
    private ZLFileRelayConfiguration? _currentConfiguration;

    public void SetRemoteServer(string? serverName)
    {
        _remoteServer = serverName;
        UpdateConfigPath();
    }

    private void UpdateConfigPath()
    {
        if (string.IsNullOrEmpty(_remoteServer) || _remoteServer == "localhost")
        {
            // Local path
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        }
        else
        {
            // Remote UNC path - assume standard install location
            _configPath = $@"\\{_remoteServer}\c$\Program Files\ZLFileRelay\appsettings.json";
        }
    }

    public async Task<ZLFileRelayConfiguration> LoadAsync()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                // Try alternate location
                if (!string.IsNullOrEmpty(_remoteServer))
                {
                    _configPath = $@"\\{_remoteServer}\ZLFileRelay$\appsettings.json";
                }
            }
            
            var json = await File.ReadAllTextAsync(_configPath);
            // ... rest of load logic
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError("Access denied to {Path}. Ensure you have permissions.", _configPath);
            throw;
        }
    }
}
```

---

### 3. Update ServiceManager for Remote Support

**Changes to `ServiceManager.cs`:**

```csharp
public class ServiceManager
{
    private readonly ILogger<ServiceManager> _logger;
    private string _serviceName = "ZLFileRelay";
    private string? _remoteServer;

    public void SetRemoteServer(string? serverName)
    {
        _remoteServer = serverName;
    }

    public Task<ServiceStatus> GetStatusAsync()
    {
        try
        {
            var machineName = string.IsNullOrEmpty(_remoteServer) ? "." : _remoteServer;
            using var service = new ServiceController(_serviceName, machineName);
            
            var status = service.Status switch
            {
                ServiceControllerStatus.Running => ServiceStatus.Running,
                ServiceControllerStatus.Stopped => ServiceStatus.Stopped,
                // ... rest of mapping
            };
            
            return Task.FromResult(status);
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult(ServiceStatus.NotInstalled);
        }
    }

    public Task<bool> StartAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                var machineName = string.IsNullOrEmpty(_remoteServer) ? "." : _remoteServer;
                using var service = new ServiceController(_serviceName, machineName);
                
                if (service.Status == ServiceControllerStatus.Running)
                {
                    return true;
                }
                
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start service on {Server}", _remoteServer ?? "localhost");
                return false;
            }
        });
    }

    public async Task<bool> InstallAsync(string servicePath)
    {
        if (!IsRunningAsAdministrator())
        {
            return false;
        }

        try
        {
            var scTarget = string.IsNullOrEmpty(_remoteServer) ? "" : $"\\\\{_remoteServer} ";
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"{scTarget}create {_serviceName} binPath=\"{servicePath}\" start=auto",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            await process.WaitForExitAsync();
            
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install service");
            return false;
        }
    }
}
```

---

### 4. Update ServiceAccountManager for Remote Support

**Changes to `ServiceAccountManager.cs`:**

```csharp
public class ServiceAccountManager
{
    private readonly ILogger<ServiceAccountManager> _logger;
    private const string ServiceName = "ZLFileRelay";
    private string? _remoteServer;

    public void SetRemoteServer(string? serverName)
    {
        _remoteServer = serverName;
    }

    public async Task<string?> GetCurrentServiceAccountAsync()
    {
        try
        {
            var scTarget = string.IsNullOrEmpty(_remoteServer) ? "" : $"\\\\{_remoteServer} ";
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"{scTarget}qc {ServiceName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse output for SERVICE_START_NAME
            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("SERVICE_START_NAME"))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        return parts[1].Trim();
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service account on {Server}", _remoteServer ?? "localhost");
            return null;
        }
    }

    public async Task<bool> SetServiceAccountAsync(string username, string password)
    {
        try
        {
            var scTarget = string.IsNullOrEmpty(_remoteServer) ? "" : $"\\\\{_remoteServer} ";
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"{scTarget}config {ServiceName} obj= \"{username}\" password= \"{password}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            await process.WaitForExitAsync();
            
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set service account on {Server}", _remoteServer ?? "localhost");
            return false;
        }
    }
}
```

---

### 5. Remote-Aware Features

**Features that work remotely out of the box:**
- ✅ Service status monitoring
- ✅ Service start/stop/restart
- ✅ Service install/uninstall (if admin on remote machine)
- ✅ Service account management
- ✅ Configuration read/write (via UNC path)
- ✅ SSH connection testing (already remote)

**Features that require local execution:**
- ❌ SSH key generation (keys should be generated on the server)
- ❌ Folder permission management (requires local execution)
- ⚠️ Grant "Logon as Service" right (requires remote PowerShell)

**Solution for Limited Features:**
- Show warning when remote server is selected
- Disable unsupported features
- Provide instructions for manual steps
- Or: Generate PowerShell script for remote execution

---

## 📦 Installer Changes

### Install Options

**1. Full Installation** (Default)
- ZLFileRelay.Service
- ZLFileRelay.WebPortal
- ZLFileRelay.ConfigTool
- Target: Servers with GUI

**2. Server Core Installation**
- ZLFileRelay.Service
- ZLFileRelay.WebPortal (Kestrel)
- NO ConfigTool (no GUI support)
- Target: Windows Server Core

**3. ConfigTool Only** (New)
- ZLFileRelay.ConfigTool ONLY
- NO Service
- NO WebPortal
- Target: Admin workstations

**4. Custom Installation**
- Let user choose components
- Target: Advanced users

### Inno Setup Changes

```inno
[Components]
Name: "service"; Description: "ZL File Relay Service"; Types: full server
Name: "webportal"; Description: "Web Upload Portal"; Types: full server
Name: "configtool"; Description: "Configuration Tool"; Types: full configonly custom
Name: "configtool\remotemanagement"; Description: "Remote Management Support"; Types: full configonly custom

[Tasks]
Name: "installservice"; Description: "Install and start Windows service"; Components: service
Name: "configureiis"; Description: "Configure IIS for Web Portal"; Components: webportal; Check: not IsServerCore
Name: "createshortcut"; Description: "Create desktop shortcut for ConfigTool"; Components: configtool

[Code]
function IsServerCore: Boolean;
begin
  // Check if Server Core
  Result := not RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Windows NT\CurrentVersion\Server\ServerLevels');
end;

function GetDefaultInstallDir(Param: String): String;
begin
  if WizardIsComponentSelected('configtool') and not WizardIsComponentSelected('service') then
    Result := ExpandConstant('{autopf}\ZLFileRelay\ConfigTool')
  else
    Result := ExpandConstant('{autopf}\ZLFileRelay');
end;
```

---

## 🔐 Security Considerations

### Credential Management

**For Remote Connections:**
1. Use Windows Integrated Authentication (Kerberos/NTLM) when possible
2. If explicit credentials needed:
   - Store encrypted using DPAPI for user context
   - Never store in plain text
   - Clear from memory after use

### Required Permissions

**Admin Workstation:**
- Local administrator (to run ConfigTool)
- Network access to remote server

**Remote Server:**
- Administrative share access (\\server\c$)
- Service control permissions
- File system permissions

### Firewall Rules

**Remote Management requires:**
- SMB (445/tcp) - For file share access
- RPC (135/tcp, dynamic ports) - For service control
- Optional: HTTPS (8443/tcp) - For WebPortal API (future)

---

## 🚀 Implementation Phases

### Phase 1: Basic Remote Support (2-3 hours)
- [x] Add RemoteServerViewModel
- [x] Add Remote Server connection UI
- [x] Update ConfigurationService for UNC paths
- [x] Update ServiceManager for remote service control
- [x] Update ServiceAccountManager for remote sc.exe
- [x] Test remote connection
- [x] Update installer with "ConfigTool Only" option

### Phase 2: Enhanced Remote Support (Future)
- [ ] Remote SSH key generation (execute ssh-keygen remotely)
- [ ] Remote permission management (PowerShell remoting)
- [ ] Remote PowerShell script execution
- [ ] Multi-server management (manage multiple servers)

### Phase 3: REST API Support (Future)
- [ ] Add config API to WebPortal
- [ ] Implement API authentication
- [ ] Update ConfigTool to use API
- [ ] Support both file-based and API-based management

---

## 📋 Testing Checklist

### Local Mode Testing
- [ ] All features work as before
- [ ] "localhost" works same as current behavior

### Remote Mode Testing
- [ ] Connect to remote Windows Server
- [ ] Connect to remote Server Core
- [ ] Read remote configuration
- [ ] Write remote configuration
- [ ] Get remote service status
- [ ] Start/stop remote service
- [ ] Change remote service account
- [ ] Test SSH connection from remote server
- [ ] Handle access denied gracefully
- [ ] Handle offline server gracefully

### Installer Testing
- [ ] Full installation on Windows Server with GUI
- [ ] Server Core installation (no ConfigTool)
- [ ] ConfigTool Only installation on workstation
- [ ] Upgrade existing installation

---

## 📚 Documentation Updates

### User Documentation
- How to install ConfigTool on admin workstation
- How to connect to remote server
- Required permissions for remote management
- Troubleshooting remote connections

### Deployment Scenarios
1. **Single Server (GUI)**
   - Install: Full
   - Manage: Locally

2. **Single Server (Core)**
   - Install: Server Core (no ConfigTool)
   - Manage: From admin workstation

3. **Multiple Servers**
   - Install: Server Core on all servers
   - Install: ConfigTool Only on admin workstation
   - Manage: Connect to each server as needed

---

## 💡 Recommendations

### Quick Win Path
1. Implement Phase 1 (Basic Remote Support)
2. Update installer with component options
3. Test with Server Core
4. Document remote management

### Future Enhancements
1. REST API for WebPortal
2. Multi-server dashboard
3. PowerShell remoting integration
4. Centralized configuration management

---

## 🎯 Summary

**Current State:** ConfigTool only works on local machine  
**Target State:** ConfigTool can manage local OR remote installations  
**Primary Use Case:** Manage Server Core installations from admin workstation  
**Implementation Approach:** File share + remote service control (Phase 1)  
**Estimated Effort:** 2-3 hours for Phase 1  

**Key Benefits:**
- ✅ Supports Windows Server Core deployment
- ✅ Remote management from admin workstation
- ✅ No additional infrastructure required
- ✅ Uses native Windows features
- ✅ Simple to implement and maintain

---

**Ready to implement?** This is a great enhancement for enterprise deployment!

