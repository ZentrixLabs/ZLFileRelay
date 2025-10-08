# ZL File Relay - Remote Management Guide

## Overview

The ZL File Relay ConfigTool supports managing both local and remote installations, making it ideal for Windows Server Core deployments where GUI applications cannot run locally.

---

## 📦 Installation Scenarios

### Scenario 1: Server with GUI (Traditional)
**Installation:** Full Installation
- ZLFileRelay.Service
- ZLFileRelay.WebPortal  
- ZLFileRelay.ConfigTool

**Management:** Run ConfigTool locally on the server

---

### Scenario 2: Windows Server Core
**Installation:** Server Core Installation
- ZLFileRelay.Service
- ZLFileRelay.WebPortal (Kestrel mode)
- NO ConfigTool (GUI not supported)

**Management:** Install ConfigTool on admin workstation and connect remotely

---

### Scenario 3: Multiple Servers
**Installation:** Server Core Installation on all servers
**Management:** ConfigTool Only installation on admin workstation

---

## 🔧 Setup for Remote Management

### Step 1: Install on Server Core

Run the installer on the Server Core server:
```powershell
# Option 1: Use installer with /COMPONENTS parameter
ZLFileRelay-Setup.exe /COMPONENTS="service,webportal" /SILENT

# Option 2: Choose "Server Core Installation" during interactive install
ZLFileRelay-Setup.exe
```

This installs:
- ✅ Service
- ✅ WebPortal
- ❌ ConfigTool (not installed)

---

### Step 2: Install ConfigTool on Admin Workstation

Run the installer on your Windows workstation:
```powershell
# Option 1: Use installer with /COMPONENTS parameter
ZLFileRelay-Setup.exe /COMPONENTS="configtool" /SILENT

# Option 2: Choose "ConfigTool Only" during interactive install
ZLFileRelay-Setup.exe
```

This installs:
- ❌ Service (not installed)
- ❌ WebPortal (not installed)
- ✅ ConfigTool only

---

### Step 3: Configure Remote Connection

1. Launch ConfigTool on admin workstation
2. Go to **"Remote Server"** tab (first tab)
3. Select **"Remote Server"** radio button
4. Enter server name:
   - Example: `SERVER01`
   - Example: `192.168.1.100`
   - Example: `server.domain.com`
5. Click **"Test Connection"** to verify:
   - Network connectivity (ping)
   - Administrative share access (\\\\server\\c$)
   - Service control permissions
   - ZL File Relay installation
6. Review test results in log
7. Click **"Connect"**

---

## 🔐 Required Permissions

### Network Access
- Your workstation must be able to reach the server
- Firewall must allow:
  - **SMB (TCP 445)** - For file share access
  - **RPC (TCP 135 + dynamic ports)** - For service control

### User Permissions
You must be running ConfigTool with an account that has:
- **Administrator** privileges on the remote server
- **Network access** to administrative shares (\\\\server\\c$)
- **Service control** permissions on remote server

### Best Practice
Use a domain account with administrative rights on the target server.

---

##  Remote Features

### ✅ Fully Supported (Remote Mode)

| Feature | Status | Notes |
|---------|--------|-------|
| Configuration Management | ✅ Supported | Read/write via UNC path |
| Service Control (Start/Stop) | ✅ Supported | Via remote Service Controller |
| Service Install/Uninstall | ✅ Supported | Requires admin rights |
| Service Account Management | ✅ Supported | Via remote sc.exe |
| SSH Connection Testing | ✅ Supported | Already tests remote SSH servers |
| Web Portal Configuration | ✅ Supported | Updates remote config file |
| View Service Status | ✅ Supported | Real-time remote status |

### ⚠️ Limited Support (Remote Mode)

| Feature | Status | Notes |
|---------|--------|-------|
| SSH Key Generation | ⚠️ Limited | Must be done on the server |
| Grant "Logon as Service" | ⚠️ Limited | Requires PowerShell Remoting |
| Folder Permissions | ⚠️ Limited | Requires PowerShell Remoting |

**Workaround:** For limited features:
1. Generate SSH keys on the server directly using PowerShell
2. Or: Use PowerShell Remoting if enabled
3. Or: Use ConfigTool locally via RDP session

---

## 📋 Common Tasks - Remote Mode

### Connect to Remote Server

```
1. Open ConfigTool on your workstation
2. Go to "Remote Server" tab
3. Select "Remote Server" mode
4. Enter: SERVER01
5. Click "Test Connection"
6. Review test results
7. Click "Connect"
8. Status shows: "✅ Connected to SERVER01"
```

### Update Remote Configuration

```
1. Ensure connected to remote server
2. Go to "Configuration" tab
3. Make changes
4. Click "Save Configuration"
5. Config saved to \\SERVER01\c$\Program Files\ZLFileRelay\appsettings.json
```

### Start Remote Service

```
1. Ensure connected to remote server
2. Go to "Service Management" tab
3. Click "Start Service"
4. Service starts on SERVER01
5. Status updates automatically
```

### Test Remote SSH Connection

```
1. Ensure connected to remote server
2. Go to "SSH Settings" tab
3. Configure SSH host, port, username
4. Browse to remote private key: \\SERVER01\c$\ProgramData\ZLFileRelay\zlrelay_key
5. Click "Test SSH Connection"
6. Connection tests from SERVER01 to destination
```

---

## 🔄 Switching Between Local and Remote

You can switch between managing local and remote servers:

### Switch to Remote Server
```
1. Go to "Remote Server" tab
2. Select "Remote Server"
3. Enter server name
4. Click "Connect"
5. All tabs now target remote server
```

### Switch Back to Local
```
1. Go to "Remote Server" tab
2. Click "Disconnect"
3. Select "Local Machine"
4. All tabs now target local machine
```

---

## 🐛 Troubleshooting

### Cannot Connect to Remote Server

**Problem:** "Server is not reachable via ping"  
**Solution:**
- Verify server name is correct
- Ensure server is powered on
- Check network connectivity
- Verify no firewall blocking ping (ICMP)

---

**Problem:** "Cannot access administrative share"  
**Solution:**
- Verify you have admin rights on remote server
- Check Windows Firewall allows SMB (port 445)
- Ensure administrative shares are enabled:
  ```powershell
  # On remote server
  net share
  # Should see C$ share
  ```
- Test manually: `\\SERVER01\c$` in Windows Explorer

---

**Problem:** "Cannot query services"  
**Solution:**
- Verify RPC is allowed through firewall
- Ensure Remote Registry service is running
- Check you have permissions to query remote services
- Try: `sc \\SERVER01 query ZLFileRelay` from command prompt

---

### Access Denied Errors

**Problem:** "Access denied" when saving configuration  
**Solution:**
- Ensure you're logged in with admin account
- Verify account has write access to \\\\SERVER01\\c$\\Program Files\\ZLFileRelay
- Check file is not locked by another process
- Try closing and reopening ConfigTool as administrator

---

###SSH Key Management

**Problem:** Need to generate SSH keys on remote Server Core  
**Solution:**

Option 1 - Via PowerShell Remoting (if enabled):
```powershell
# From workstation
Enter-PSSession -ComputerName SERVER01
cd C:\ProgramData\ZLFileRelay
ssh-keygen -t ed25519 -f zlrelay_key -N "" -C "ZLFileRelay"
Exit-PSSession
```

Option 2 - Via RDP (if allowed):
```
1. RDP to Server Core
2. PowerShell appears
3. Run: cd C:\ProgramData\ZLFileRelay
4. Run: ssh-keygen -t ed25519 -f zlrelay_key -N "" -C "ZLFileRelay"
5. Keys created
6. Exit RDP
7. Use ConfigTool to configure path to remote keys
```

Option 3 - Via ConfigTool SSH Settings (if OpenSSH available):
```
1. Connect to remote server in ConfigTool
2. Go to SSH Settings tab
3. Note: Key generation may not work remotely
4. Use Option 1 or 2 instead
```

---

## 🔒 Security Considerations

### Network Security
- Remote management requires SMB and RPC ports open
- These should only be open on internal network
- Do NOT expose SMB/RPC to internet
- Consider using VPN for remote management over internet

### Credential Security
- Use domain accounts with appropriate rights
- Consider using dedicated service admin account
- Avoid using Domain Admin unless necessary
- Log all remote management operations (audit log)

### Best Practices
- Only allow remote management from trusted workstations
- Use Windows Firewall to restrict which IPs can connect
- Enable audit logging on remote servers
- Regularly review access logs
- Use separate accounts for ConfigTool vs service accounts

---

## 📊 Deployment Patterns

### Pattern 1: Single Site, Single Server Core

```
[Admin Workstation]              [Server Core - SERVER01]
┌─────────────────┐              ┌─────────────────────┐
│  ConfigTool     │──────────────│  Service            │
│                 │   Remote     │  WebPortal          │
│  (GUI)          │   Manage     │  (No GUI)           │
└─────────────────┘              └─────────────────────┘
```

**Setup:**
1. Install Server Core package on SERVER01
2. Install ConfigTool Only on workstation
3. Connect to SERVER01 from workstation

---

### Pattern 2: Multi-Site Deployment

```
[Admin Workstation]              [Site A - SERVER01]    [Site B - SERVER02]
┌─────────────────┐              ┌─────────────────┐    ┌─────────────────┐
│  ConfigTool     │──────────────│  Service        │    │  Service        │
│                 │   Connect    │  WebPortal      │    │  WebPortal      │
│  Switch Between │──────────────│  (No GUI)       │    │  (No GUI)       │
│  Servers        │              └─────────────────┘    └─────────────────┘
└─────────────────┘
```

**Setup:**
1. Install Server Core package on all servers
2. Install ConfigTool Only on workstation
3. Use Remote Server tab to switch between servers as needed

---

### Pattern 3: Hybrid (GUI Servers + Core Servers)

```
[Server with GUI - SERVER01]     [Server Core - SERVER02]    [Admin Workstation]
┌──────────────────────────┐     ┌─────────────────────┐    ┌─────────────────┐
│  Service                 │     │  Service            │    │  ConfigTool     │
│  WebPortal               │     │  WebPortal          │    │                 │
│  ConfigTool (Local)      │     │  (No GUI)           │────│  Remote Manage  │
└──────────────────────────┘     └─────────────────────┘    │  SERVER02       │
                                                             └─────────────────┘
```

**Setup:**
1. SERVER01: Full Installation (manage locally)
2. SERVER02: Server Core Installation
3. Workstation: ConfigTool Only (for SERVER02)

---

## 📝 Configuration File Locations

### Local Mode
```
Configuration: C:\Program Files\ZLFileRelay\appsettings.json
SSH Keys:      C:\ProgramData\ZLFileRelay\zlrelay_key
Logs:          C:\FileRelay\logs\
```

### Remote Mode (viewing from workstation)
```
Configuration: \\SERVER01\c$\Program Files\ZLFileRelay\appsettings.json
SSH Keys:      \\SERVER01\c$\ProgramData\ZLFileRelay\zlrelay_key
Logs:          \\SERVER01\c$\FileRelay\logs\
```

---

## ✅ Testing Remote Management

### Pre-Deployment Test
```
1. Install test Server Core VM
2. Install ZL File Relay (Server Core package)
3. Install ConfigTool on workstation
4. Test connection to Server Core
5. Verify all features work remotely
6. Document any issues or limitations
7. Create runbook for production deployment
```

### Production Validation
```
1. Connect to production server
2. Test service start/stop
3. Verify configuration changes apply
4. Test SSH connection from server
5. Confirm file transfers work
6. Review all logs for errors
7. Disconnect and reconnect to verify persistence
```

---

## 🎯 Summary

**Remote Management Enables:**
- ✅ Server Core deployment support
- ✅ Centralized management from admin workstation
- ✅ Multi-server management from single ConfigTool
- ✅ No RDP session required for routine tasks
- ✅ Consistent configuration across multiple sites

**Best For:**
- Windows Server Core deployments
- Multiple server environments
- DMZ/OT network scenarios where RDP is restricted
- Centralized administration model

**Requirements:**
- Network connectivity (SMB + RPC)
- Administrative credentials
- Windows Firewall configured appropriately
- OpenSSH available on servers for SSH key operations

---

**For more information:**
- [Installation Guide](INSTALLATION.md)
- [Configuration Reference](CONFIGURATION.md)
- [Quick Start Guide](../CONFIGTOOL_QUICK_START.md)
- [Remote Management Plan](../REMOTE_MANAGEMENT_PLAN.md)

