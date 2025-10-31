# ZL File Relay - Build & Release Scripts

This directory contains PowerShell scripts for building, signing, and releasing ZL File Relay.

## Prerequisites

### Required
- **Windows 10/11 or Windows Server 2019+**
- **.NET 8.0 SDK** - [Download](https://dot.net)
- **Git** - For repository management
- **Inno Setup 6.x** - [Download](https://jrsoftware.org/isinfo.php)

### For Code Signing
- **Windows SDK** - Contains `signtool.exe` (installed with Visual Studio or standalone)
- **Code Signing Certificate** - Valid Authenticode certificate
- **Environment Variable**: `CODESIGN_CERT_SHA1` - Set to your certificate thumbprint

## Scripts Overview

### `update-version.ps1` - Update Version Numbers
Automatically updates version across all project files and installer.

```powershell
# Update to version 1.2.3
.\update-version.ps1 -Version "1.2.3"
```

**Files Updated:**
- `src/ZLFileRelay.Core/ZLFileRelay.Core.csproj`
- `src/ZLFileRelay.Service/ZLFileRelay.Service.csproj`
- `src/ZLFileRelay.WebPortal/ZLFileRelay.WebPortal.csproj`
- `src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj`
- `installer/ZLFileRelay.iss` (MyAppVersion)

**Note:** CHANGELOG.md still needs to be updated manually.

### `build-app.ps1` - Build All Components
Builds all ZLFileRelay components (Service, WebPortal, ConfigTool) in Release configuration.

```powershell
# Build with default settings (Release, win-x64)
.\build-app.ps1

# Build with specific configuration
.\build-app.ps1 -Configuration Release -Runtime win-x64
```

**Output Locations:**
- Service: `src/ZLFileRelay.Service/bin/Release/net8.0/`
- WebPortal: `src/ZLFileRelay.WebPortal/bin/Release/net8.0/`
- ConfigTool: `src/ZLFileRelay.ConfigTool/bin/Release/net8.0-windows/`

### `sign-app.ps1` - Code Sign Executables
Signs all executables and main DLL files using your code signing certificate.

```powershell
# Sign using certificate thumbprint from environment variable
.\sign-app.ps1

# Sign with explicit thumbprint
.\sign-app.ps1 -Thumbprint "YOUR_CERT_THUMBPRINT_HERE"

# Sign with custom timestamp server
.\sign-app.ps1 -TimestampUrl "http://timestamp.digicert.com"
```

**Files Signed:**
- `ZLFileRelay.Service.exe`
- `ZLFileRelay.WebPortal.exe`
- `ZLFileRelay.ConfigTool.exe`
- `ZLFileRelay.Core.dll`

### `publish-selfcontained.ps1` - Publish Self-Contained
Creates self-contained deployments with .NET 8 runtime included.

```powershell
# Publish with defaults
.\publish-selfcontained.ps1

# Publish with specific configuration
.\publish-selfcontained.ps1 -Configuration Release -Runtime win-x64
```

**Output:** `publish/` directory with Service, WebPortal, and ConfigTool folders.

### `build-installer.ps1` - Build Complete Installer
Full workflow: publishes all components and creates Inno Setup installer.

```powershell
# Build installer with defaults
.\build-installer.ps1

# Build with custom Inno Setup path
.\build-installer.ps1 -InnoSetupPath "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

**Output:** `installer/output/ZLFileRelaySetup-*.exe`

### `release.ps1` - Complete Release Workflow
Orchestrates the entire release process: version update, build, sign, publish, and installer creation.

```powershell
# Release without code signing
.\release.ps1 -Version "1.0.0"

# Release WITH code signing
.\release.ps1 -Version "1.0.0" -Sign

# Release using existing build
.\release.ps1 -Version "1.0.0" -Sign -SkipBuild
```

## Typical Workflows

### Development Build (No Signing)

```powershell
# Build all components
.\build-app.ps1

# Test locally
# ... run components from bin directories ...
```

### Signed Development Build

```powershell
# Set certificate thumbprint (one time)
$env:CODESIGN_CERT_SHA1 = "YOUR_CERT_THUMBPRINT"

# Build and sign
.\build-app.ps1
.\sign-app.ps1

# Create installer (you'll sign it manually in Inno Setup)
.\build-installer.ps1
```

### Production Release (Full Workflow)

```powershell
# Set certificate thumbprint (one time)
$env:CODESIGN_CERT_SHA1 = "YOUR_CERT_THUMBPRINT"

# Run complete release workflow
.\release.ps1 -Version "1.0.0" -Sign

# The script will prompt you to:
# 1. Update version numbers in project files
# 2. Wait for build completion
# 3. Wait for code signing
# 4. Compile installer in Inno Setup GUI (with signing)
# 5. Verify final installer

# Output: installer/output/ZLFileRelaySetup-1.0.0.exe (signed)
```

## Code Signing Setup

### 1. Get Certificate Thumbprint

```powershell
# List available certificates
Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert

# Or list from LocalMachine store
Get-ChildItem Cert:\LocalMachine\My -CodeSigningCert
```

Copy the thumbprint (40-character hex string).

### 2. Set Environment Variable

**For Current Session:**
```powershell
$env:CODESIGN_CERT_SHA1 = "YOUR_THUMBPRINT_HERE"
```

**Permanently (User Level):**
```powershell
[System.Environment]::SetEnvironmentVariable(
    "CODESIGN_CERT_SHA1", 
    "YOUR_THUMBPRINT_HERE", 
    "User"
)
```

**Permanently (System Level - Requires Admin):**
```powershell
[System.Environment]::SetEnvironmentVariable(
    "CODESIGN_CERT_SHA1", 
    "YOUR_THUMBPRINT_HERE", 
    "Machine"
)
```

### 3. Verify Signing Works

```powershell
.\sign-app.ps1 -Configuration Release
```

Should sign all executables and verify signatures.

## Signing the Installer

The installer itself should be signed in Inno Setup. Configure SignTool in Inno Setup GUI:

**Tools → Configure Sign Tools...**

Add a sign tool named `signtool` with command:
```
"C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe" sign /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com /sha1 YOUR_THUMBPRINT /d "ZL File Relay Setup" /du "https://github.com/ZentrixLabs/ZLFileRelay" $f
```

Then in `installer/ZLFileRelay.iss`, ensure this line is present:
```
SignTool=signtool
```

## Troubleshooting

### "signtool.exe not found"
**Solution:** Install Windows 10/11 SDK from Visual Studio Installer or standalone.

### "Certificate not found"
**Solutions:**
1. Verify certificate is installed: `Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert`
2. Check thumbprint is correct (no spaces)
3. Try LocalMachine store instead: `Cert:\LocalMachine\My`

### "Timestamp server unavailable"
**Solution:** Try different timestamp server:
- DigiCert: `http://timestamp.digicert.com`
- Sectigo: `http://timestamp.sectigo.com`
- GlobalSign: `http://timestamp.globalsign.com`

### "Access denied" when signing
**Solution:** Run PowerShell as Administrator, or ensure you have access to the certificate's private key.

### Build fails with "project not found"
**Solution:** Ensure you're running scripts from the `build/` directory or that paths are correct.

## CI/CD Integration

For automated builds in CI/CD:

```yaml
# Example Azure Pipelines
- task: PowerShell@2
  displayName: 'Build ZLFileRelay'
  inputs:
    filePath: 'build/build-app.ps1'
    
- task: PowerShell@2
  displayName: 'Sign Executables'
  inputs:
    filePath: 'build/sign-app.ps1'
  env:
    CODESIGN_CERT_SHA1: $(CertificateThumbprint)
```

## Version Management

### Automated Version Update

Use the `update-version.ps1` script to automatically update all project files:

```powershell
.\update-version.ps1 -Version "1.2.3"
```

This updates:
- ✅ All 4 .csproj files (Core, Service, WebPortal, ConfigTool)
- ✅ Inno Setup installer script (MyAppVersion)

Manual update still required:
- ⚠️ `CHANGELOG.md` - Add release notes

The `release.ps1` script calls `update-version.ps1` automatically.

## Output Structure

After running `publish-selfcontained.ps1`:

```
publish/
├── Service/           # Windows Service (self-contained)
├── WebPortal/         # Web application (self-contained)
├── ConfigTool/        # WPF app (self-contained, single file)
├── appsettings.json   # Configuration template
└── docs/              # User documentation
```

After running `build-installer.ps1`:

```
installer/output/
├── ZLFileRelaySetup-1.0.0.exe        # Installer
└── ZLFileRelaySetup-1.0.0.exe.sha256 # Checksum
```

## Support

For issues with build scripts:
- Check script output for specific error messages
- Ensure all prerequisites are installed
- Verify paths and permissions
- Check PowerShell execution policy: `Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned`

For product issues:
- See main repository README.md
- Check docs/README.md for documentation

