# ZL File Relay - IIS-Free Architecture for DMZ/OT

**🎉 MAJOR ADVANTAGE:** No IIS dependency! Perfect for air-gapped OT environments.

---

## 🏗️ Updated Architecture

### Two Windows Services + One GUI

```
┌──────────────────────────────────────────────────────────┐
│  ZL FILE RELAY DEPLOYMENT (DMZ/OT Compatible)           │
│  • Self-Contained with .NET 8 Runtime                    │
│  • No IIS Required                                       │
│  • No ASP.NET Core Hosting Bundle Required               │
│  • Installer: ~150MB (single .exe)                       │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│  Windows Service 1: ZLFileRelay                          │
├──────────────────────────────────────────────────────────┤
│  Display Name: ZL File Relay - File Transfer            │
│  Description:  Automated file transfer (DMZ→SCADA)       │
│  Startup:      Automatic                                 │
│  Account:      Local System (or custom service account)  │
│  Functions:                                              │
│    • Watch upload directory for new files                │
│    • Queue files for processing                          │
│    • Transfer via SSH/SCP or SMB/CIFS                    │
│    • Retry with exponential backoff                      │
│    • File verification                                   │
│    • Logging to C:\FileRelay\logs                        │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│  Windows Service 2: ZLFileRelay.WebPortal                │
├──────────────────────────────────────────────────────────┤
│  Display Name: ZL File Relay - Web Portal               │
│  Description:  Web upload interface (Kestrel)            │
│  Startup:      Automatic                                 │
│  Account:      Local System (or custom service account)  │
│  Ports:        8080 (HTTP), 8443 (HTTPS)                │
│  Functions:                                              │
│    • ASP.NET Core Kestrel web server                     │
│    • Multi-file upload interface                         │
│    • Windows Authentication                              │
│    • User subdirectories                                 │
│    • File validation                                     │
│    • Logging to C:\FileRelay\logs                        │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│  Desktop Application: ZLFileRelay.ConfigTool             │
├──────────────────────────────────────────────────────────┤
│  Type:         WPF with ModernWPF (Fluent Design)       │
│  Functions:                                              │
│    • Configure services                                  │
│    • Manage Windows Services (start/stop/install)        │
│    • Generate SSH keys                                   │
│    • Test connections                                    │
│    • View logs                                           │
│    • Manage service accounts                             │
└──────────────────────────────────────────────────────────┘
```

---

## ✅ Why This is PERFECT for DMZ/OT

### Zero External Dependencies
- ✅ **No IIS required**
- ✅ **No ASP.NET Core Hosting Bundle required**
- ✅ **No .NET installation required** (bundled)
- ✅ Works on **Windows Server Core** (headless)
- ✅ Works on **air-gapped networks**
- ✅ **Single 150MB installer**

### Windows Authentication Still Works
- ✅ Kestrel supports Windows Authentication via Negotiate
- ✅ No IIS needed for authentication
- ✅ Group-based authorization functional
- ✅ User identity captured

### Professional Deployment
- ✅ Two Windows Services (managed via Services.msc)
- ✅ Automatic startup
- ✅ Service recovery options available
- ✅ Event Log integration
- ✅ Standard Windows administration

---

## 🌐 Accessing the Web Portal

### On the Server
```
http://localhost:8080
http://localhost:8080/Upload
```

### From Network Clients
```
http://server-name:8080
http://server-ip:8080
http://server-name:8080/Upload
```

### With HTTPS (if certificate configured)
```
https://server-name:8443
https://server-name:8443/Upload
```

---

## 🔒 Security

### Firewall Configuration
Installer automatically creates rule:
```powershell
netsh advfirewall firewall add rule name="ZL File Relay Web Portal" dir=in action=allow protocol=TCP localport=8080
```

### Windows Authentication
Works natively with Kestrel:
- Negotiate authentication (NTLM/Kerberos)
- User identity available
- Group membership checked
- No IIS required!

### HTTPS (Optional)
Configure certificate in appsettings.json:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:8443",
        "Certificate": {
          "Path": "path/to/cert.pfx",
          "Password": "cert-password"
        }
      }
    }
  }
}
```

---

## 🎯 Service Management

### Via Services.msc
Both services appear in Windows Services:
- **ZLFileRelay** - File Transfer
- **ZLFileRelay.WebPortal** - Web Upload

### Via PowerShell
```powershell
# Check status
Get-Service ZLFileRelay*

# Start services
Start-Service ZLFileRelay
Start-Service ZLFileRelay.WebPortal

# Stop services
Stop-Service ZLFileRelay
Stop-Service ZLFileRelay.WebPortal

# Check if listening
netstat -ano | Select-String "8080"
```

### Via Config Tool
- Start/Stop both services
- View status
- Install/Uninstall
- View logs

---

## 📊 Comparison: IIS vs Kestrel Service

| Feature | IIS | Kestrel Service |
|---------|-----|-----------------|
| **Dependencies** | IIS + ANCM | None |
| **Installer Size** | ~210MB | ~150MB |
| **Server Core Support** | Limited | ✅ Full |
| **DMZ/OT Deployment** | Complex | ✅ Simple |
| **Windows Auth** | ✅ Yes | ✅ Yes |
| **HTTPS** | ✅ Yes | ✅ Yes |
| **Configuration** | IIS Manager | appsettings.json |
| **Troubleshooting** | Complex | ✅ Simpler |
| **Startup Time** | Slower | ✅ Faster |
| **Resource Usage** | Higher | ✅ Lower |

**Winner for DMZ/OT:** Kestrel Service 🏆

---

## 🚀 Deployment Process

### What Installer Does
1. ✅ Copies all files (with .NET 8)
2. ✅ Creates directories
3. ✅ Installs Service 1: File Transfer
4. ✅ Installs Service 2: Web Portal (Kestrel)
5. ✅ Configures firewall (port 8080)
6. ✅ Starts both services
7. ✅ Launches Config Tool

### User Experience
```
User runs: ZLFileRelay-Setup-v1.0.0-SelfContained.exe
  ↓
Installs in ~2 minutes
  ↓
Both services running automatically
  ↓
Web portal accessible at http://server:8080
  ↓
No additional configuration needed!
  ↓
Ready to upload files! 🎉
```

---

## 📝 Configuration Examples

### Change Port
Edit `appsettings.json`:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://*:80"
      }
    }
  }
}
```

Or set via environment variable:
```powershell
sc config ZLFileRelay.WebPortal binPath="C:\...\ZLFileRelay.WebPortal.exe --urls http://*:80"
```

### Enable HTTPS Only
```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443",
        "Certificate": {
          "Path": "cert.pfx",
          "Password": "password"
        }
      }
    }
  }
}
```

---

## 🎊 Benefits Summary

### For IT Operations
- ✅ Standard Windows Service management
- ✅ Works with existing monitoring tools
- ✅ Event Log integration
- ✅ Service recovery policies
- ✅ Clean start/stop/restart

### For Security Teams
- ✅ Fewer attack vectors (no IIS)
- ✅ Simpler security audit
- ✅ Smaller attack surface
- ✅ No additional components to patch
- ✅ Self-contained = known good version

### For DMZ/OT Deployment
- ✅ **ZERO internet requirement**
- ✅ **ZERO additional installs**
- ✅ **Works on Server Core**
- ✅ **Single 150MB installer**
- ✅ **Professional deployment experience**

---

## 🔍 Troubleshooting

### Check Services
```powershell
Get-Service ZLFileRelay*
```

### Check Web Portal is Listening
```powershell
netstat -ano | findstr 8080
Test-NetConnection localhost -Port 8080
```

### View Logs
```powershell
Get-Content C:\FileRelay\logs\zlfilerelay-web-*.log -Tail 50 -Wait
```

### Test from Browser
```
http://localhost:8080
http://server-name:8080
```

---

**This is a GAME CHANGER for DMZ/OT deployment!** 🎉

No IIS = Simpler, More Reliable, Perfect for Restricted Environments


