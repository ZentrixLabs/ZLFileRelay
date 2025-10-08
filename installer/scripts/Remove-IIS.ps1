# Remove IIS Configuration for ZL File Relay Web Portal

param(
    [string]$SiteName = "ZLFileRelay"
)

$ErrorActionPreference = "Continue"

Write-Host "Removing IIS configuration for ZL File Relay..." -ForegroundColor Cyan

try {
    Import-Module WebAdministration -ErrorAction SilentlyContinue
    
    # Remove website
    if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
        Write-Host "Removing website: $SiteName" -ForegroundColor Yellow
        Remove-Website -Name $SiteName -ErrorAction SilentlyContinue
    }
    
    # Remove application pool
    $appPoolName = "${SiteName}AppPool"
    if (Test-Path "IIS:\AppPools\$appPoolName" -ErrorAction SilentlyContinue) {
        Write-Host "Removing application pool: $appPoolName" -ForegroundColor Yellow
        Remove-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue
    }
    
    Write-Host "✅ IIS configuration removed" -ForegroundColor Green
    
} catch {
    Write-Host "⚠️  Could not remove IIS configuration (this is OK if IIS not installed)" -ForegroundColor Yellow
}

