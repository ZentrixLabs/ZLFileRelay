# ZL File Relay Documentation

Complete documentation for ZL File Relay - Enterprise File Transfer Solution.

---

## Quick Links

| Document | Description |
|----------|-------------|
| **[SETUP.md](SETUP.md)** | **Complete setup guide** - Installation, configuration, and troubleshooting |
| **[ENTRA_ID_SETUP.md](ENTRA_ID_SETUP.md)** | Azure AD (Entra ID) authentication configuration |
| **[SSH_TARGET_SERVER.md](SSH_TARGET_SERVER.md)** | Setting up SSH on the target server for file transfers |

---

## Getting Started

### New Installation?

Start here: **[SETUP.md](SETUP.md)**

This comprehensive guide covers:
- System requirements
- Installation walkthrough
- Initial configuration
- Authentication setup (Entra ID or Local Accounts)
- Transfer configuration (SSH or SMB)
- Service management
- Testing and troubleshooting

### Need to Configure Azure AD?

See: **[ENTRA_ID_SETUP.md](ENTRA_ID_SETUP.md)**

Step-by-step guide for:
- Azure App registration
- Redirect URIs configuration
- Client secret creation
- Troubleshooting common Azure AD errors

### Setting Up SSH Target Server?

See: **[SSH_TARGET_SERVER.md](SSH_TARGET_SERVER.md)**

Guide for:
- Installing and configuring SSH on Linux/Unix servers
- Creating service accounts
- Adding SSH public keys
- Security hardening
- Troubleshooting SSH connections

---

## What's Included

**ZL File Relay** consists of three components:

### 1. File Transfer Service
Automated Windows Service that monitors directories and transfers files via SSH/SCP or SMB.

**Features**:
- Real-time file system monitoring
- Automatic retry with exponential backoff
- File integrity verification
- Comprehensive logging

### 2. Web Upload Portal
User-friendly web interface for file uploads.

**Features**:
- Hybrid authentication (Entra ID + Local Accounts)
- Multi-file upload with progress tracking
- Destination selection (DMZ or SCADA)
- Responsive design
- Real-time transfer status

### 3. Configuration Tool
WPF desktop application for managing all settings.

**Features**:
- Unified configuration interface
- SSH key generation
- Service installation and management
- Entra ID Setup Wizard
- Certificate Store Browser
- Real-time service status

---

## Common Tasks

### Installing ZL File Relay
1. Run installer as Administrator
2. Select components to install
3. Launch Configuration Tool
4. Follow setup guide: [SETUP.md](SETUP.md)

### Configuring Authentication

**Option A: Entra ID (Azure AD)**
- Best for organizations with Microsoft 365
- See: [ENTRA_ID_SETUP.md](ENTRA_ID_SETUP.md)

**Option B: Local Accounts**
- Best for air-gapped networks
- See: [SETUP.md](SETUP.md#authentication-setup)

### Setting Up File Transfer

**SSH/SCP (Recommended)**:
1. Generate SSH keys in Configuration Tool
2. Configure target server: [SSH_TARGET_SERVER.md](SSH_TARGET_SERVER.md)
3. Test connection
4. Start service

**SMB (Windows Shares)**:
1. Configure SMB settings in Configuration Tool
2. Test connection
3. Start service

### Troubleshooting

See troubleshooting sections in:
- [SETUP.md](SETUP.md#troubleshooting) - General issues
- [ENTRA_ID_SETUP.md](ENTRA_ID_SETUP.md#troubleshooting) - Azure AD issues
- [SSH_TARGET_SERVER.md](SSH_TARGET_SERVER.md#troubleshooting) - SSH issues

---

## Configuration File

All settings stored in: `C:\ProgramData\ZLFileRelay\appsettings.json`

**Important**: Use Configuration Tool to edit settings (not manual editing). The tool:
- Validates configuration
- Encrypts sensitive data (passwords, secrets)
- Ensures proper formatting
- Restarts services automatically

---

## Service Management

### Via Configuration Tool (Recommended)
1. Open Configuration Tool as Administrator
2. Go to Dashboard tab
3. Use Start/Stop buttons

### Via Windows Services
1. Open `services.msc`
2. Find "ZL File Relay Service"
3. Right-click → Start/Stop/Restart

### Via PowerShell
```powershell
# Start service
Start-Service ZLFileRelay

# Stop service
Stop-Service ZLFileRelay

# Restart service
Restart-Service ZLFileRelay

# Check status
Get-Service ZLFileRelay
```

---

## Logging

### Service Logs
- Location: `C:\FileRelay\logs\zlfilerelay-service-[date].log`
- Format: Structured logging with timestamps
- Retention: 30 days (rolling daily)

### Web Portal Logs
- Location: `C:\FileRelay\logs\zlfilerelay-web-[date].log`
- Format: Structured logging with timestamps
- Retention: 30 days (rolling daily)

### Windows Event Log
- Source: `ZLFileRelay`
- Log: Application
- View: `eventvwr.msc`

---

## Security Best Practices

1. ✅ **Use SSH key authentication** (never use SSH passwords)
2. ✅ **Enable Entra ID** for enterprise SSO
3. ✅ **Use trusted SSL certificates** (not self-signed in production)
4. ✅ **Run services under service account** (not local system)
5. ✅ **Restrict directory permissions** to service account only
6. ✅ **Monitor logs regularly** for suspicious activity
7. ✅ **Keep .NET runtime updated** via Windows Update
8. ✅ **Rotate Azure AD client secrets** annually
9. ✅ **Enable Windows Firewall** rules for necessary ports only
10. ✅ **Use strong passwords** for local accounts

---

## Support

### Getting Help

**Documentation**:
- Start here: [SETUP.md](SETUP.md)
- Azure AD: [ENTRA_ID_SETUP.md](ENTRA_ID_SETUP.md)
- SSH Setup: [SSH_TARGET_SERVER.md](SSH_TARGET_SERVER.md)

**Logs**:
- Service: `C:\FileRelay\logs\zlfilerelay-service-[date].log`
- Web Portal: `C:\FileRelay\logs\zlfilerelay-web-[date].log`
- Windows Events: `eventvwr.msc` → Application

**Contact**:
- Email: support@yourdomain.com
- Include: Log files, configuration (remove secrets), error messages

---

## Archive

Previous documentation has been moved to `archive/` for reference. The current docs above are the authoritative source.

---

**ZL File Relay** - Secure, Reliable, Professional File Transfer for Industrial Environments
