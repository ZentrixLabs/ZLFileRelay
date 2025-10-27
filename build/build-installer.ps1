# ZL File Relay - Complete Installer Build Script
# Publishes all components and creates Inno Setup installer

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$InnoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

$ErrorActionPreference = "Stop"
$startTime = Get-Date

Write-Host "`n╔════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                    ║" -ForegroundColor Cyan
Write-Host "║           🚀 ZL FILE RELAY - COMPLETE INSTALLER BUILD             ║" -ForegroundColor Cyan
Write-Host "║                                                                    ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "`n📋 Build Configuration:" -ForegroundColor Yellow
Write-Host "  • Configuration: $Configuration" -ForegroundColor White
Write-Host "  • Runtime: $Runtime" -ForegroundColor White
Write-Host "  • Target: DMZ/OT/Air-Gapped Environments" -ForegroundColor White
Write-Host "  • .NET Runtime: Included (Self-Contained)" -ForegroundColor White

# Check prerequisites
Write-Host "`n🔍 Checking prerequisites..." -ForegroundColor Cyan

if (-not (Test-Path $InnoSetupPath)) {
    Write-Host "❌ Inno Setup not found at: $InnoSetupPath" -ForegroundColor Red
    Write-Host "   Download from: https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
    exit 1
}
Write-Host "  ✅ Inno Setup found" -ForegroundColor Green

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "❌ .NET SDK not found!" -ForegroundColor Red
    exit 1
}
Write-Host "  ✅ .NET SDK found" -ForegroundColor Green

$dotnetVersion = dotnet --version
Write-Host "  ℹ️  .NET Version: $dotnetVersion" -ForegroundColor Gray

# Step 1: Publish all components
Write-Host "`n" + ("═" * 70) -ForegroundColor DarkCyan
Write-Host "  STEP 1/2: Publishing Components" -ForegroundColor Cyan
Write-Host ("═" * 70) -ForegroundColor DarkCyan

& "$PSScriptRoot\publish-selfcontained.ps1" -Configuration $Configuration -Runtime $Runtime

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Publish failed!" -ForegroundColor Red
    exit 1
}

# Step 2: Build Inno Setup installer
Write-Host "`n" + ("═" * 70) -ForegroundColor DarkCyan
Write-Host "  STEP 2/2: Building Inno Setup Installer" -ForegroundColor Cyan
Write-Host ("═" * 70) -ForegroundColor DarkCyan

Write-Host "`n🔨 Compiling installer with Inno Setup..." -ForegroundColor Cyan

$innoScript = "../installer/ZLFileRelay.iss"
if (-not (Test-Path $innoScript)) {
    Write-Host "❌ Inno Setup script not found: $innoScript" -ForegroundColor Red
    exit 1
}

# Run Inno Setup compiler
Write-Host "  Running: ISCC.exe $innoScript" -ForegroundColor Gray
& $InnoSetupPath $innoScript

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Inno Setup compilation failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Installer compiled successfully!" -ForegroundColor Green

# Find the output installer
$outputDir = "../installer/output"
if (Test-Path $outputDir) {
    $installer = Get-ChildItem $outputDir -Filter "*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if ($installer) {
        $installerSize = $installer.Length / 1MB
        $endTime = Get-Date
        $duration = $endTime - $startTime
        
        Write-Host "`n╔════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║                                                                    ║" -ForegroundColor Green
        Write-Host "║                   ✅ BUILD COMPLETE - SUCCESS!                     ║" -ForegroundColor Green
        Write-Host "║                                                                    ║" -ForegroundColor Green
        Write-Host "╚════════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
        
        Write-Host "`n📦 Installer Information:" -ForegroundColor Cyan
        Write-Host "  File: $($installer.Name)" -ForegroundColor White
        Write-Host "  Size: $("{0:N2}" -f $installerSize) MB" -ForegroundColor White
        Write-Host "  Path: $($installer.FullName)" -ForegroundColor White
        Write-Host "  Date: $($installer.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor White
        
        Write-Host "`n⏱️  Build Duration: $($duration.ToString('mm\:ss'))" -ForegroundColor Cyan
        
        Write-Host "`n✨ Features Included:" -ForegroundColor Yellow
        Write-Host "  ✅ File Transfer Service (Windows Service)" -ForegroundColor Green
        Write-Host "  ✅ Web Upload Portal (Kestrel - No IIS needed!)" -ForegroundColor Green
        Write-Host "  ✅ Configuration Tool (WPF Desktop App)" -ForegroundColor Green
        Write-Host "  ✅ .NET 8 Runtime (Self-Contained)" -ForegroundColor Green
        Write-Host "  ✅ Complete Documentation" -ForegroundColor Green
        Write-Host "  ✅ Proper Application Icons" -ForegroundColor Green
        
        Write-Host "`n🎯 Deployment Scenarios:" -ForegroundColor Yellow
        Write-Host "  • DMZ Environments" -ForegroundColor White
        Write-Host "  • OT/SCADA Networks" -ForegroundColor White
        Write-Host "  • Air-Gapped Systems" -ForegroundColor White
        Write-Host "  • Windows Server 2019+" -ForegroundColor White
        Write-Host "  • No Internet Required" -ForegroundColor White
        
        Write-Host "`n🚀 Next Steps:" -ForegroundColor Yellow
        Write-Host "  1. Test installer on clean VM" -ForegroundColor White
        Write-Host "  2. Verify all components install correctly" -ForegroundColor White
        Write-Host "  3. Test file transfer workflow" -ForegroundColor White
        Write-Host "  4. Deploy to production environment" -ForegroundColor White
        
        Write-Host "`n📂 Installer Location:" -ForegroundColor Cyan
        Write-Host "  $($installer.FullName)" -ForegroundColor Green
        
        Write-Host "`n" + ("═" * 70) -ForegroundColor DarkGray
        Write-Host ""
        
        # Open the output folder
        Write-Host "💡 Tip: Opening installer directory..." -ForegroundColor Gray
        Start-Process explorer.exe -ArgumentList "/select,`"$($installer.FullName)`""
        
    } else {
        Write-Host "⚠️  Warning: Installer file not found in output directory" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️  Warning: Output directory not found" -ForegroundColor Yellow
}

