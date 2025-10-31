# ZL File Relay - Deployment Guide

## Overview

ZL File Relay uses **Kestrel standalone** (no IIS required) by default, making it perfect for DMZ, OT, and air-gapped environments.

---

## 🚀 Quick Start

### Standard Deployment (Recommended)

1. **Run Installer** - Choose installation type:
   - **Full Installation** - Service + WebPortal + ConfigTool (servers with GUI)
   - **Server Core** - Service + WebPortal only (headless servers)
   - **ConfigTool Only** - Management tool for admin workstations

2. **Configure** - Use ConfigTool to set up SSH keys and settings

3. **Start Services** - Both services start automatically

**That's it!** No IIS, no hosting bundle, no external dependencies.

---

## 📦 Installation Types

### 1. Full Installation (Windows Server with GUI)
```
Components:
├── File Transfer Service    (automated transfer)
├── Web Portal Service       (upload interface on port 8080)
└── Configuration Tool       (management GUI)

Use Case: Traditional servers with desktop experience
```

### 2. Server Core Installation
```
Components:
├── File Transfer Service    (automated transfer)
└── Web Portal Service       (upload interface on port 8080)

Use Case: Headless servers, production environments
Management: Remote via ConfigTool from admin workstation
```

### 3. ConfigTool Only
```
Components:
└── Configuration Tool       (management GUI)

Use Case: Admin workstations for remote management
Target: Manage multiple Server Core installations
```

---

## 🏗️ Architecture

### Default: Kestrel Standalone (No IIS)

```
┌─────────────────────────────────────────┐
│  Windows Server (2016+)                 │
├─────────────────────────────────────────┤
│                                         │
│  Service 1: ZLFileRelay                 │
│  ├─ File watching                       │
│  ├─ SSH/SCP or SMB transfer             │
│  └─ Logging                             │
│                                         │
│  Service 2: ZLFileRelay.WebPortal       │
│  ├─ Kestrel web server                  │
│  ├─ HTTP: 8080                          │
│  ├─ HTTPS: 8443 (optional)              │
│  ├─ Hybrid Authentication               │
│  │  ├─ Entra ID (Azure AD) optional     │
│  │  └─ Local Accounts (air-gapped)      │
│  └─ Upload interface                    │
│                                         │
│  Optional: ConfigTool (GUI management)  │
│                                         │
└─────────────────────────────────────────┘
```

**Advantages:**
- ✅ No IIS required
- ✅ No ASP.NET Core Hosting Bundle needed  
- ✅ Works on Server Core
- ✅ Self-contained (includes .NET 8 runtime)
- ✅ Zero external dependencies
- ✅ Perfect for air-gapped environments
- ✅ Local authentication works offline (no cloud required)
- ✅ Entra ID optional for cloud-connected deployments

---

## 🌐 Network Scenarios

### Scenario 1: DMZ Server (Air-Gapped)

**Environment:** Isolated DMZ network, no internet access

**Solution:**
1. Copy installer to DMZ server (USB, internal file share)
2. Run installer (self-contained, includes .NET 8)
3. Configure via ConfigTool (local or remote)
4. Access web portal: `http://dmz-server:8080`

**No external downloads required!**

---

### Scenario 2: OT/SCADA Network

**Environment:** Operational Technology network, restricted access

**Deployment:**
```
[Admin Workstation]              [OT Server - Server Core]
├── ConfigTool                   ├── Service (file transfer)
└── Remote Management ──────────►└── WebPortal (port 8080)
```

**Steps:**
1. Install "Server Core" package on OT server
2. Install "ConfigTool Only" on admin workstation
3. Configure remotely via UNC paths
4. No RDP session required for routine management

---

### Scenario 3: Multi-Site Deployment

**Environment:** Multiple remote sites needing file relay

**Deployment:**
```
[Central Admin Workstation]      [Site 1]  [Site 2]  [Site 3]
└── ConfigTool ─────────────────► Server   Server    Server
    (Switch between servers)      Core      Core      Core
```

**Steps:**
1. Install "Server Core" at each site
2. Install "ConfigTool Only" at central location
3. Use Remote Management to switch between servers
4. Consistent configuration across all sites

---

## 🔧 Configuration

### Basic Configuration

**Via ConfigTool:**
1. Open ConfigTool
2. Configure SSH connection (host, port, keys)
3. Set source/destination paths
4. Test connection
5. Save configuration
6. Start services

**Configuration File Location:**
```
Installation:  C:\Program Files\ZLFileRelay\appsettings.json
Data:          C:\ProgramData\ZLFileRelay\
Uploads:       C:\FileRelay\uploads\
Logs:          C:\FileRelay\logs\
```

---

### Port Configuration

**Default Ports:**
- HTTP: `8080`
- HTTPS: `8443` (optional)

**Change Ports:**

Edit `appsettings.json`:
```json
{
  "ZLFileRelay": {
    "WebPortal": {
      "Kestrel": {
        "HttpPort": 8080,
        "HttpsPort": 8443,
        "EnableHttps": false
      }
    }
  }
}
```

Or use ConfigTool → Web Portal tab

---

### HTTPS Configuration (Optional)

**Enable HTTPS:**
1. Obtain SSL certificate (.pfx file)
2. Open ConfigTool → Web Portal tab
3. Enable HTTPS
4. Browse to certificate file
5. Enter certificate password
6. Save configuration
7. Restart WebPortal service

**Access:** `https://server:8443`

---

## 🔥 Firewall Configuration

### Required Rules

**For File Transfer Service:**
- Outbound SSH: TCP 22 (to destination server)
- Or outbound SMB: TCP 445 (to file share)

**For Web Portal:**
- Inbound HTTP: TCP 8080
- Inbound HTTPS: TCP 8443 (if enabled)

**For Remote Management:**
- Inbound SMB: TCP 445 (admin share access)
- Inbound RPC: TCP 135 + dynamic (service control)

### PowerShell Commands

```powershell
# Allow web portal HTTP
New-NetFirewallRule -DisplayName "ZL File Relay - Web Portal (HTTP)" `
    -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow

# Allow web portal HTTPS (if enabled)
New-NetFirewallRule -DisplayName "ZL File Relay - Web Portal (HTTPS)" `
    -Direction Inbound -Protocol TCP -LocalPort 8443 -Action Allow

# Allow SSH outbound (for file transfer)
New-NetFirewallRule -DisplayName "ZL File Relay - SSH Outbound" `
    -Direction Outbound -Protocol TCP -RemotePort 22 -Action Allow
```

---

## 🔐 Security Considerations

### Service Accounts

**Default:** Local System  
**Recommended:** Dedicated service account with minimal permissions

**Required Permissions:**
- Read/Write to upload directories
- Read to transfer directories
- Write to log directories
- Network access for SSH/SMB

**Configure via ConfigTool:** Service Account tab

---

### Authentication

**Web Portal:** Hybrid Authentication (Flexible)
- **Local Accounts** (Air-Gapped): Works offline, no internet required
- **Entra ID (Azure AD)** (Optional): SSO for cloud-connected environments
- Role-based authorization (Admin/Uploader)
- No passwords transmitted (DPAPI-encrypted credentials)
- Integrated with Active Directory

**File Transfer:** SSH Public Key Authentication
- Generate keys via ConfigTool
- No passwords stored
- Ed25519 encryption

---

## 📊 Deployment Comparison

| Scenario | Service | WebPortal | ConfigTool | Remote Mgmt |
|----------|---------|-----------|------------|-------------|
| **Full Install** | ✅ | ✅ | ✅ | Optional |
| **Server Core** | ✅ | ✅ | ❌ | Required |
| **Admin Workstation** | ❌ | ❌ | ✅ | Yes |

---

## 🛠️ Advanced Scenarios

### Option: IIS Reverse Proxy (Optional)

If you prefer to use IIS:

**Benefits:**
- Centralized certificate management
- URL rewriting
- Load balancing support

**Requirements:**
- IIS 10.0+
- URL Rewrite module
- Application Request Routing module

**Not recommended** - Adds complexity. Use Kestrel directly instead.

**See:** `docs/archive/IIS_DEPLOYMENT_DMZ_OT.md` for legacy IIS setup

---

### Air-Gapped Installation

**For completely isolated environments:**

1. **On internet-connected machine:**
   - Download installer
   - Verify hash/signature

2. **Transfer to isolated network:**
   - USB drive
   - Internal file share
   - CD/DVD

3. **Install:**
   - Installer is self-contained
   - Includes .NET 8 runtime (~150MB)
   - No internet required

**See:** `docs/archive/DEPLOYMENT_SELF_CONTAINED.md` for details

---

## 📝 Post-Deployment

### Verification Steps

1. **Check Services:**
   ```powershell
   Get-Service ZLFileRelay*
   # Should show: Running
   ```

2. **Access Web Portal:**
   ```
   http://server:8080
   # Should load upload interface
   ```

3. **Test File Transfer:**
   - Upload file via web portal
   - Check transfer queue
   - Verify file appears at destination

4. **Review Logs:**
   ```
   C:\FileRelay\logs\
   ```

---

### Monitoring

**Service Status:** ConfigTool → Service Management tab  
**Logs:** `C:\FileRelay\logs\`  
**Windows Event Log:** Application → Source: ZLFileRelay  
**Web Portal:** `http://server:8080` (alive check)

---

## 🆘 Troubleshooting

### Service Won't Start

**Check:**
1. Service account permissions
2. Port 8080/8443 not in use
3. Configuration file valid
4. Logs for specific error

**Fix:**
```powershell
# Check port availability
netstat -ano | findstr "8080"

# Check service status
Get-Service ZLFileRelay*

# View logs
Get-Content C:\FileRelay\logs\zlrelay-*.log -Tail 50
```

---

### Web Portal Not Accessible

**Check:**
1. Service is running
2. Firewall allows port 8080 (or 8443 for HTTPS)
3. Correct URL (http://server:8080 or https://server:8443)
4. Authentication configured (Local Accounts or Entra ID)

**Test:**
```powershell
# Test locally on server
Invoke-WebRequest http://localhost:8080

# Check firewall
Get-NetFirewallRule | Where-Object {$_.DisplayName -like "*ZL File*"}
```

---

### Remote Management Not Working

**Check:**
1. SMB enabled on remote server
2. Administrative shares available (\\server\c$)
3. Remote Registry service running
4. Correct credentials

**Test:**
```powershell
# Test admin share
Test-Path \\server\c$

# Test service query
sc \\server query ZLFileRelay
```

---

## 📚 Additional Resources

- **[Installation Guide](INSTALLATION.md)** - Detailed installation steps
- **[Configuration Reference](CONFIGURATION.md)** - All settings explained
- **[ConfigTool Guide](CONFIGTOOL_QUICK_START.md)** - Using the management tool
- **[Remote Management](REMOTE_MANAGEMENT.md)** - Managing Server Core installations
- **[Quick Reference](DEPLOYMENT_QUICK_REFERENCE.md)** - Command cheatsheet

---

## 📞 Support

**Documentation:** `docs/` folder  
**Logs:** `C:\FileRelay\logs\`  
**Event Log:** Windows Application Log → Source: ZLFileRelay  
**Configuration:** `C:\Program Files\ZLFileRelay\appsettings.json`

---

**Version:** 1.0.0  
**Last Updated:** October 8, 2025  
**Platform:** Windows Server 2016+ (including Server Core)
