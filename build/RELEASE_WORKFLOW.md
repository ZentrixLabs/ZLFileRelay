# Release Workflow - Quick Guide

## Prerequisites

✅ **Environment Variables Set:**
- `$env:CODESIGN_CERT_SHA1` - Your code signing certificate thumbprint
- `$env:CODESIGN_TIMESTAMP_URL` - (Optional) Timestamp server URL (defaults to Sectigo)

## Quick Release (Recommended)

```powershell
cd build
.\release.ps1 -Version "1.1.2" -Sign
```

This will:
1. ✅ Update version numbers in all project files
2. ✅ Prompt you to build in Visual Studio (Release)
3. ✅ Sign all executables automatically
4. ✅ Prompt you to compile installer in Inno Setup
5. ✅ Create SHA256 checksum
6. ✅ Upload to GitHub (if upload-release.ps1 exists)

## Step-by-Step Manual Release

### Step 1: Update Version

```powershell
cd build
.\update-version.ps1 -Version "1.1.2"
```

This updates:
- All `.csproj` files
- `installer/ZLFileRelay.iss`

### Step 2: Build All Components

**Option A: Use Build Script**
```powershell
.\build-app.ps1
```

**Option B: Build in Visual Studio**
- Open `ZLFileRelay.sln`
- Set Configuration to **Release**
- Build Solution (Ctrl+Shift+B)

### Step 3: Sign Executables

```powershell
.\sign-app.ps1
```

This signs:
- ✅ `ZLFileRelay.Service.exe`
- ✅ `ZLFileRelay.WebPortal.exe`
- ✅ `ZLFileRelay.ConfigTool.exe`
- ✅ `ZLFileRelay.Core.dll`

**Verification:**
```powershell
Get-AuthenticodeSignature src\*\bin\Release\**\*.exe
```

Should show: `Status = Valid`

### Step 4: Publish Self-Contained (Optional)

If you need self-contained deployments:

```powershell
.\publish-selfcontained.ps1
```

### Step 5: Build Installer

1. Open **Inno Setup Compiler**
2. Open `installer/ZLFileRelay.iss`
3. Click **Build → Compile** (F9)
4. Installer will be signed automatically (if SignTool configured)

**Output Location:**
- `installer/output/ZLFileRelay-Setup-v1.1.2.exe`
- Or `artifacts/` if configured

### Step 6: Create Checksum

```powershell
$installer = "installer\output\ZLFileRelay-Setup-v1.1.2.exe"
$hash = (Get-FileHash -Algorithm SHA256 $installer).Hash
Set-Content -Path "$installer.sha256" -Value $hash
```

### Step 7: Upload to GitHub (Optional)

```powershell
.\upload-release.ps1 -Version "1.1.2" -InstallerPath "installer\output\ZLFileRelay-Setup-v1.1.2.exe"
```

## Environment Variables

### Required
```powershell
$env:CODESIGN_CERT_SHA1 = "YOUR_40_CHARACTER_THUMBPRINT"
```

### Optional
```powershell
$env:CODESIGN_TIMESTAMP_URL = "http://timestamp.sectigo.com/rfc3161"
```

**To set permanently:**
```powershell
[System.Environment]::SetEnvironmentVariable(
    "CODESIGN_CERT_SHA1",
    "YOUR_THUMBPRINT",
    "User"
)
```

## Verification Checklist

Before releasing, verify:

- [ ] Version numbers updated in all files
- [ ] All components built successfully
- [ ] All executables signed (Status = Valid)
- [ ] Installer compiled and signed
- [ ] Checksum file created
- [ ] Test installer on clean VM
- [ ] Release notes prepared

## Troubleshooting

### Certificate Not Found
```powershell
# Check certificate exists
Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Format-List Thumbprint, Subject
```

### SignTool Not Found
Install **Windows 10/11 SDK** which includes signtool.exe

### Timestamp Server Failed
The script will retry, but you can manually specify:
```powershell
.\sign-app.ps1 -TimestampUrl "http://timestamp.digicert.com"
```

## Files Signed

| Component | File | Location |
|-----------|------|----------|
| Service | `ZLFileRelay.Service.exe` | `src/ZLFileRelay.Service/bin/Release/net8.0/` |
| WebPortal | `ZLFileRelay.WebPortal.exe` | `src/ZLFileRelay.WebPortal/bin/Release/net8.0/` |
| ConfigTool | `ZLFileRelay.ConfigTool.exe` | `src/ZLFileRelay.ConfigTool/bin/Release/net8.0-windows/` |
| Core | `ZLFileRelay.Core.dll` | `src/ZLFileRelay.Core/bin/Release/net8.0/` |
| Installer | `ZLFileRelay-Setup-*.exe` | `installer/output/` (via Inno Setup) |

## Quick Commands

```powershell
# Full release
.\release.ps1 -Version "1.1.2" -Sign

# Just build
.\build-app.ps1

# Just sign
.\sign-app.ps1

# Update version
.\update-version.ps1 -Version "1.1.2"

# Verify signatures
Get-AuthenticodeSignature src\*\bin\Release\**\*.exe
```
