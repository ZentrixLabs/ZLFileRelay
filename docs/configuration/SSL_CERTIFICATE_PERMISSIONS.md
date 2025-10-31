# SSL Certificate Permissions Guide

## Overview

When using SSL certificates with the ZL File Relay Web Portal, the Windows service account needs access to the certificate's private key. This guide explains how to ensure proper permissions are configured.

## Service Account Considerations

### LocalSystem Account (Default)

**Good News:** The web portal service runs as `NT AUTHORITY\SYSTEM` by default, which typically has access to certificates in the `LocalMachine\My` store.

**Limitations:**
- LocalSystem has limited network access (can't use domain credentials)
- May not work with certificates imported into `CurrentUser` store
- May have issues with certificates that have custom ACLs

**Best Practice:** For LocalSystem, import certificates into `LocalMachine\My` store. The import script will automatically configure permissions.

### Custom Service Account (Recommended for Production)

For better security and network access, use a dedicated service account:

**Benefits:**
- Can use domain credentials for network access
- Follows principle of least privilege
- Better auditability

**Requirements:**
1. Service account must have "Log on as a service" right
2. Service account must have private key access to the certificate
3. Service account must have file system permissions for logs/uploads

## Certificate Import Methods

### Method 1: Import New Certificate (Recommended)

The import script automatically configures permissions:

```powershell
# Import certificate and grant access to LocalSystem
.\Import-SslCertificate.ps1 -CertificatePath "C:\path\to\cert.pfx" -Password "password"

# Import certificate and grant access to custom service account
.\Import-SslCertificate.ps1 -CertificatePath "C:\path\to\cert.pfx" -Password "password" -ServiceAccount "DOMAIN\svc_zlfilerelay"
```

### Method 2: Use Existing Certificate by Thumbprint

If the certificate is already in the store:

```powershell
# Grant access to LocalSystem (default)
.\Import-SslCertificate.ps1 -Thumbprint "ABC123DEF456..." -ServiceAccount "NT AUTHORITY\SYSTEM"

# Grant access to custom service account
.\Import-SslCertificate.ps1 -Thumbprint "ABC123DEF456..." -ServiceAccount "DOMAIN\svc_zlfilerelay"
```

## Manual Permission Configuration

If automatic permission grant fails, use these manual methods:

### Option 1: Using Certificate Manager (GUI)

1. Open Certificate Manager as Administrator:
   - Press `Win + R`, type `certlm.msc`, press Enter

2. Navigate to the certificate:
   - Expand `Personal` → `Certificates`
   - Find your certificate (by thumbprint or subject)

3. Grant private key access:
   - Right-click the certificate → `All Tasks` → `Manage Private Keys...`
   - Click `Add...`
   - Enter the service account name (e.g., `NT AUTHORITY\SYSTEM` or `DOMAIN\svc_account`)
   - Select `Read` permission
   - Click `OK`

### Option 2: Using certutil (Command Line)

```powershell
# Replace THUMBPRINT with your certificate thumbprint (no spaces/dashes)
certutil.exe -repairstore My THUMBPRINT
```

**Note:** `certutil -repairstore` grants access to the **current user** running the command. If you run it as Administrator, it grants access to LocalSystem. For custom service accounts, you may need to use Method 1 (Certificate Manager) or set file system permissions directly.

### Option 3: Using PowerShell (Advanced)

For custom service accounts, locate and grant permissions on the private key file:

```powershell
# Find the certificate's private key file (in MachineKeys directory)
# Grant permissions to service account
$ServiceAccount = "DOMAIN\svc_zlfilerelay"
$KeyPath = "C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\<key-file>"

$Acl = Get-Acl $KeyPath
$AccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $ServiceAccount, "Read", "Allow")
$Acl.AddAccessRule($AccessRule)
Set-Acl $KeyPath $Acl
```

**Warning:** Finding the exact key file can be challenging. Use Certificate Manager (Method 1) for reliability.

## Verification

### Check Service Account

```powershell
# Check what account the service is running as
Get-WmiObject Win32_Service -Filter "Name='ZLFileRelay.WebPortal'" | Select-Object Name, StartName
```

### Test Certificate Access

```powershell
# Import the certificate into PowerShell as the service account context
# This verifies the account can access the private key
$Thumbprint = "YOUR_THUMBPRINT" # No spaces/dashes
$Store = New-Object System.Security.Cryptography.X509Certificates.X509Store(
    [System.Security.Cryptography.X509Certificates.StoreName]::My,
    [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine)
$Store.Open("ReadOnly")
$Cert = $Store.Certificates.Find(
    [System.Security.Cryptography.X509Certificates.X509FindType]::FindByThumbprint,
    $Thumbprint,
    $false)

if ($Cert.Count -gt 0 -and $Cert[0].HasPrivateKey) {
    Write-Host "✅ Certificate accessible with private key"
    # Try to use the private key (this will fail if permissions are wrong)
    try {
        $PrivateKey = $Cert[0].PrivateKey
        Write-Host "✅ Private key accessible!"
    } catch {
        Write-Error "❌ Private key NOT accessible: $_"
    }
} else {
    Write-Error "Certificate not found or has no private key"
}

$Store.Close()
```

### Test Service Startup

1. Configure certificate in `appsettings.json`:
```json
{
  "ZLFileRelay": {
    "WebPortal": {
      "Kestrel": {
        "EnableHttps": true,
        "CertificateThumbprint": "ABC123DEF456...",
        "CertificateStoreLocation": "LocalMachine",
        "CertificateStoreName": "My"
      }
    }
  }
}
```

2. Start the service:
```powershell
Start-Service ZLFileRelay.WebPortal
```

3. Check logs:
```powershell
# Look for certificate loading messages
Get-Content "C:\FileRelay\logs\zlfilerelay-web-*.log" -Tail 50 | Select-String "certificate"
```

If you see errors like "Access denied" or "The private key is not accessible", the permissions are incorrect.

## Troubleshooting

### Service Can't Access Private Key

**Symptoms:**
- Service starts but HTTPS fails
- Log shows: "The private key is not accessible" or "Access denied"

**Solutions:**
1. Verify service account has access:
   - Open Certificate Manager (certlm.msc)
   - Check certificate → Manage Private Keys
   - Ensure service account has Read permission

2. Re-run the import script:
   ```powershell
   .\Import-SslCertificate.ps1 -Thumbprint "YOUR_THUMBPRINT" -ServiceAccount "YOUR_SERVICE_ACCOUNT"
   ```

3. Use certutil repair:
   ```powershell
   # Run as Administrator
   certutil.exe -repairstore My YOUR_THUMBPRINT
   ```

### Certificate Not Found

**Symptoms:**
- Service fails to start
- Log shows: "Certificate with thumbprint 'XXX' not found"

**Solutions:**
1. Verify thumbprint (remove spaces/dashes):
   ```powershell
   Get-ChildItem Cert:\LocalMachine\My | Where-Object {$_.Thumbprint -eq "YOUR_THUMBPRINT"}
   ```

2. Check store location in appsettings.json matches where certificate actually is

3. Import certificate to correct store:
   ```powershell
   .\Import-SslCertificate.ps1 -CertificatePath "cert.pfx" -Password "pass" -StoreLocation "LocalMachine" -StoreName "My"
   ```

### Certificate Expired

**Symptoms:**
- Service starts but browsers show certificate errors
- Log shows certificate expiry warnings

**Solutions:**
1. Import new certificate
2. Update thumbprint in appsettings.json
3. Restart service

## Best Practices

1. **Use LocalMachine\My store** - Works best with LocalSystem
2. **Test after importing** - Verify service can start and HTTPS works
3. **Document thumbprint** - Keep track of certificate thumbprints for troubleshooting
4. **Monitor expiry** - Set calendar reminders for certificate renewal
5. **Use service account** - For production, use dedicated account (not LocalSystem)
6. **Grant minimal permissions** - Only grant Read access to private key

## Related Documentation

- [SSL Certificate Setup Guide](../getting-started/INSTALLATION.md#ssl-certificate-configuration)
- [Service Account Configuration](SERVICE_ACCOUNT.md)
- [Web Portal Configuration](CONFIGURATION.md#web-portal)

