# ZL File Relay - HttpSys URL Reservation Setup Script
# Configures URL reservations and SSL certificate binding for HttpSys (HTTP.sys Linus server)
# Required for cross-domain Windows Authentication support
# 
# Usage:
#   .\Setup-HttpSysUrlReservations.ps1 -HttpPort 8080 -HttpsPort 8443 -CertificateThumbprint "ABC123..." [-User "NT AUTHORITY\SYSTEM"]

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [int]$HttpPort,
    
    [Parameter(Mandatory = $true)]
    [int]$HttpsPort,
    
    [Parameter(Mandatory = $false)]
    [string]$CertificateThumbprint,
    
    [Parameter(Mandatory = $false)]
    [string]$User = "NT AUTHORITY\SYSTEM",
    
    [Parameter(Mandatory = $false)]
    [string]$AppId = "{8BCA9B5F-8E0B-4A3C-9F1D-2E3C4D5E6F7A}"
)

$ErrorActionPreference = "Stop"

# Ensure running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin)
{
    Write-Error "This script must be run as Administrator (required for netsh http commands)"
    exit 1
}

Write-Host "🔧 Setting up HttpSys URL reservations for ZL File Relay" -ForegroundColor Cyan
Write-Host ""

# Remove existing URL reservations (if they exist) to avoid conflicts
Write-Host "Cleaning up existing URL reservations..." -ForegroundColor Yellow
$httpUrl = "http://*:$HttpPort/"
$httpsUrl = "https://*:$HttpsPort/"

try {
    netsh http delete urlacl url=$httpUrl 2>&1 | Out-Null
    Write-Host "  ✅ Removed existing HTTP URL reservation" -ForegroundColor Green
} catch {
    Write-Host "  ℹ️  No existing HTTP URL reservation to remove" -ForegroundColor Gray
}

try {
    netsh http delete urlacl url=$httpsUrl 2>&1 | Out-Null
    Write-Host "  ✅ Removed existing HTTPS URL reservation" -ForegroundColor Green
} catch {
    Write-Host "  ℹ️  No existing HTTPS URL reservation to remove" -ForegroundColor Gray
}

# Add URL reservations
Write-Host ""
Write-Host "Adding URL reservations..." -ForegroundColor Yellow

try {
    netsh http add urlacl url=$httpUrl user=$User
    Write-Host "  ✅ HTTP URL reservation added: $httpUrl (user: $User)" -ForegroundColor Green
} catch {
    Write-Error "Failed to add HTTP URL reservation: $_"
    exit 1
}

try {
    netsh http add urlacl url=$httpsUrl user=$User
    Write-Host "  ✅ HTTPS URL reservation added: $httpsUrl (user: $User)" -ForegroundColor Green
} catch {
    Write-Error "Failed to add HTTPS URL reservation: $_"
    exit 1
}

# Configure SSL certificate binding if thumbprint provided
if (-not [string]::IsNullOrWhiteSpace($CertificateThumbprint))
{
    Write-Host ""
    Write-Host "Configuring SSL certificate binding..." -ForegroundColor Yellow
    
    # Remove spaces and dashes from thumbprint
    $certHash = $CertificateThumbprint.Replace(" ", "").Replace("-", "")
    $ipport = "0.0.0.0:$HttpsPort"
    
    # Remove existing SSL certificate binding (if it exists)
    try {
        netsh http delete sslcert ipport=$ipport 2>&1 | Out-Null
        Write-Host "  ✅ Removed existing SSL certificate binding" -ForegroundColor Green
    } catch {
        Write-Host "  ℹ️  No existing SSL certificate binding to remove" -ForegroundColor Gray
    }
    
    # Add SSL certificate binding
    try {
        netsh http add sslcert ipport=$ipport certhash=$certHash appid=$AppId
        Write-Host "  ✅ SSL certificate binding configured:" -ForegroundColor Green
        Write-Host "     IP:Port: $ipport" -ForegroundColor Gray
        Write-Host "     Certificate Hash: $certHash" -ForegroundColor Gray
        Write-Host "     App ID: $AppId" -ForegroundColor Gray
    } catch {
        Write-Warning "Failed to add SSL certificate binding: $_"
        Write-Warning "You may need to configure this manually or ensure the certificate is installed in LocalMachine\My store"
        Write-Host ""
        Write-Host "Manual command:" -ForegroundColor Yellow
        Write-Host "  netsh http add sslcert ipport=$ipport certhash=$certHash appid=$AppId" -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "✅ HttpSys URL reservations configured successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  HTTP URL:  $httpUrl" -ForegroundColor White
Write-Host "  HTTPS URL: $httpsUrl" -ForegroundColor White
Write-Host "  User:      $User" -ForegroundColor White
if (-not [string]::IsNullOrWhiteSpace($CertificateThumbprint))
{
    Write-Host "  SSL Cert:  $CertificateThumbprint" -ForegroundColor White
}
Write-Host ""
Write-Host "Note: The web portal service must be restarted for changes to take effect." -ForegroundColor Yellow

