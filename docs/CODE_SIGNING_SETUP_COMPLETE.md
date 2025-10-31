# Code Signing Setup Complete ✅

**Date:** October 31, 2025  
**Status:** Complete - Ready for Production Signing

---

## 📋 What Was Added

### New Build Scripts

1. **`build/build-app.ps1`** - Build all components
   - Builds Service, WebPortal, ConfigTool
   - Prepares for code signing
   - Clean, organized output

2. **`build/sign-app.ps1`** - Sign executables
   - Signs all EXEs and main DLL
   - Uses `signtool.exe` from Windows SDK
   - Verifies signatures after signing
   - Supports environment variable for thumbprint

3. **`build/release.ps1`** - Complete release workflow
   - Orchestrates entire release process
   - Handles version updates
   - Optional code signing
   - Creates checksums
   - Opens output directory

### Updated Files

1. **`installer/ZLFileRelay.iss`**
   - Added SignTool configuration section
   - Instructions for Inno Setup signing
   - Ready for automated installer signing

2. **`build/README.md`**
   - Comprehensive script documentation
   - Usage examples
   - Troubleshooting guide

3. **`installer/README.md`**
   - Added code signing section
   - Step-by-step Inno Setup configuration
   - Certificate management instructions

### New Documentation

1. **`docs/CODE_SIGNING.md`**
   - Complete code signing guide
   - Prerequisites and setup
   - Workflows and best practices
   - Troubleshooting
   - CI/CD integration examples
   - Security considerations

---

## 🚀 Quick Start

### Basic Workflow (No Signing)

```powershell
# Just build
cd build
.\build-app.ps1
```

### Build + Sign Workflow

```powershell
# 1. Set your certificate thumbprint
$env:CODESIGN_CERT_SHA1 = "YOUR_THUMBPRINT_HERE"

# 2. Build all components
.\build-app.ps1

# 3. Sign all executables
.\sign-app.ps1
```

### Complete Release (Build + Sign + Publish + Installer)

```powershell
# One command does it all
.\release.ps1 -Version "1.0.0" -Sign
```

---

## 📦 What Gets Signed

### Application Components (4 files)

| File | Path | Description |
|------|------|-------------|
| `ZLFileRelay.Service.exe` | `Service/bin/Release/net8.0/` | Windows Service |
| `ZLFileRelay.WebPortal.exe` | `WebPortal/bin/Release/net8.0/` | Web Application |
| `ZLFileRelay.ConfigTool.exe` | `ConfigTool/bin/Release/net8.0-windows/` | WPF Config Tool |
| `ZLFileRelay.Core.dll` | `Core/bin/Release/net8.0/` | Shared Library |

### Installer

| File | Path | Description |
|------|------|-------------|
| `ZLFileRelaySetup-*.exe` | `installer/output/` | Installation Package |

---

## 🔧 Setup Requirements

### 1. Get Certificate Thumbprint

```powershell
# List your code signing certificates
Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert
```

Copy the **Thumbprint** (40-character hex string).

### 2. Set Environment Variable

```powershell
# Permanent (recommended)
[System.Environment]::SetEnvironmentVariable(
    "CODESIGN_CERT_SHA1",
    "YOUR_THUMBPRINT_HERE",
    "User"
)
```

### 3. Verify Windows SDK Installed

Scripts will auto-detect `signtool.exe` from:
- Windows Kits (via Visual Studio)
- Standalone Windows SDK
- PATH environment variable

---

## 📝 Typical Release Process

### Step 1: Update Version Numbers

Edit these files:
- `src/ZLFileRelay.Service/ZLFileRelay.Service.csproj`
- `src/ZLFileRelay.WebPortal/ZLFileRelay.WebPortal.csproj`
- `src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj`
- `src/ZLFileRelay.Core/ZLFileRelay.Core.csproj`
- `installer/ZLFileRelay.iss` (AppVersion line)
- `CHANGELOG.md`

### Step 2: Run Release Script

```powershell
cd build
.\release.ps1 -Version "1.0.0" -Sign
```

This will:
1. ✅ Remind you to update versions
2. ✅ Build all components
3. ✅ Sign executables (Service, WebPortal, ConfigTool, Core.dll)
4. ✅ Publish self-contained builds
5. ✅ Prompt to sign published files (optional)
6. ✅ Prompt to compile installer in Inno Setup
7. ✅ Create SHA256 checksum
8. ✅ Open output directory

### Step 3: Compile and Sign Installer

**In Inno Setup IDE:**
1. Open `installer/ZLFileRelay.iss`
2. Press **Ctrl+F9** to compile
3. Installer will auto-sign if SignTool configured

**Or command line:**
```powershell
iscc installer\ZLFileRelay.iss
```

### Step 4: Distribute

Files in `installer/output/`:
- ✅ `ZLFileRelaySetup-1.0.0.exe` (signed)
- ✅ `ZLFileRelaySetup-1.0.0.exe.sha256` (checksum)

---

## 🔐 Configuring Inno Setup Signing

### One-Time Setup

1. Open **Inno Setup IDE**
2. Go to **Tools → Configure Sign Tools...**
3. Click **Add**
4. **Name:** `signtool`
5. **Command:**
   ```
   "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe" sign /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com /sha1 YOUR_CERT_THUMBPRINT /d "ZL File Relay Setup" /du "https://github.com/ZentrixLabs/ZLFileRelay" $f
   ```
6. Replace `YOUR_CERT_THUMBPRINT` with your actual thumbprint
7. Click **OK**

### Enable in Script

Edit `installer/ZLFileRelay.iss`:
```ini
[Setup]
; ... other settings ...
SignTool=signtool    ; <-- Uncomment this line
```

Now installers will auto-sign during compilation!

---

## ✅ Verification

### Check Individual File

```powershell
# PowerShell
Get-AuthenticodeSignature "path\to\file.exe"

# Or using signtool
signtool verify /pa "path\to\file.exe"
```

### Check All Components

```powershell
# Quick verification script
$files = @(
    "src\ZLFileRelay.Service\bin\Release\net8.0\ZLFileRelay.Service.exe",
    "src\ZLFileRelay.WebPortal\bin\Release\net8.0\ZLFileRelay.WebPortal.exe",
    "src\ZLFileRelay.ConfigTool\bin\Release\net8.0-windows\ZLFileRelay.ConfigTool.exe"
)

foreach ($file in $files) {
    $sig = Get-AuthenticodeSignature $file
    Write-Host "$file : $($sig.Status)"
}
```

Expected output: All should show **"Valid"**

---

## 📚 Documentation Reference

| Document | Purpose |
|----------|---------|
| `build/README.md` | Build scripts documentation |
| `installer/README.md` | Installer setup and signing |
| `docs/CODE_SIGNING.md` | Complete code signing guide |
| This file | Quick setup summary |

---

## 🔍 Troubleshooting Quick Reference

### "signtool.exe not found"
→ Install Windows SDK via Visual Studio or standalone

### "Certificate not found"
→ Check thumbprint: `Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert`

### "Timestamp server unavailable"
→ Retry or use different server: `.\sign-app.ps1 -TimestampUrl "http://timestamp.sectigo.com"`

### "Access denied"
→ Run PowerShell as Administrator

### Build fails
→ Ensure .NET 8 SDK installed: `dotnet --version`

---

## 🎯 Next Steps

1. **Set up certificate thumbprint** as environment variable
2. **Test signing workflow** with development build
3. **Configure Inno Setup SignTool** for installer signing
4. **Run test release** using `release.ps1 -Version "1.0.0-test" -Sign`
5. **Verify all signatures** on a clean test machine
6. **Create production release** when ready

---

## 📖 Additional Resources

### Internal Documentation
- Build scripts: `build/README.md`
- Code signing: `docs/CODE_SIGNING.md`
- Installer: `installer/README.md`

### External References
- [SignTool Documentation](https://docs.microsoft.com/en-us/windows/win32/seccrypto/signtool)
- [Inno Setup Sign Tool](https://jrsoftware.org/ishelp/index.php?topic=setup_signtool)
- [Windows SDK Download](https://developer.microsoft.com/windows/downloads/windows-sdk/)

---

## ✨ Summary

You now have a **complete code signing solution** for ZL File Relay:

✅ **Build scripts** - Automated build process  
✅ **Signing scripts** - Automated code signing  
✅ **Release workflow** - End-to-end release automation  
✅ **Installer signing** - Inno Setup integration  
✅ **Documentation** - Comprehensive guides  
✅ **Verification** - Signature checking tools  

**Ready for production releases!** 🚀

---

**Questions?** See `docs/CODE_SIGNING.md` for detailed information.

