# Configure IIS for ZL File Relay Web Portal
# Self-Contained ASP.NET Core 8 Application

param(
    [Parameter(Mandatory=$true)]
    [string]$WebAppPath,
    
    [string]$SiteName = "ZLFileRelay",
    [int]$Port = 8080
)

$ErrorActionPreference = "Stop"

Write-Host "Configuring IIS for ZL File Relay Web Portal..." -ForegroundColor Cyan

try {
    # Import WebAdministration module
    Import-Module WebAdministration -ErrorAction Stop
    
    # Check if site already exists
    if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
        Write-Host "Removing existing site: $SiteName" -ForegroundColor Yellow
        Remove-Website -Name $SiteName
    }
    
    # Create application pool
    $appPoolName = "${SiteName}AppPool"
    
    if (Test-Path "IIS:\AppPools\$appPoolName") {
        Write-Host "Removing existing app pool: $appPoolName" -ForegroundColor Yellow
        Remove-WebAppPool -Name $appPoolName
    }
    
    Write-Host "Creating application pool: $appPoolName" -ForegroundColor Cyan
    New-WebAppPool -Name $appPoolName
    
    # Configure app pool for ASP.NET Core
    # Self-contained = No .NET CLR needed!
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "startMode" -Value "AlwaysRunning"
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "processModel.idleTimeout" -Value "00:00:00"
    
    # Create website
    Write-Host "Creating website: $SiteName" -ForegroundColor Cyan
    New-Website -Name $SiteName `
        -PhysicalPath $WebAppPath `
        -ApplicationPool $appPoolName `
        -Port $Port `
        -Force
    
    # Configure Windows Authentication (if needed)
    $authConfig = "$SiteName/authentication"
    Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" `
        -Name "enabled" -Value $true -PSPath "IIS:\" -Location $SiteName
    
    Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" `
        -Name "enabled" -Value $true -PSPath "IIS:\" -Location $SiteName
    
    Write-Host "✅ IIS configured successfully!" -ForegroundColor Green
    Write-Host "   Site Name: $SiteName" -ForegroundColor White
    Write-Host "   Port: $Port" -ForegroundColor White
    Write-Host "   URL: http://localhost:$Port" -ForegroundColor White
    Write-Host "   App Pool: $appPoolName (No Managed Code - Self-Contained)" -ForegroundColor White
    
} catch {
    Write-Host "❌ Error configuring IIS: $_" -ForegroundColor Red
    Write-Host "You can configure IIS manually later using the Configuration Tool" -ForegroundColor Yellow
    exit 1
}

