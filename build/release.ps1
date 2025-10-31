#requires -version 5.1
# ZL File Relay - Release Orchestration Script
# Coordinates build, signing, and installer creation

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [switch]$Sign,
    [switch]$SkipBuild,
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = 'Stop'

# Ensure we operate from repository root
$root = Resolve-Path "$PSScriptRoot\.."
Set-Location $root

$startTime = Get-Date

Write-Host "`n╔════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                    ║" -ForegroundColor Cyan
Write-Host "║           🚀 ZL FILE RELAY - RELEASE WORKFLOW                     ║" -ForegroundColor Cyan
Write-Host "║                                                                    ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "`n📋 Release Configuration:" -ForegroundColor Yellow
Write-Host "  • Version: $Version" -ForegroundColor White
Write-Host "  • Configuration: $Configuration" -ForegroundColor White
Write-Host "  • Runtime: $Runtime" -ForegroundColor White
Write-Host "  • Code Signing: $(if ($Sign) { 'Enabled' } else { 'Disabled' })" -ForegroundColor White
Write-Host "  • Skip Build: $(if ($SkipBuild) { 'Yes' } else { 'No' })" -ForegroundColor White

# Step 1: Update version in project files
Write-Host "`n" + ("═" * 70) -ForegroundColor DarkCyan
Write-Host "  STEP 1: Update Version" -ForegroundColor Cyan
Write-Host ("═" * 70) -ForegroundColor DarkCyan

Write-Host "`n⚠️  Manual step required:" -ForegroundColor Yellow
Write-Host "  Please update version in these files to: $Version" -ForegroundColor White
Write-Host "  • src/ZLFileRelay.Service/ZLFileRelay.Service.csproj" -ForegroundColor Gray
Write-Host "  • src/ZLFileRelay.WebPortal/ZLFileRelay.WebPortal.csproj" -ForegroundColor Gray
Write-Host "  • src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj" -ForegroundColor Gray
Write-Host "  • src/ZLFileRelay.Core/ZLFileRelay.Core.csproj" -ForegroundColor Gray
Write-Host "  • installer/ZLFileRelay.iss (AppVersion)" -ForegroundColor Gray
Write-Host "  • CHANGELOG.md" -ForegroundColor Gray

Read-Host "`nPress Enter after updating version numbers"

# Step 2: Build all components
if (-not $SkipBuild) {
    Write-Host "`n" + ("═" * 70) -ForegroundColor DarkCyan
    Write-Host "  STEP 2: Build All Components" -ForegroundColor Cyan
    Write-Host ("═" * 70) -ForegroundColor DarkCyan
    
    Write-Host "`n🔨 Building all components..." -ForegroundColor Cyan
    & "$PSScriptRoot\build-app.ps1" -Configuration $Configuration -Runtime $Runtime
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
} else {
    Write-Host "`n⏭️  Skipping build step (using existing binaries)" -ForegroundColor Yellow
}

# Step 3: Code signing (if enabled)
if ($Sign) {
    Write-Host "`n" + ("═" * 70) -ForegroundColor DarkCyan
    Write-Host "  STEP 3: Code Signing" -ForegroundColor Cyan
    Write-Host ("═" * 70) -ForegroundColor DarkCyan
    
    Write-Host "`n✍️  Signing executables..." -ForegroundColor Cyan
    & "$PSScriptRoot\sign-app.ps1" -Configuration $Configuration
    
    if ($LASTEXITCODE -ne 0) {
        throw "Code signing failed"
    }
} else {
    Write-Host "`n⏭️  Skipping code signing (use -Sign to enable)" -ForegroundColor Yellow
}

# Step 4: Publish self-contained
Write-Host "`n" + ("═" * 70) -ForegroundColor DarkCyan
Write-Host "  STEP 4: Publish Self-Contained" -ForegroundColor Cyan
Write-Host ("═" * 70) -ForegroundColor DarkCyan

Write-Host "`n📦 Publishing self-contained builds..." -ForegroundColor Cyan
& "$PSScriptRoot\publish-selfcontained.ps1" -Configuration $Configuration -Runtime $Runtime

if ($LASTEXITCODE -ne 0) {
    throw "Publish failed"
}

# Step 5: Sign published executables (if signing enabled)
if ($Sign) {
    Write-Host "`n" + ("═" * 70) -ForegroundColor DarkCyan
    Write-Host "  STEP 5: Sign Published Executables" -ForegroundColor Cyan
    Write-Host ("═" * 70) -ForegroundColor DarkCyan
    
    Write-Host "`n✍️  Signing published executables..." -ForegroundColor Yellow
    Write-Host "⚠️  You may need to manually sign files in the publish directory" -ForegroundColor Yellow
    Write-Host "   Located at: publish/" -ForegroundColor DarkGray
    
    Read-Host "`nPress Enter after signing published files (or skip if not needed)"
}

# Step 6: Create Inno Setup installer
Write-Host "`n" + ("═" * 70) -ForegroundColor DarkCyan
Write-Host "  STEP 6: Create Installer" -ForegroundColor Cyan
Write-Host ("═" * 70) -ForegroundColor DarkCyan

Write-Host "`n🔧 Please compile the installer in Inno Setup:" -ForegroundColor Yellow
Write-Host "  1. Open: installer/ZLFileRelay.iss" -ForegroundColor White
Write-Host "  2. Compile the installer" -ForegroundColor White
if ($Sign) {
    Write-Host "  3. Sign the installer using Inno Setup's SignTool integration" -ForegroundColor White
}
Write-Host "  Output: installer/output/ZLFileRelaySetup-$Version.exe" -ForegroundColor DarkGray

Read-Host "`nPress Enter after the installer has been created"

# Step 7: Verify installer
$installerPath = "installer/output/ZLFileRelaySetup-$Version.exe"
if (-not (Test-Path $installerPath)) {
    # Try to find any installer in output
    $installerDir = "installer/output"
    if (Test-Path $installerDir) {
        $installer = Get-ChildItem $installerDir -Filter "*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($installer) {
            $installerPath = $installer.FullName
            Write-Host "⚠️  Found installer at different path: $installerPath" -ForegroundColor Yellow
        }
    }
}

if (Test-Path $installerPath) {
    $installer = Get-Item $installerPath
    $installerSize = $installer.Length / 1MB
    
    Write-Host "`n╔════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║                                                                    ║" -ForegroundColor Green
    Write-Host "║                   ✅ RELEASE COMPLETE - SUCCESS!                   ║" -ForegroundColor Green
    Write-Host "║                                                                    ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-Host "`n📦 Release Package:" -ForegroundColor Cyan
    Write-Host "  Version: $Version" -ForegroundColor White
    Write-Host "  File: $($installer.Name)" -ForegroundColor White
    Write-Host "  Size: $("{0:N2}" -f $installerSize) MB" -ForegroundColor White
    Write-Host "  Path: $($installer.FullName)" -ForegroundColor White
    Write-Host "  Date: $($installer.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor White
    Write-Host "  Code Signed: $(if ($Sign) { 'Yes' } else { 'No' })" -ForegroundColor White
    
    Write-Host "`n⏱️  Total Duration: $($duration.ToString('hh\:mm\:ss'))" -ForegroundColor Cyan
    
    # Create checksum
    Write-Host "`n🔒 Creating checksum..." -ForegroundColor Cyan
    $shaFile = "$($installer.FullName).sha256"
    $sha = (Get-FileHash -Algorithm SHA256 $installer.FullName).Hash
    Set-Content -Path $shaFile -NoNewline -Value $sha
    Write-Host "  Checksum saved: $shaFile" -ForegroundColor Green
    Write-Host "  SHA256: $sha" -ForegroundColor DarkGray
    
    Write-Host "`n✅ Release artifacts ready for distribution!" -ForegroundColor Green
    
    Write-Host "`n📋 Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Test installer on clean VM" -ForegroundColor White
    Write-Host "  2. Verify all components work correctly" -ForegroundColor White
    Write-Host "  3. Update GitHub release with installer and checksum" -ForegroundColor White
    Write-Host "  4. Update documentation if needed" -ForegroundColor White
    
    Write-Host "`n📂 Installer Location:" -ForegroundColor Cyan
    Write-Host "  $($installer.FullName)" -ForegroundColor Green
    
    # Open the output folder
    Write-Host "`n💡 Opening installer directory..." -ForegroundColor Gray
    Start-Process explorer.exe -ArgumentList "/select,`"$($installer.FullName)`""
    
} else {
    Write-Host "`n⚠️  Warning: Installer not found at expected path" -ForegroundColor Yellow
    Write-Host "  Expected: $installerPath" -ForegroundColor DarkGray
    Write-Host "  Please verify the installer was created successfully" -ForegroundColor Yellow
}

Write-Host "`n" + ("═" * 70) -ForegroundColor DarkGray
Write-Host ""

