# ZL File Relay - Configuration Reference

Complete reference for all configuration options.

## Configuration File Location

**Primary Configuration:**
```
C:\ProgramData\ZLFileRelay\appsettings.json
```

**Development Override:**
```
C:\ProgramData\ZLFileRelay\appsettings.Development.json
```

## Configuration Structure

```json
{
  "ZLFileRelay": {
    "Branding": { },
    "Paths": { },
    "Logging": { },
    "Service": { },
    "WebPortal": { },
    "Transfer": { },
    "Security": { }
  }
}
```

## Branding Section

Customize application branding and identity.

```json
"Branding": {
  "CompanyName": "Your Company Name",
  "ProductName": "ZL File Relay",
  "SiteName": "Site Name or Location",
  "SupportEmail": "support@example.com",
  "LogoPath": "Assets/logo.png",
  "Theme": {
    "PrimaryColor": "#0066CC",
    "SecondaryColor": "#003366",
    "AccentColor": "#FF6600"
  }
}
```

**Options:**
- `CompanyName`: Your company name (appears in UI)
- `ProductName`: Product name (default: "ZL File Relay")
- `SiteName`: Specific site or location name
- `SupportEmail`: Contact email for support
- `LogoPath`: Path to company logo (relative to web portal root)
- `Theme.PrimaryColor`: Primary UI color (hex)
- `Theme.SecondaryColor`: Secondary UI color (hex)
- `Theme.AccentColor`: Accent color for highlights (hex)

## Paths Section

Configure directories for uploads, transfers, logs, and configuration.

```json
"Paths": {
  "UploadDirectory": "C:\\FileRelay\\uploads",
  "TransferDirectory": "C:\\FileRelay\\uploads\\transfer",
  "LogDirectory": "C:\\FileRelay\\logs",
  "ConfigDirectory": "C:\\ProgramData\\ZLFileRelay",
  "TempDirectory": "C:\\FileRelay\\temp"
}
```

**Options:**
- `UploadDirectory`: Where web portal saves uploaded files
- `TransferDirectory`: Directory monitored by service for transfers
- `LogDirectory`: Location for log files
- `ConfigDirectory`: Configuration and encrypted credentials location
- `TempDirectory`: Temporary file processing directory

## Logging Section

Configure logging behavior for all components.

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft": "Warning",
    "Microsoft.AspNetCore": "Warning",
    "ZLFileRelay": "Debug"
  },
  "RetentionDays": 30,
  "MaxFileSizeMB": 100,
  "EnableEventLog": true,
  "EnableConsole": false
}
```

**Log Levels:**
- `Trace`: Most detailed, for debugging only
- `Debug`: Detailed diagnostic information
- `Information`: General informational messages
- `Warning`: Warning messages for potentially harmful situations
- `Error`: Error messages for failures
- `Critical`: Critical failures requiring immediate attention

**Options:**
- `RetentionDays`: Days to keep log files (default: 30)
- `MaxFileSizeMB`: Maximum log file size before rolling (default: 100)
- `EnableEventLog`: Write to Windows Event Log (default: true)
- `EnableConsole`: Write to console (useful for debugging)

## Service Section

Configure the file transfer Windows Service.

```json
"Service": {
  "Enabled": true,
  "ServiceName": "ZLFileRelay",
  "DisplayName": "ZL File Relay Service",
  "Description": "Automated file transfer from DMZ to SCADA networks",
  "StartMode": "Automatic",
  "WatchDirectory": "C:\\FileRelay\\uploads\\transfer",
  "TransferMethod": "ssh",
  "RetryAttempts": 3,
  "RetryDelaySeconds": 30,
  "RetryBackoffMultiplier": 2.0,
  "MaxConcurrentTransfers": 5,
  "FileFilter": "*.*",
  "DeleteAfterTransfer": false,
  "ArchiveAfterTransfer": true,
  "ArchiveDirectory": "C:\\FileRelay\\archive",
  "VerifyTransfer": true
}
```

**Options:**
- `Enabled`: Enable/disable the service
- `WatchDirectory`: Directory to monitor for files
- `TransferMethod`: "ssh" or "smb"
- `RetryAttempts`: Number of retry attempts on failure
- `RetryDelaySeconds`: Initial delay between retries
- `RetryBackoffMultiplier`: Exponential backoff multiplier
- `MaxConcurrentTransfers`: Max simultaneous transfers
- `FileFilter`: File pattern to watch (e.g., "*.csv", "*.*")
- `DeleteAfterTransfer`: Delete source file after successful transfer
- `ArchiveAfterTransfer`: Move to archive instead of delete
- `ArchiveDirectory`: Archive location
- `VerifyTransfer`: Verify file integrity after transfer

## WebPortal Section

Configure the ASP.NET Core web upload portal.

```json
"WebPortal": {
  "Enabled": true,
  "Authentication": {
    "EnableEntraId": false,
    "EnableLocalAccounts": true,
    "EntraIdTenantId": null,
    "EntraIdClientId": null,
    "EntraIdClientSecret": null,
    "ConnectionString": "Data Source=C:\\ProgramData\\ZLFileRelay\\zlfilerelay.db",
    "RequireEmailConfirmation": false,
    "RequireApproval": true
  },
  "MaxFileSizeBytes": 4294967295,
  "MaxConcurrentUploads": 10,
  "AllowedFileExtensions": [],
  "BlockedFileExtensions": [".exe", ".dll", ".bat", ".cmd", ".ps1"],
  "EnableUploadToTransfer": true,
  "UploadLocations": {
    "DMZ": "C:\\FileRelay\\uploads\\dmz",
    "Transfer": "C:\\FileRelay\\uploads\\transfer"
  },
  "EnableNotifications": false,
  "NotificationEmail": "notifications@example.com"
}
```

**Authentication Options:**
Authentication supports two modes:
1. **Local Accounts** (default): SQLite-based user accounts, works offline
2. **Entra ID**: Microsoft 365/Azure AD SSO, requires internet connectivity

**Important:** When Entra ID is enabled, Local Accounts are automatically disabled. At least one authentication method must always be enabled.

**Authentication Settings:**
- `Authentication.EnableEntraId`: Enable Azure AD SSO authentication (requires internet)
  - When enabled, Local Accounts are automatically disabled
  - Requires `EntraIdTenantId` and `EntraIdClientId`
- `Authentication.EnableLocalAccounts`: Enable local username/password authentication (works offline)
  - Default: true
  - Automatically disabled when Entra ID is enabled
- `Authentication.EntraIdTenantId`: Azure AD Tenant ID (GUID, required if Entra ID enabled)
- `Authentication.EntraIdClientId`: Azure AD Application ID (GUID, required if Entra ID enabled)
- `Authentication.EntraIdClientSecret`: Azure AD Client Secret (stored encrypted in credentials.dat)
- `Authentication.ConnectionString`: SQLite database path for local accounts
- `Authentication.RequireEmailConfirmation`: Require email verification for new local accounts
- `Authentication.RequireApproval`: Require admin approval for new local accounts

**Portal Options:**
- `MaxFileSizeBytes`: Maximum file size (default: ~4GB)
- `MaxConcurrentUploads`: Max simultaneous uploads per user
- `AllowedFileExtensions`: If set, only these extensions allowed
- `BlockedFileExtensions`: File extensions to reject
- `EnableUploadToTransfer`: Allow direct upload to transfer queue
- `UploadLocations`: Named upload destinations
- `EnableNotifications`: Send email on uploads
- `NotificationEmail`: Email address for notifications

## Transfer Section

Configure transfer destination settings.

### SSH Transfer (Recommended)

```json
"Transfer": {
  "Ssh": {
    "Host": "scada-server.example.com",
    "Port": 22,
    "Username": "svc_filetransfer",
    "AuthMethod": "PublicKey",
    "PrivateKeyPath": "C:\\ProgramData\\ZLFileRelay\\id_ed25519",
    "PrivateKeyPassphrase": "",
    "DestinationPath": "/data/incoming",
    "CreateDestinationDirectory": true,
    "PreserveTimestamps": true,
    "ConnectionTimeout": 30,
    "OperationTimeout": 300,
    "KeepAliveInterval": 30
  }
}
```

**SSH Options:**
- `Host`: SCADA server hostname or IP
- `Port`: SSH port (default: 22)
- `Username`: SSH username
- `AuthMethod`: "PublicKey" or "Password"
- `PrivateKeyPath`: Path to private key file
- `PrivateKeyPassphrase`: Passphrase for encrypted keys (optional)
- `DestinationPath`: Remote directory path
- `CreateDestinationDirectory`: Create directory if missing
- `PreserveTimestamps`: Keep original file timestamps
- `ConnectionTimeout`: Connection timeout (seconds)
- `OperationTimeout`: File transfer timeout (seconds)
- `KeepAliveInterval`: Keep connection alive interval

### SMB Transfer (Fallback)

```json
"Transfer": {
  "Smb": {
    "Server": "\\\\scada-server\\share",
    "SharePath": "\\\\scada-server\\share\\incoming",
    "UseCredentials": true,
    "Username": "DOMAIN\\svc_filetransfer",
    "PasswordEncrypted": "<encrypted>",
    "Domain": "DOMAIN",
    "Timeout": 300,
    "BufferSize": 8192
  }
}
```

**SMB Options:**
- `Server`: Server UNC path
- `SharePath`: Full UNC path to destination
- `UseCredentials`: Use specific credentials (vs current user)
- `Username`: SMB username (DOMAIN\\user format)
- `PasswordEncrypted`: Encrypted password (use ConfigTool to set)
- `Domain`: Active Directory domain
- `Timeout`: Transfer timeout (seconds)
- `BufferSize`: Transfer buffer size (bytes)

## Security Section

Advanced security settings.

```json
"Security": {
  "EncryptCredentials": true,
  "RequireSecureTransfer": true,
  "AllowedCipherSuites": ["aes256-gcm", "aes128-gcm"],
  "MinTlsVersion": "1.2",
  "EnableAuditLog": true,
  "AuditLogPath": "C:\\FileRelay\\logs\\audit.log",
  "SensitiveDataMasking": true
}
```

**Options:**
- `EncryptCredentials`: Encrypt stored credentials with DPAPI
- `RequireSecureTransfer`: Only allow secure protocols
- `AllowedCipherSuites`: SSH cipher suites to allow
- `MinTlsVersion`: Minimum TLS version for web portal
- `EnableAuditLog`: Log all security-relevant operations
- `AuditLogPath`: Location for audit log
- `SensitiveDataMasking`: Mask sensitive data in logs

## Environment-Specific Configuration

Use environment-specific files to override settings:

**Development:**
```
appsettings.Development.json
```

**Production:**
```
appsettings.Production.json
```

**Example Override:**
```json
{
  "ZLFileRelay": {
    "Logging": {
      "LogLevel": {
        "Default": "Debug"
      }
    }
  }
}
```

## Configuration Validation

The application validates configuration on startup:
- Required paths exist or can be created
- Credentials are properly encrypted
- Network destinations are reachable
- File permissions are correct

Check logs for validation errors:
```
C:\FileRelay\logs\log-YYYYMMDD.txt
```

## Configuration Tool

Use the WPF Configuration Tool for easy configuration:
- Graphical interface for all settings
- Automatic validation
- Test connections before saving
- SSH key generation
- Credential encryption

## Best Practices

1. **Use SSH over SMB** - More secure and reliable
2. **Limit upload file extensions** - Reduce security risks
3. **Enable audit logging** - Track all operations
4. **Use dedicated service accounts** - Don't use admin accounts
5. **Set appropriate retry limits** - Balance reliability vs queue backup
6. **Monitor log directory size** - Set up log rotation
7. **Test configuration changes** - Test before deploying to production
8. **Backup configuration** - Regular backups of appsettings.json
9. **Restrict file permissions** - Limit who can modify config
10. **Use strong passphrases** - For SSH keys if encryption used

## Next Steps

- [Installation Guide](INSTALLATION.md)
- [Deployment Guide](DEPLOYMENT.md)
- [Troubleshooting](TROUBLESHOOTING.md)
