# Credential Management in ZL File Relay

## Overview

ZL File Relay uses **two completely separate types of credentials** that must never be confused:

1. **Remote Management Credentials** - For the ConfigTool to manage remote servers
2. **Service Account Credentials** - For the ZLFileRelay Windows Service to run

## Remote Management Credentials

### Purpose
These credentials are used by the ConfigTool to connect to and manage a remote ZL File Relay installation.

### Types
1. **Current User Credentials (Default)**
   - The Windows credentials of the user running the ConfigTool
   - No password entry required
   - Uses Windows integrated authentication
   - **This is the recommended approach**

2. **Alternate Admin Credentials (Optional)** ✅ **Fully Implemented**
   - Explicitly provided admin username/password
   - Used when current user doesn't have admin rights on remote server
   - Provided through the Remote Server Settings UI
   - Password securely converted to SecureString
   - Credentials validated before connection attempt

### Where Used
- Remote server connection testing
- PowerShell Remoting (WinRM)
- Administrative share access (\\server\c$)
- Service control operations
- Configuration file management

### Configuration Location
- Configured in: **Remote Server Connection** page in ConfigTool
- NOT stored anywhere (entered per-session, memory only)
- Never written to appsettings.json
- Password converted to SecureString before use
- Credentials cleared on disconnect

## Service Account Credentials

### Purpose
These credentials are used to run the ZLFileRelay Windows Service itself.

### Details
- Windows service account (e.g., `DOMAIN\svc_filerelay`)
- Requires "Log on as a service" permission
- Needs read/write access to upload directories
- Needs network access for SSH/SCP or SMB transfers

### Where Used
- Running the ZLFileRelay Windows Service
- File watching operations
- SSH/SCP file transfers
- SMB file transfers
- Writing to log files

### Configuration Location
- Configured in: **Service Account** page in ConfigTool
- Stored in: Windows Service Manager (not in appsettings.json)
- Password encrypted by Windows

## Critical Security Rules

### ❌ NEVER Do This
- **NEVER** use service account credentials for remote management
- **NEVER** confuse the two credential types
- **NEVER** store remote management passwords in files
- **NEVER** pass service account credentials to PowerShell Remoting

### ✅ ALWAYS Do This
- **ALWAYS** use current user credentials for remote management (default)
- **ALWAYS** keep credential types separate
- **ALWAYS** document which credentials are being used
- **ALWAYS** use alternate admin credentials only when necessary

## Code Implementation

### Remote Management
```csharp
// PowerShellRemotingService.cs
private async Task<Runspace> CreateRunspaceAsync(string serverName)
{
    var connectionInfo = new WSManConnectionInfo(
        new Uri($"http://{serverName}:5985/wsman"));
    
    // IMPORTANT: Always use current user credentials or explicitly provided admin credentials
    // NEVER use service account credentials for remote management operations
    
    return RunspaceFactory.CreateRunspace(connectionInfo);
}
```

### Service Account Management
```csharp
// ServiceAccountManager.cs
public async Task<bool> SetServiceAccountAsync(string username, string password)
{
    // These credentials are ONLY used for running the service, NOT for remote management
    _logger.LogInformation("Setting service account credentials for ZLFileRelay service: {Username}", username);
    _logger.LogDebug("Note: Service account credentials are NOT used for remote management operations");
    
    // Configure Windows Service...
}
```

## UI Indicators

### Remote Server Connection Page
```
[✓] Use current Windows credentials
    Use your current Windows credentials for remote management.
    Note: These are NOT the service account credentials - those are configured separately.
    
[ ] Alternate Admin Credentials (Optional) ✅ IMPLEMENTED
    Username: [DOMAIN\adminuser_________]
    Password: [**************_________]
    
    Provide administrator credentials if different from your current login.
    Do NOT enter service account credentials here.
```

**How It Works:**
1. Check "Use current Windows credentials" (default) → Uses your logged-in user
2. Uncheck it → Alternate credentials section becomes enabled
3. Enter admin username and password
4. Click "Connect" → Credentials validated and used for remote operations
5. Password never logged or stored to disk

### Service Account Page
```
Current Service Account: DOMAIN\svc_filerelay

New Service Account Credentials:
Username: [DOMAIN\svc_account]
Password: [**********]

Note: These credentials are for running the ZLFileRelay service,
NOT for remote management of servers.
```

## Troubleshooting

### "Access Denied" on Remote Connection
- **Problem**: Current user doesn't have admin rights on remote server
- **Solution**: Use alternate admin credentials (NOT service account)
- **Check**: Is the user in the Administrators group on the remote server?

### "Service Failed to Start"
- **Problem**: Service account credentials are incorrect
- **Solution**: Update service account credentials in the Service Account page
- **Check**: Does the account have "Log on as a service" permission?

### "Cannot Access SSH Key"
- **Problem**: Service account doesn't have access to SSH key file
- **Solution**: Grant service account Read permission to SSH key file
- **Check**: Use the Permission Manager to fix service folder permissions

## Logging

All credential operations are logged with context:

```
[INFO] Setting service account credentials for ZLFileRelay service: DOMAIN\svc_filerelay
[DEBUG] Note: Service account credentials are NOT used for remote management operations

[INFO] Connecting to remote server: SERVER01 using current user credentials
[DEBUG] Remote management uses Windows authentication, not service account
```

## Summary

| Credential Type | Purpose | Configured Via | Stored Where | Used For |
|----------------|---------|----------------|--------------|----------|
| **Remote Management** | Connect to remote servers | Remote Server page | Not stored (per-session) | ConfigTool operations |
| **Service Account** | Run the service | Service Account page | Windows Service Manager | File transfer service |

Remember: **These are completely separate** and must never be confused or interchanged.
