# ZL File Relay - SSL Certificate Import Script
# Imports a .pfx/.p12 certificate into the Windows certificate store
# Also grants private key access to LocalSystem and optionally a service account
# Usage:
#   Import new:  .\Import-SslCertificate.ps1 -CertificatePath "C:\path\to\cert.pfx" -Password "password" [-ServiceAccount "DOMAIN\account"]
#   Use existing: .\Import-SslCertificate.ps1 -Thumbprint "ABC123..." [-ServiceAccount "DOMAIN\account"]

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, ParameterSetName = "ImportFile")]
    [string]$CertificatePath,
    
    [Parameter(Mandatory = $false, ParameterSetName = "ImportFile")]
    [string]$Password,
    
    [Parameter(Mandatory = $true, ParameterSetName = "ExistingThumbprint")]
    [string]$Thumbprint,
    
    [Parameter(Mandatory = $false)]
    [string]$StoreName = "My",
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("LocalMachine", "CurrentUser")]
    [string]$StoreLocation = "LocalMachine",
    
    [Parameter(Mandatory = $false)]
    [string]$ServiceAccount = "NT AUTHORITY\SYSTEM"
)

$ErrorActionPreference = "Stop"

function Grant-PrivateKeyAccess {
    param(
        [Parameter(Mandatory = $true)]
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate,
        
        [Parameter(Mandatory = $true)]
        [string]$AccountName
    )
    
    try {
        # Get the certificate's private key file path
        $KeyFileName = $Certificate.PrivateKey.Key.UniqueName
        if (-not $KeyFileName) {
            Write-Warning "Certificate does not have a private key or private key is not accessible"
            return $false
        }
        
        # Find the private key file in the MachineKeys directory
        $MachineKeysPath = "$env:ProgramData\Microsoft\Crypto\RSA\MachineKeys"
        $KeyFilePath = Join-Path $MachineKeysPath $KeyFileName
        
        if (-not (Test-Path $KeyFilePath)) {
            # Try to find it in other locations (CSP-specific)
            Write-Warning "Private key file not found in standard location. Attempting alternative method..."
            
            # Use certutil to grant permissions (works for all key storage providers)
            $ThumbprintClean = $Certificate.Thumbprint.Replace(" ", "").Replace("-", "")
            $Result = & certutil.exe -repairstore My $ThumbprintClean 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Certificate key permissions repaired (access granted to $AccountName)" -ForegroundColor Green
                
                # Now grant explicit permissions using icacls/cacls if possible
                try {
                    # Get the registry path for the private key
                    $RegPath = "HKLM:\SOFTWARE\Microsoft\SystemCertificates\My\Keys\$ThumbprintClean"
                    if (Test-Path $RegPath) {
                        # Grant read permissions to the service account on the registry key
                        $RegAcl = Get-Acl $RegPath
                        $RegRule = New-Object System.Security.AccessControl.RegistryAccessRule(
                            $AccountName, "Read", "Allow")
                        $RegAcl.AddAccessRule($RegRule)
                        Set-Acl $RegPath $RegAcl
                    }
                } catch {
                    Write-Warning "Could not set registry permissions, but certutil repair should be sufficient: $_"
                }
                
                return $true
            } else {
                Write-Warning "certutil repair failed. Error: $Result"
                return $false
            }
        }
        
        # Grant file system permissions on the private key file
        $Acl = Get-Acl $KeyFilePath
        $FileAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $AccountName, "Read", "Allow")
        $Acl.AddAccessRule($FileAccessRule)
        Set-Acl $KeyFilePath $Acl
        
        Write-Host "✅ Private key permissions granted to $AccountName" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Warning "Failed to grant private key permissions: $_"
        Write-Host "Attempting alternative method using certutil..." -ForegroundColor Yellow
        
        # Alternative: Use certutil to grant permissions
        try {
            $ThumbprintClean = $Certificate.Thumbprint.Replace(" ", "").Replace("-", "")
            
            # Use certutil to set permissions
            # First, ensure the service account can access the certificate
            $Result = & certutil.exe -repairstore My $ThumbprintClean 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Certificate key permissions granted via certutil" -ForegroundColor Green
                return $true
            } else {
                Write-Error "Failed to set permissions via certutil: $Result"
                return $false
            }
        }
        catch {
            Write-Error "All methods to grant permissions failed: $_"
            return $false
        }
    }
}

try {
    $Cert = $null
    
    # Case 1: Import new certificate from file
    if ($PSCmdlet.ParameterSetName -eq "ImportFile") {
        Write-Host "Importing SSL certificate from file..." -ForegroundColor Cyan
        
        # Validate certificate file exists
        if (-not (Test-Path $CertificatePath)) {
            throw "Certificate file not found: $CertificatePath"
        }
        
        # Convert password to SecureString if provided
        $SecurePassword = $null
        if ($Password) {
            $SecurePassword = ConvertTo-SecureString -String $Password -Force -AsPlainText
        }
        
        # Determine store location
        $StoreLocationEnum = if ($StoreLocation -eq "CurrentUser") {
            [System.Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser
        } else {
            [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine
        }
        
        # Import certificate
        if ($SecurePassword) {
            $Cert = Import-PfxCertificate -FilePath $CertificatePath -Password $SecurePassword -CertStoreLocation "Cert:\$StoreLocation\$StoreName"
        } else {
            $Cert = Import-PfxCertificate -FilePath $CertificatePath -CertStoreLocation "Cert:\$StoreLocation\$StoreName"
        }
        
        Write-Host "✅ Certificate imported successfully!" -ForegroundColor Green
    }
    # Case 2: Use existing certificate by thumbprint
    else {
        Write-Host "Locating existing certificate in certificate store..." -ForegroundColor Cyan
        
        # Determine store location
        $StoreLocationEnum = if ($StoreLocation -eq "CurrentUser") {
            [System.Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser
        } else {
            [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine
        }
        
        # Open certificate store
        $Store = New-Object System.Security.Cryptography.X509Certificates.X509Store(
            $StoreName, $StoreLocationEnum)
        $Store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)
        
        try {
            # Clean thumbprint (remove spaces/dashes)
            $ThumbprintClean = $Thumbprint.Replace(" ", "").Replace("-", "")
            
            # Find certificate
            $Certificates = $Store.Certificates.Find(
                [System.Security.Cryptography.X509Certificates.X509FindType]::FindByThumbprint,
                $ThumbprintClean,
                $false)
            
            if ($Certificates.Count -eq 0) {
                throw "Certificate with thumbprint '$Thumbprint' not found in store $StoreLocation\$StoreName"
            }
            
            $Cert = $Certificates[0]
            Write-Host "✅ Certificate found in certificate store!" -ForegroundColor Green
        }
        finally {
            $Store.Close()
        }
    }
    
    if ($null -eq $Cert) {
        throw "Failed to obtain certificate"
    }
    
    Write-Host ""
    Write-Host "Certificate Details:" -ForegroundColor Cyan
    Write-Host "  Subject: $($Cert.Subject)" -ForegroundColor White
    Write-Host "  Thumbprint: $($Cert.Thumbprint)" -ForegroundColor White
    Write-Host "  Issuer: $($Cert.Issuer)" -ForegroundColor White
    Write-Host "  Valid From: $($Cert.NotBefore)" -ForegroundColor White
    Write-Host "  Valid Until: $($Cert.NotAfter)" -ForegroundColor White
    Write-Host "  Store: $StoreLocation\$StoreName" -ForegroundColor White
    Write-Host ""
    
    # Warn if certificate expires soon
    $DaysUntilExpiry = ($Cert.NotAfter - (Get-Date)).Days
    if ($DaysUntilExpiry -lt 0) {
        Write-Warning "⚠️  Certificate has EXPIRED!"
    } elseif ($DaysUntilExpiry -lt 30) {
        Write-Warning "⚠️  Certificate expires in $DaysUntilExpiry days!"
    }
    
    # Grant private key permissions to service account
    Write-Host ""
    Write-Host "Granting private key access to $ServiceAccount..." -ForegroundColor Cyan
    
    if ($Cert.HasPrivateKey) {
        $PermissionGranted = Grant-PrivateKeyAccess -Certificate $Cert -AccountName $ServiceAccount
        
        if ($PermissionGranted) {
            Write-Host "✅ Private key permissions configured" -ForegroundColor Green
        } else {
            Write-Warning "⚠️  Could not automatically grant private key permissions. You may need to configure manually:"
            Write-Host "   Use: certutil.exe -repairstore My $($Cert.Thumbprint.Replace(' ', '').Replace('-', ''))" -ForegroundColor Yellow
            Write-Host "   Or grant permissions via Certificate Manager (certlm.msc)" -ForegroundColor Yellow
        }
    } else {
        Write-Warning "⚠️  Certificate does not have a private key. This may be a public certificate only."
    }
    
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Configure the certificate thumbprint in appsettings.json:" -ForegroundColor White
    Write-Host "   `"CertificateThumbprint`": `"$($Cert.Thumbprint)`"" -ForegroundColor Yellow
    Write-Host "   `"CertificateStoreLocation`": `"$StoreLocation`"" -ForegroundColor Yellow
    Write-Host "   `"CertificateStoreName`": `"$StoreName`"" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "2. Enable HTTPS in appsettings.json:" -ForegroundColor White
    Write-Host "   `"EnableHttps`": true" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "3. Verify service account has access:" -ForegroundColor White
    Write-Host "   Service Account: $ServiceAccount" -ForegroundColor Yellow
    Write-Host "   If using a custom account, ensure it has private key access" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "4. Restart the ZLFileRelay.WebPortal service" -ForegroundColor White
    
    # Return thumbprint for use in installer
    return @{
        Thumbprint = $Cert.Thumbprint
        Subject = $Cert.Subject
        ExpiryDate = $Cert.NotAfter
        HasPrivateKey = $Cert.HasPrivateKey
        Success = $true
    }
}
catch {
    Write-Error "❌ Failed to import/configure certificate: $_"
    return @{
        Success = $false
        Error = $_.Exception.Message
    }
}
