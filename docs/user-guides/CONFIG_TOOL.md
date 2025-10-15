# ZL File Relay ConfigTool - Quick Start Guide

## 🚀 Getting Started

The ConfigTool provides a unified interface for configuring and managing all aspects of ZL File Relay.

---

## 📋 Tab Overview

### 1. 🎛️ Service Management
**Control the Windows service and view status**

**Features:**
- Real-time service status (auto-refresh every 5 seconds)
- Install/Uninstall Windows service
- Start/Stop service
- View operation logs

**Quick Actions:**
```
Install Service:  Click "Install Service" (requires Admin)
Start Service:    Click "Start Service"
Stop Service:     Click "Stop Service"
Uninstall:        Click "Uninstall Service" (requires Admin)
```

---

### 2. ⚙️ Configuration
**Edit all application settings**

**Settings Categories:**
- **Branding** - Company name, product name, site name, logo, colors
- **Paths** - Upload, transfer, log, config, temp, archive directories
- **Service** - Retry attempts, concurrency, file stability, queue size
- **Security** - Executable files, hidden files, max upload size
- **Logging** - Retention days, max file size, event log settings

**Quick Actions:**
```
Save Configuration:      Click "Save Configuration"
Create Directories:      Click "Create Directories"
Browse Directory:        Click "Browse" next to path field
Reset to Defaults:       Click "Reset to Defaults"
Export Configuration:    Click "Export Configuration"
Import Configuration:    Click "Import Configuration"
Open Config Folder:      Click "Open Config Folder"
```

---

### 3. 🔐 SSH Settings
**Configure SSH/SCP file transfer**

**Features:**
- Generate SSH keys (ED25519)
- Test SSH connections
- Configure host, port, username
- View and copy public keys
- Detailed SSH operation log

**Quick Actions:**
```
Generate SSH Keys:       Click "Generate SSH Keys"
View Public Key:         Generated key appears in log
Copy Public Key:         Click "Copy Public Key"
Browse Private Key:      Click "Browse" to select existing key
Test SSH Connection:     Click "Test SSH Connection"
Save SSH Settings:       Click "Save Configuration"
```

**SSH Key Setup Process:**
1. Click "Generate SSH Keys"
2. Keys created in: `C:\ProgramData\ZLFileRelay\zlrelay_key`
3. Public key displays in log
4. Click "Copy Public Key" to copy to clipboard
5. Add to remote server's `~/.ssh/authorized_keys` file
6. Configure host, port, username, destination path
7. Click "Test SSH Connection" to verify
8. Click "Save Configuration" to persist settings

---

### 4. 🌐 Web Portal
**Configure the web upload portal**

**Features:**
- HTTP/HTTPS port configuration
- SSL certificate management
- Windows Authentication settings
- File upload limits

**Quick Actions:**
```
Set HTTP Port:           Enter port number (default: 8080)
Set HTTPS Port:          Enter port number (default: 8443)
Enable HTTPS:            Check "Enable HTTPS"
Browse Certificate:      Click "Browse Certificate"
Test Certificate:        Click "Test Certificate"
Save Web Settings:       Click "Save Configuration"
```

---

### 5. 👤 Service Account
**Manage the Windows service account**

**Features:**
- View current service account
- Change service account
- Grant "Logon as Service" right
- Fix folder permissions
- Profile status checking

**Quick Actions:**
```
View Current Account:    Displays at top of tab
Refresh Status:          Click "Refresh Status"
Set Service Account:     Enter username/password, click "Set Service Account"
Grant Logon Right:       Click "Grant Logon Right"
Fix Source Permissions:  Click "Fix Source Permissions"
Fix Service Permissions: Click "Fix Service Permissions"
```

**Service Account Setup:**
1. Enter service account username (DOMAIN\username)
2. Enter password
3. Click "Set Service Account"
4. Click "Grant Logon Right"
5. Click "Fix Source Permissions"
6. Click "Fix Service Permissions"
7. Restart service

---

## 🎯 Common Tasks

### First-Time Setup

**1. Configure Basic Settings**
```
Tab: Configuration
Action:
  1. Set company name and site name
  2. Configure upload directory
  3. Configure transfer directory
  4. Set log directory
  5. Click "Create Directories"
  6. Click "Save Configuration"
```

**2. Generate and Configure SSH Keys**
```
Tab: SSH Settings
Action:
  1. Click "Generate SSH Keys"
  2. Click "Copy Public Key"
  3. Add public key to remote server's ~/.ssh/authorized_keys
  4. Enter SSH host and port
  5. Enter SSH username
  6. Enter remote destination path
  7. Click "Test SSH Connection"
  8. Verify connection succeeds
  9. Click "Save Configuration"
```

**3. Install and Start Service**
```
Tab: Service Management
Action:
  1. Click "Install Service" (as Administrator)
  2. Wait for installation to complete
  3. Click "Start Service"
  4. Verify status shows "Running"
```

---

### Update SSH Connection Settings

```
Tab: SSH Settings
Action:
  1. Update host, port, or username as needed
  2. Update destination path if needed
  3. Click "Test SSH Connection" to verify
  4. If successful, click "Save Configuration"
```

---

### Change Service Account

```
Tab: Service Account
Action:
  1. Stop the service first (Service Management tab)
  2. Enter new service account username
  3. Enter password
  4. Click "Set Service Account"
  5. Click "Grant Logon Right"
  6. Click "Fix Source Permissions"
  7. Click "Fix Service Permissions"
  8. Start the service (Service Management tab)
```

---

### Backup Configuration

```
Tab: Configuration
Action:
  1. Click "Export Configuration"
  2. Choose save location
  3. Enter filename (auto-generates with timestamp)
  4. Click "Save"
  5. Configuration exported as JSON
```

---

### Restore Configuration

```
Tab: Configuration
Action:
  1. Click "Import Configuration"
  2. Browse to exported JSON file
  3. Click "Open"
  4. Configuration loaded (not saved yet)
  5. Review settings
  6. Click "Save Configuration" to apply
```

---

### Enable HTTPS for Web Portal

```
Tab: Web Portal
Action:
  1. Check "Enable HTTPS"
  2. Set HTTPS port (e.g., 8443)
  3. Click "Browse Certificate"
  4. Select .pfx or .p12 certificate file
  5. Enter certificate password
  6. Click "Test Certificate"
  7. Verify certificate is valid
  8. Click "Save Configuration"
  9. Restart Web Portal service to apply
```

---

## 🔧 Troubleshooting

### Service Won't Start

**Check:**
1. Service account has "Logon as Service" right
2. Service account has permissions to source folder
3. Service account has permissions to service folder
4. Configuration is valid (check validation errors)
5. All required directories exist

**Fix:**
```
Tab: Service Account
Action:
  1. Click "Refresh Status"
  2. Click "Grant Logon Right"
  3. Click "Fix Source Permissions"
  4. Click "Fix Service Permissions"
  
Tab: Configuration
Action:
  1. Click "Create Directories"
  2. Verify no validation errors
  
Tab: Service Management
Action:
  1. Try starting service again
```

---

### SSH Connection Fails

**Check:**
1. SSH host is reachable
2. SSH port is correct (default: 22)
3. Username is correct
4. Private key file exists
5. Public key is in server's authorized_keys
6. Destination path exists on remote server

**Fix:**
```
Tab: SSH Settings
Action:
  1. Verify host and port
  2. Verify username
  3. Click "View Public Key"
  4. Verify public key is on remote server
  5. Click "Test SSH Connection"
  6. Check log for detailed error
```

---

### Configuration Won't Save

**Check:**
1. Validation errors displayed
2. Write permissions to appsettings.json
3. Required fields are filled

**Fix:**
```
Tab: Configuration
Action:
  1. Check validation errors at bottom
  2. Fix any errors shown
  3. Click "Save Configuration" again
```

---

## 📁 File Locations

### Configuration Files
```
Main Config:        C:\Users\[user]\GitHub\ZLFileRelay\appsettings.json
                   (or application directory)

Data Directory:     C:\ProgramData\ZLFileRelay\
SSH Keys:          C:\ProgramData\ZLFileRelay\zlrelay_key
                   C:\ProgramData\ZLFileRelay\zlrelay_key.pub
Credentials:       C:\ProgramData\ZLFileRelay\credentials.dat
```

### Default Directories
```
Upload:            C:\FileRelay\uploads\
Transfer:          C:\FileRelay\uploads\transfer\
Archive:           C:\FileRelay\archive\
Logs:              C:\FileRelay\logs\
Temp:              C:\FileRelay\temp\
```

---

## 💡 Tips & Best Practices

### Security
- ✅ Use service accounts, not personal accounts
- ✅ Use SSH keys, not passwords
- ✅ Enable HTTPS for web portal in production
- ✅ Regularly review audit logs
- ✅ Use strong passwords for service accounts

### Configuration
- ✅ Export configuration before major changes
- ✅ Test SSH connection before saving
- ✅ Create all directories before starting service
- ✅ Verify service account permissions
- ✅ Use descriptive site names for multi-site deployments

### Monitoring
- ✅ Check service status regularly
- ✅ Monitor log files in log directory
- ✅ Review audit log for security events
- ✅ Test file transfers periodically
- ✅ Verify disk space on source and destination

---

## 🆘 Need Help?

### Check Logs
```
Service Logs:      C:\FileRelay\logs\
Web Portal Logs:   C:\FileRelay\logs\
Audit Log:         C:\FileRelay\logs\audit.log
Windows Event Log: Application log, Source: "ZLFileRelay"
```

### View Configuration
```
Tab: Configuration
Action: Click "Export Configuration" to see all settings
```

### Test Connectivity
```
Tab: SSH Settings
Action: Click "Test SSH Connection" for detailed results
```

---

## ✅ Checklist: Production Deployment

**Before Go-Live:**
- [ ] Generate SSH keys
- [ ] Configure SSH connection (test successful)
- [ ] Configure service account with proper permissions
- [ ] Create all required directories
- [ ] Configure web portal (HTTPS enabled for production)
- [ ] Test file upload through web portal
- [ ] Test file transfer via SSH
- [ ] Verify audit logging enabled
- [ ] Export configuration backup
- [ ] Document server-specific settings

**After Go-Live:**
- [ ] Verify service is running
- [ ] Test end-to-end file transfer
- [ ] Check log files for errors
- [ ] Verify web portal accessibility
- [ ] Set up monitoring/alerts
- [ ] Schedule regular maintenance

---

## 📚 Related Documentation

- [Full Documentation](README.md)
- [ViewModels Complete](VIEWMODELS_COMPLETE.md)
- [Implementation Summary](VIEWMODEL_IMPLEMENTATION_SUMMARY.md)
- [Configuration Reference](docs/CONFIGURATION.md)
- [Deployment Guide](docs/DEPLOYMENT.md)

---

**ConfigTool Version:** 1.0  
**Last Updated:** October 8, 2025  
**Status:** ✅ Production Ready

