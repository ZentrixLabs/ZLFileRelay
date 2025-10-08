# WinRM Setup Guide for ZL File Relay

## Overview

**WinRM (Windows Remote Management)** is required for full remote management capabilities in ZL File Relay ConfigTool.

Most enterprise environments already have WinRM enabled via Group Policy, but if you're setting up manually or in a lab environment, follow this guide.

---

## ⚡ Quick Setup (Recommended)

On each target server that will be managed remotely:

```powershell
# Run in PowerShell as Administrator:
Enable-PSRemoting -Force
```

That's it! This single command:
- ✅ Enables WinRM service
- ✅ Creates firewall rules
- ✅ Configures listeners
- ✅ Sets service to auto-start

---

## 🔍 Verify WinRM is Working

### On the Target Server

```powershell
# Test local WinRM:
Test-WSMan

# Expected output:
# wsmid           : http://schemas.dmtf.org/wbem/wsman/identity/1/wsmanidentity.xsd
# ProtocolVersion : http://schemas.dmtf.org/wbem/wsman/1/wsman.xsd
# ProductVendor   : Microsoft Corporation
# ProductVersion  : OS: ...
```

### From Your Workstation

```powershell
# Test remote WinRM (replace SERVER01 with your server name):
Test-WSMan -ComputerName SERVER01

# If successful, you'll see similar output as above
```

---

## 🔐 Firewall Configuration

`Enable-PSRemoting` automatically configures firewall rules, but if you need to verify or configure manually:

### Check Firewall Rules

```powershell
Get-NetFirewallRule -DisplayName "Windows Remote Management*" | Select-Object DisplayName, Enabled
```

### Enable Rules Manually (if needed)

```powershell
# Enable HTTP-In rule (port 5985):
Enable-NetFirewallRule -DisplayName "Windows Remote Management (HTTP-In)"

# For HTTPS (port 5986) - if using SSL:
Enable-NetFirewallRule -DisplayName "Windows Remote Management (HTTPS-In)"
```

### Ports Required

| Protocol | Port | Purpose |
|----------|------|---------|
| HTTP | 5985 | Default WinRM (unencrypted, but Kerberos-encrypted in domain) |
| HTTPS | 5986 | HTTPS WinRM (SSL encrypted) |

**Note:** In domain environments, even HTTP WinRM is encrypted via Kerberos authentication.

---

## 👥 User Permissions

### Remote Management Users Group

By default, only **Administrators** can use WinRM. To allow other users:

```powershell
# Add user to Remote Management Users group:
Add-LocalGroupMember -Group "Remote Management Users" -Member "DOMAIN\username"
```

### For ZL File Relay ConfigTool

The user running ConfigTool needs:
- ✅ Member of **Administrators** group on target server (recommended)
- ✅ OR member of **Remote Management Users** + administrative privileges

---

## 🏢 Enterprise Deployment via Group Policy

For deploying WinRM across multiple servers, use Group Policy:

### 1. Create or Edit Group Policy

```
Computer Configuration
  └── Policies
      └── Administrative Templates
          └── Windows Components
              └── Windows Remote Management (WinRM)
                  └── WinRM Service
```

### 2. Configure Settings

| Setting | Value | Purpose |
|---------|-------|---------|
| Allow remote server management through WinRM | **Enabled** | Turns on WinRM |
| IPv4 filter | `*` | Allow all IPs (or restrict as needed) |
| IPv6 filter | `*` | Allow all IPs (or restrict as needed) |

### 3. Configure Windows Firewall

```
Computer Configuration
  └── Policies
      └── Windows Settings
          └── Security Settings
              └── Windows Defender Firewall with Advanced Security
```

- Enable inbound rule for **Windows Remote Management (HTTP-In)**
- Scope to appropriate networks (e.g., corporate network only)

### 4. Auto-Start WinRM Service

```
Computer Configuration
  └── Preferences
      └── Control Panel Settings
          └── Services
```

- Service: WinRM
- Startup: Automatic
- Service action: Start service

---

## 🔒 Security Best Practices

### 1. Network Restrictions

```powershell
# Restrict WinRM to specific IP ranges (example):
Set-Item WSMan:\localhost\Service\IPv4Filter -Value "192.168.1.0/24"
```

### 2. HTTPS Configuration (Optional)

For environments requiring SSL encryption:

```powershell
# Create self-signed certificate:
$cert = New-SelfSignedCertificate -DnsName "server01.domain.com" -CertStoreLocation Cert:\LocalMachine\My

# Configure HTTPS listener:
winrm create winrm/config/Listener?Address=*+Transport=HTTPS @{Hostname="server01.domain.com"; CertificateThumbprint=$cert.Thumbprint}
```

### 3. Audit Logging

Enable WinRM logging for security auditing:

```powershell
# Enable WinRM logging:
Set-Item WSMan:\localhost\Plugin\Microsoft.PowerShell\Quotas\MaxMemoryPerShellMB 1024
```

View logs in Event Viewer:
- **Applications and Services Logs → Microsoft → Windows → Windows Remote Management**

---

## 🐛 Troubleshooting

### Issue: "Test-WSMan" fails with access denied

**Solution:**
```powershell
# Check WinRM service is running:
Get-Service WinRM

# If stopped, start it:
Start-Service WinRM
Set-Service WinRM -StartupType Automatic
```

---

### Issue: "Cannot connect from workstation"

**Checklist:**
1. ✅ WinRM enabled on server? (`Test-WSMan` on server)
2. ✅ Firewall allows port 5985? (`Test-NetConnection -ComputerName SERVER01 -Port 5985`)
3. ✅ User has permissions? (Member of Administrators or Remote Management Users)
4. ✅ Server name resolves? (`ping SERVER01`)

---

### Issue: "Access is denied" when running remote commands

**Solution:**
```powershell
# Check if user is in correct groups on target server:
Get-LocalGroupMember -Group "Administrators"
Get-LocalGroupMember -Group "Remote Management Users"

# Add user if needed:
Add-LocalGroupMember -Group "Administrators" -Member "DOMAIN\username"
```

---

### Issue: Trusted Hosts error (workgroup environments)

If not in a domain, you may need to configure trusted hosts:

**On Workstation:**
```powershell
# Add server to trusted hosts:
Set-Item WSMan:\localhost\Client\TrustedHosts -Value "SERVER01" -Force

# Or allow all (less secure):
Set-Item WSMan:\localhost\Client\TrustedHosts -Value "*" -Force

# View current trusted hosts:
Get-Item WSMan:\localhost\Client\TrustedHosts
```

**Note:** This is only needed for workgroup environments. Domain environments use Kerberos automatically.

---

## ✅ Testing with ConfigTool

After enabling WinRM:

1. Open **ZL File Relay ConfigTool**
2. Go to **Remote Server** tab
3. Enter server name
4. Click **"Test Connection"**
5. Check results for WinRM test:
   - ✅ **"WinRM is available and accessible"** = Success!
   - ⚠️ **"WinRM not available"** = Follow troubleshooting above

---

## 📋 Quick Reference Commands

```powershell
# Enable WinRM:
Enable-PSRemoting -Force

# Test WinRM locally:
Test-WSMan

# Test WinRM remotely:
Test-WSMan -ComputerName SERVER01

# Check WinRM service:
Get-Service WinRM

# View WinRM configuration:
winrm get winrm/config

# Check firewall rules:
Get-NetFirewallRule -DisplayName "Windows Remote Management*"

# Add user to remote management:
Add-LocalGroupMember -Group "Administrators" -Member "DOMAIN\user"

# Test PowerShell Remoting:
Invoke-Command -ComputerName SERVER01 -ScriptBlock { Get-ComputerInfo }
```

---

## 📊 Network Requirements Summary

| From | To | Protocol | Port | Purpose |
|------|----|---------| -----|---------|
| Workstation | Server | TCP | 445 | SMB (file access) |
| Workstation | Server | TCP | 135 | RPC (service control) |
| Workstation | Server | TCP | 5985 | WinRM HTTP |
| Workstation | Server | TCP | 5986 | WinRM HTTPS (optional) |

---

## 🎯 Summary

**For most enterprise environments:**
- ✅ WinRM likely already enabled via GPO
- ✅ Just verify with `Test-WSMan` on servers
- ✅ ConfigTool will detect and use it automatically

**For manual setup:**
- ✅ Run `Enable-PSRemoting -Force` on each server
- ✅ Verify with ConfigTool connection test
- ✅ Done!

---

**Related Documentation:**
- [Remote Management Guide](REMOTE_MANAGEMENT.md)
- [Installation Guide](INSTALLATION.md)
- [Configuration Reference](CONFIGURATION.md)
