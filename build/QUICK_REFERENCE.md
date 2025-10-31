# Code Signing Quick Reference Card

**One-page cheat sheet for ZL File Relay code signing**

---

## 🔧 One-Time Setup

```powershell
# 1. Get certificate thumbprint
Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert

# 2. Set environment variable (permanent)
[System.Environment]::SetEnvironmentVariable("CODESIGN_CERT_SHA1", "YOUR_THUMBPRINT", "User")

# 3. Restart PowerShell to load the variable
```

---

## 🚀 Common Commands

### Build Only (No Signing)
```powershell
cd build
.\build-app.ps1
```

### Build + Sign
```powershell
cd build
.\build-app.ps1
.\sign-app.ps1
```

### Complete Release (Interactive)
```powershell
cd build
.\release.ps1 -Version "1.0.0" -Sign
```

### Verify Signatures
```powershell
# Check one file
Get-AuthenticodeSignature "path\to\file.exe"

# Check all built files
Get-AuthenticodeSignature src\*\bin\Release\**\*.exe
```

---

## 📦 What Gets Signed

**Application Components:**
- `ZLFileRelay.Service.exe` (Windows Service)
- `ZLFileRelay.WebPortal.exe` (Web App)
- `ZLFileRelay.ConfigTool.exe` (GUI Tool)
- `ZLFileRelay.Core.dll` (Shared Library)

**Installer:**
- `ZLFileRelaySetup-*.exe` (via Inno Setup)

---

## 🔐 Inno Setup Signing

**Configure once in Inno Setup IDE:**
1. Tools → Configure Sign Tools...
2. Add → Name: `signtool`
3. Command:
```
"C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe" sign /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com /sha1 YOUR_THUMBPRINT /d "ZL File Relay Setup" $f
```

**Enable in installer/ZLFileRelay.iss:**
```ini
SignTool=signtool
```

---

## ⚡ Script Options

### build-app.ps1
```powershell
.\build-app.ps1 [-Configuration Release] [-Runtime win-x64]
```

### sign-app.ps1
```powershell
.\sign-app.ps1 [-Thumbprint "..."] [-TimestampUrl "..."]
```

### release.ps1
```powershell
.\release.ps1 -Version "1.0.0" [-Sign] [-SkipBuild]
```

---

## 🆘 Quick Troubleshooting

| Problem | Solution |
|---------|----------|
| signtool not found | Install Windows SDK |
| Certificate not found | Check thumbprint with `Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert` |
| Timestamp failure | Retry or use `-TimestampUrl "http://timestamp.sectigo.com"` |
| Access denied | Run as Administrator |
| Invalid signature | Check certificate hasn't expired |

---

## 📁 Output Locations

**After build:**
- Service: `src/ZLFileRelay.Service/bin/Release/net8.0/`
- WebPortal: `src/ZLFileRelay.WebPortal/bin/Release/net8.0/`
- ConfigTool: `src/ZLFileRelay.ConfigTool/bin/Release/net8.0-windows/`

**After publish:**
- All: `publish/` directory

**After installer:**
- Installer: `installer/output/`

---

## 🎯 Typical Release Workflow

```powershell
# 1. Update versions in .csproj files and ZLFileRelay.iss
# 2. Run release script
cd build
.\release.ps1 -Version "1.0.0" -Sign

# 3. Script will guide you through:
#    - Building
#    - Signing
#    - Publishing
#    - Creating installer

# 4. Output: installer/output/ZLFileRelaySetup-1.0.0.exe (signed)
```

---

## 📚 Full Documentation

- **Build Scripts:** `build/README.md`
- **Code Signing:** `docs/CODE_SIGNING.md`
- **Setup Complete:** `docs/CODE_SIGNING_SETUP_COMPLETE.md`
- **Installer:** `installer/README.md`

---

**Save this file for quick reference!** 📋

