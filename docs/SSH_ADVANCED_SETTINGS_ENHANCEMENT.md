# SSH Advanced Settings Enhancement

## Overview
Enhanced SSH configuration to match the capabilities of the legacy ZLBridge system, adding support for remote server type detection and advanced SSH connection parameters.

## Changes Made

### 1. Core Configuration Model
**File:** `src/ZLFileRelay.Core/Models/ZLFileRelayConfiguration.cs`

Added new property to `SshSettings` class:
- **`RemoteServerType`** (string) - Specifies whether the remote SSH server is "Windows" or "Linux"
  - Default: "Windows"
  - Affects how the SSH implementation handles path formatting
  - Critical for proper file transfers to different OS types

### 2. ViewModel Updates
**File:** `src/ZLFileRelay.ConfigTool/ViewModels/FileTransferViewModel.cs`

Added observable properties for SSH advanced settings:
- `IsRemoteServerWindows` (bool) - Radio button binding for Windows server type
- `IsRemoteServerLinux` (bool) - Radio button binding for Linux server type
- `SshCompression` (bool) - Enable/disable SSH compression
- `SshConnectionTimeout` (int) - Connection timeout in seconds (10-300)
- `SshTransferTimeout` (int) - Transfer timeout in seconds (60-3600)
- `SshStrictHostKeyChecking` (bool) - Enable/disable strict host key verification

Added logic to:
- Load these settings from configuration
- Save these settings back to configuration
- Ensure mutual exclusivity between Windows/Linux server type radio buttons

### 3. User Interface
**File:** `src/ZLFileRelay.ConfigTool/Views/FileTransferView.xaml`

Added new "Advanced SSH Settings" expandable section with:

#### Remote Server Type
- Radio buttons for Windows vs Linux selection
- Helpful tooltip: "Affects path format: Windows uses backslashes (\), Linux uses forward slashes (/)"

#### SSH Compression
- Checkbox to enable/disable compression
- Description: "Can improve transfer speed for large files on slow connections"

#### Connection Timeout
- NumberBox with range 10-300 seconds
- Default: 30 seconds
- Description: "Seconds to wait when establishing SSH connection"

#### Transfer Timeout
- NumberBox with range 60-3600 seconds
- Default: 300 seconds
- Description: "Seconds to wait for file transfer operations"

#### Strict Host Key Checking
- Checkbox to enable/disable verification
- Description: "Ensures you're connecting to the expected server (disable only for testing)"

### 4. Configuration Files Updated
Updated all `appsettings.json` files to include new SSH properties:

**Updated Files:**
- `appsettings.json` - Main configuration template
- `appsettings.DMZ.json` - DMZ deployment template
- `src/ZLFileRelay.Service/appsettings.json` - Service default config
- `src/ZLFileRelay.Service/appsettings.Development.json` - Development config

**Added Properties:**
```json
{
  "Transfer": {
    "Ssh": {
      "RemoteServerType": "Linux",
      "Compression": true,
      "ConnectionTimeout": 30,
      "TransferTimeout": 300,
      "StrictHostKeyChecking": true
    }
  }
}
```

## Benefits

### 1. Feature Parity with Legacy ZLBridge
Now matches the SSH configuration capabilities of the original ZLBridge.ConfigTool, ensuring smooth migration path for existing deployments.

### 2. Cross-Platform Support
The `RemoteServerType` setting enables proper handling of:
- **Windows SSH Servers** (e.g., OpenSSH on Windows Server)
  - Path format: `C:\path\to\file` or `C:/path/to/file`
  - Windows-specific SSH considerations
  
- **Linux/Unix SSH Servers** (traditional)
  - Path format: `/path/to/file`
  - Unix-specific file handling

### 3. Performance Tuning
- **Compression** can be enabled for slow network connections
- **Timeouts** can be adjusted for different network conditions:
  - High-latency networks: Increase timeouts
  - Fast local networks: Decrease timeouts for faster failure detection

### 4. Security Flexibility
- **Strict Host Key Checking** can be disabled during initial setup/testing
- Should be re-enabled in production for security

## UI/UX Improvements

### Collapsible Design
The advanced settings are in an expandable section (`IsExpanded="False"` by default):
- Keeps the interface clean for basic users
- Power users can expand to access all settings
- Prevents overwhelming new users with too many options

### Contextual Help
- Each setting includes descriptive text explaining its purpose
- Default values are noted in tooltips
- Warnings included where security implications exist

### Input Validation
- NumberBox controls enforce minimum/maximum ranges
- Radio button groups ensure only one server type is selected
- All settings have sensible defaults

## Migration Notes

### From Legacy ZLBridge
If migrating from ZLBridge, the configuration tool will now recognize:
- `RemoteServerType` field (previously present in ZLBridge)
- All advanced SSH settings match the old implementation

### Default Behavior
Without changing any settings:
- Remote server type defaults to "Windows"
- Compression is enabled
- Standard timeouts apply (30s connection, 300s transfer)
- Strict host key checking is enabled

## Technical Implementation

### Property Binding
Uses MVVM pattern with `ObservableProperty` attributes:
```csharp
[ObservableProperty] private bool _isRemoteServerWindows = true;
[ObservableProperty] private bool _isRemoteServerLinux = false;
```

### Mutual Exclusivity
Implemented via `OnChanged` partial methods:
```csharp
partial void OnIsRemoteServerWindowsChanged(bool value)
{
    if (value) IsRemoteServerLinux = false;
}
```

### Configuration Mapping
Bidirectional mapping in `FileTransferViewModel`:
```csharp
// Load
IsRemoteServerWindows = _config.Transfer.Ssh.RemoteServerType
    ?.Equals("Windows", StringComparison.OrdinalIgnoreCase) ?? true;

// Save
_config.Transfer.Ssh.RemoteServerType = IsRemoteServerWindows ? "Windows" : "Linux";
```

## Future Enhancements

Potential additions for future versions:
1. **Auto-detection** of remote server type via SSH negotiation
2. **Connection profiles** for frequently used servers
3. **Advanced cipher suite selection** UI
4. **Keep-alive interval** configuration
5. **SSH key fingerprint** display and verification

## Compatibility

- ✅ Backward compatible with existing configurations
- ✅ Forward compatible with legacy ZLBridge settings
- ✅ All settings optional with sensible defaults
- ✅ No breaking changes to existing deployments

## Testing Recommendations

1. Test SSH to Windows OpenSSH server with `RemoteServerType: "Windows"`
2. Test SSH to Linux server with `RemoteServerType: "Linux"`
3. Verify compression toggle affects transfer performance
4. Validate timeout settings prevent hanging connections
5. Confirm strict host key checking warns on server changes

## Date
January 2025

## Author
ZL File Relay Development Team

