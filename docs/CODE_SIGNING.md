# Code Signing Guide - ZL File Relay

This guide covers code signing for ZL File Relay executables and installer.

## Overview

Code signing provides:
- ✅ Verifies software authenticity
- ✅ Identifies publisher
- ✅ Prevents tampering
- ✅ Reduces SmartScreen warnings
- ✅ Builds trust with users

## What Gets Signed

### Application Components (via `sign-app.ps1`)
1. **ZLFileRelay.Service.exe** - Windows Service for file transfer
2. **ZLFileRelay.WebPortal.exe** - Web application service
3. **ZLFileRelay.ConfigTool.exe** - WPF configuration tool
4. **ZLFileRelay.Core.dll** - Shared library

### Installer (via Inno Setup)
- **ZLFileRelaySetup-*.exe** - Installation package

## Prerequisites

### 1. Code Signing Certificate

You need a valid Authenticode code signing certificate from a trusted CA:

**Commercial Options:**
- DigiCert
- Sectigo (formerly Comodo)
- GlobalSign
- GoDaddy

**Self-Signed for Testing:**
```powershell
# Create test certificate (NOT for production!)
New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=Test Code Signing" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(2)
```

### 2. Windows SDK

Install Windows 10/11 SDK for `signtool.exe`:
- Via Visual Studio Installer (recommended)
- Or standalone: https://developer.microsoft.com/windows/downloads/windows-sdk/

### 3. Certificate Thumbprint

Get your certificate thumbprint:

```powershell
# List code signing certificates
Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert

# Output shows certificates with thumbprints
```

Example output:
```
Thumbprint                                Subject
----------                                -------
A1B2C3D4E5F6... (40 chars)               CN=Your Company, O=Your Company...
```

### 4. Environment Variable (Recommended)

Set your thumbprint as an environment variable:

```powershell
# For current session
$env:CODESIGN_CERT_SHA1 = "YOUR_THUMBPRINT_HERE"

# Permanently (user level)
[System.Environment]::SetEnvironmentVariable(
    "CODESIGN_CERT_SHA1",
    "YOUR_THUMBPRINT_HERE",
    "User"
)

# Permanently (system level - requires admin)
[System.Environment]::SetEnvironmentVariable(
    "CODESIGN_CERT_SHA1",
    "YOUR_THUMBPRINT_HERE",
    "Machine"
)
```

## Quick Start

### Simple Workflow (Build + Sign)

```powershell
# 1. Set certificate thumbprint
$env:CODESIGN_CERT_SHA1 = "YOUR_THUMBPRINT"

# 2. Build all components
cd build
.\build-app.ps1

# 3. Sign all executables
.\sign-app.ps1

# 4. Verify signatures
Get-AuthenticodeSignature ..\src\ZLFileRelay.Service\bin\Release\net8.0\*.exe
Get-AuthenticodeSignature ..\src\ZLFileRelay.WebPortal\bin\Release\net8.0\*.exe
Get-AuthenticodeSignature ..\src\ZLFileRelay.ConfigTool\bin\Release\net8.0-windows\*.exe
```

### Complete Release Workflow

```powershell
# Full release with signing
cd build
.\release.ps1 -Version "1.0.0" -Sign
```

This script will:
1. ✅ Prompt for version updates
2. ✅ Build all components
3. ✅ Sign executables
4. ✅ Publish self-contained
5. ✅ Prompt to build installer in Inno Setup
6. ✅ Create checksums
7. ✅ Open output folder

## Detailed Steps

### Step 1: Build Components

```powershell
cd build
.\build-app.ps1 -Configuration Release
```

**Output:**
- `src/ZLFileRelay.Service/bin/Release/net8.0/ZLFileRelay.Service.exe`
- `src/ZLFileRelay.WebPortal/bin/Release/net8.0/ZLFileRelay.WebPortal.exe`
- `src/ZLFileRelay.ConfigTool/bin/Release/net8.0-windows/ZLFileRelay.ConfigTool.exe`

### Step 2: Sign Executables

```powershell
.\sign-app.ps1 -Thumbprint "YOUR_CERT_THUMBPRINT"
```

Or use environment variable:
```powershell
$env:CODESIGN_CERT_SHA1 = "YOUR_CERT_THUMBPRINT"
.\sign-app.ps1
```

**Custom options:**
```powershell
.\sign-app.ps1 `
    -Thumbprint "YOUR_THUMBPRINT" `
    -TimestampUrl "http://timestamp.sectigo.com" `
    -Description "ZL File Relay" `
    -DescriptionUrl "https://zentrixlabs.com"
```

### Step 3: Publish Self-Contained

```powershell
.\publish-selfcontained.ps1
```

This creates the `publish/` directory with all components.

**Important:** If you want published files signed, you need to sign them again after publishing:
- Option A: Manually sign files in `publish/` directories
- Option B: Use the `release.ps1` workflow which prompts you

### Step 4: Sign Installer

#### Option A: Configure Inno Setup IDE (Recommended)

1. Open Inno Setup IDE
2. Go to **Tools → Configure Sign Tools...**
3. Click **Add**
4. Name: `signtool`
5. Command line:
```
"C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe" sign /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com /sha1 YOUR_CERT_THUMBPRINT /d "ZL File Relay Setup" /du "https://github.com/ZentrixLabs/ZLFileRelay" $f
```
6. Replace `YOUR_CERT_THUMBPRINT` with your actual thumbprint
7. Adjust SDK path if needed

Then edit `installer/ZLFileRelay.iss`:
```ini
[Setup]
; ... other settings ...
SignTool=signtool
```

Now when you compile the installer in Inno Setup, it will automatically sign it.

#### Option B: Manual Signing

```powershell
# After building installer
$thumbprint = "YOUR_CERT_THUMBPRINT"
$installer = "installer\output\ZLFileRelaySetup-1.0.0.exe"

signtool sign `
    /fd SHA256 `
    /td SHA256 `
    /tr http://timestamp.digicert.com `
    /sha1 $thumbprint `
    /d "ZL File Relay Setup" `
    /du "https://github.com/ZentrixLabs/ZLFileRelay" `
    "$installer"

# Verify
signtool verify /pa "$installer"
```

## Verification

### Verify Single File

```powershell
# Using Get-AuthenticodeSignature
Get-AuthenticodeSignature "path\to\file.exe"

# Using signtool
signtool verify /pa "path\to\file.exe"

# Detailed verification
signtool verify /v /pa "path\to\file.exe"
```

### Verify All Components

```powershell
# PowerShell script to check all
$files = @(
    "src\ZLFileRelay.Service\bin\Release\net8.0\ZLFileRelay.Service.exe",
    "src\ZLFileRelay.WebPortal\bin\Release\net8.0\ZLFileRelay.WebPortal.exe",
    "src\ZLFileRelay.ConfigTool\bin\Release\net8.0-windows\ZLFileRelay.ConfigTool.exe",
    "installer\output\ZLFileRelaySetup-1.0.0.exe"
)

foreach ($file in $files) {
    Write-Host "`nChecking: $file" -ForegroundColor Cyan
    if (Test-Path $file) {
        $sig = Get-AuthenticodeSignature $file
        Write-Host "Status: $($sig.Status)" -ForegroundColor $(if ($sig.Status -eq 'Valid') { 'Green' } else { 'Red' })
        Write-Host "Signer: $($sig.SignerCertificate.Subject)"
        Write-Host "Timestamp: $($sig.TimeStamperCertificate.NotAfter)"
    } else {
        Write-Host "File not found!" -ForegroundColor Red
    }
}
```

## Timestamp Servers

Using a timestamp server is **critical** - it allows your signature to remain valid even after your certificate expires.

**Recommended Servers:**
- DigiCert: `http://timestamp.digicert.com`
- Sectigo: `http://timestamp.sectigo.com`
- GlobalSign: `http://timestamp.globalsign.com`

## Troubleshooting

### "signtool.exe not found"

**Solution:** Install Windows SDK
```powershell
# Check if signtool exists
where.exe signtool

# Or manually locate
$kitsRoot = "C:\Program Files (x86)\Windows Kits\10\bin"
Get-ChildItem $kitsRoot -Recurse -Filter "signtool.exe"
```

### "Certificate not valid for signing"

**Solution:** Ensure certificate has "Code Signing" purpose
```powershell
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.Thumbprint -eq "YOUR_THUMBPRINT" }

# Check Enhanced Key Usage
$cert.EnhancedKeyUsageList
# Should include "Code Signing (1.3.6.1.5.5.7.3.3)"
```

### "Access denied" or "Private key not found"

**Solutions:**
1. Ensure you have access to private key
2. Try running PowerShell as Administrator
3. Check certificate is in correct store
4. If certificate is on smart card/token, ensure it's connected

### "Timestamp server unavailable"

**Solution:** Try different timestamp server
```powershell
.\sign-app.ps1 -TimestampUrl "http://timestamp.sectigo.com"
```

### "The specified timestamp server returned an unexpected response"

**Solution:** 
- Retry (timestamp servers occasionally timeout)
- Try different timestamp server
- Check internet connection
- Add retry logic to script

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build and Sign

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Import Certificate
      env:
        CERTIFICATE_BASE64: ${{ secrets.CERT_BASE64 }}
        CERTIFICATE_PASSWORD: ${{ secrets.CERT_PASSWORD }}
      run: |
        $certBytes = [Convert]::FromBase64String($env:CERTIFICATE_BASE64)
        [IO.File]::WriteAllBytes("cert.pfx", $certBytes)
        Import-PfxCertificate -FilePath cert.pfx -CertStoreLocation Cert:\CurrentUser\My -Password (ConvertTo-SecureString $env:CERTIFICATE_PASSWORD -AsPlainText -Force)
        $thumbprint = (Get-PfxCertificate -FilePath cert.pfx).Thumbprint
        echo "CODESIGN_CERT_SHA1=$thumbprint" >> $env:GITHUB_ENV
    
    - name: Build
      run: .\build\build-app.ps1
    
    - name: Sign
      run: .\build\sign-app.ps1
    
    - name: Publish
      run: .\build\publish-selfcontained.ps1
    
    - name: Upload Artifacts
      uses: actions/upload-artifact@v3
      with:
        name: signed-binaries
        path: publish/
```

### Azure DevOps Example

```yaml
trigger:
  tags:
    include:
    - v*

pool:
  vmImage: 'windows-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '8.0.x'

- task: AzureKeyVault@2
  inputs:
    azureSubscription: 'Your-Service-Connection'
    KeyVaultName: 'your-keyvault'
    SecretsFilter: 'CodeSigningCertThumbprint'

- powershell: |
    .\build\build-app.ps1
  displayName: 'Build Components'

- powershell: |
    $env:CODESIGN_CERT_SHA1 = "$(CodeSigningCertThumbprint)"
    .\build\sign-app.ps1
  displayName: 'Sign Executables'

- task: PublishBuildArtifacts@1
  inputs:
    pathToPublish: 'publish'
    artifactName: 'signed-release'
```

## Best Practices

1. ✅ **Always use timestamp servers** - Signatures remain valid after cert expires
2. ✅ **Sign immediately after build** - Reduces tampering window
3. ✅ **Verify signatures** - Always verify after signing
4. ✅ **Use environment variables** - Don't hardcode thumbprints in scripts
5. ✅ **Protect private keys** - Use HSM or secure storage for production
6. ✅ **Sign all executables** - Don't skip any user-facing EXEs or DLLs
7. ✅ **Sign installer too** - Most important for user trust
8. ✅ **Document your certificate** - Track expiration dates
9. ✅ **Test on clean systems** - Verify signatures work on user machines
10. ✅ **Backup certificates** - Keep secure backups with private keys

## Security Considerations

- 🔒 **Never commit certificates** to source control
- 🔒 **Use Azure Key Vault** or similar for CI/CD
- 🔒 **Restrict certificate access** to authorized personnel only
- 🔒 **Monitor certificate usage** - Watch for unauthorized signing
- 🔒 **Rotate certificates** before expiration
- 🔒 **Use EV certificates** for highest trust level (eliminates SmartScreen warnings immediately)

## Certificate Management

### Track Expiration

```powershell
# Check certificate expiration
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.Thumbprint -eq "YOUR_THUMBPRINT" }

Write-Host "Certificate expires: $($cert.NotAfter)"
$daysRemaining = ($cert.NotAfter - (Get-Date)).Days
Write-Host "Days remaining: $daysRemaining"

if ($daysRemaining -lt 30) {
    Write-Host "⚠️ WARNING: Certificate expires soon!" -ForegroundColor Yellow
}
```

### Export Certificate (for backup)

```powershell
# Export with private key (keep secure!)
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.Thumbprint -eq "YOUR_THUMBPRINT" }

$password = ConvertTo-SecureString -String "StrongPassword123!" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "backup.pfx" -Password $password
```

### Import Certificate

```powershell
# Import PFX
$password = ConvertTo-SecureString -String "StrongPassword123!" -Force -AsPlainText
Import-PfxCertificate -FilePath "backup.pfx" -CertStoreLocation Cert:\CurrentUser\My -Password $password
```

## Support

For issues with code signing:
1. Check prerequisites are installed
2. Verify certificate is valid and accessible
3. Check signtool.exe path
4. Review error messages carefully
5. Test with self-signed cert first
6. Contact certificate provider for cert-specific issues

## References

- [Microsoft Code Signing Best Practices](https://docs.microsoft.com/en-us/windows/win32/seccrypto/cryptography-tools)
- [SignTool Documentation](https://docs.microsoft.com/en-us/windows/win32/seccrypto/signtool)
- [Inno Setup Documentation](https://jrsoftware.org/ishelp/)
- [Authenticode Overview](https://docs.microsoft.com/en-us/windows-hardware/drivers/install/authenticode)

