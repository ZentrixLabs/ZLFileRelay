# Self-Contained Publish Script for ZL File Relay
# Includes .NET 8 runtime - no external dependencies needed
# Perfect for DMZ/OT/air-gapped environments

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$PublishDir = "../publish"

Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  🚀 ZL FILE RELAY - SELF-CONTAINED PUBLISH                  ║" -ForegroundColor Green
Write-Host "║  Includes .NET 8 Runtime - No Dependencies Required         ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green

Write-Host "`nConfiguration: $Configuration" -ForegroundColor Cyan
Write-Host "Runtime: $Runtime" -ForegroundColor Cyan
Write-Host "Target: DMZ/OT/Air-Gapped Environments" -ForegroundColor Yellow

# Clean previous publish
if (Test-Path $PublishDir) {
    Write-Host "`n🧹 Cleaning previous publish directory..." -ForegroundColor Yellow
    Remove-Item $PublishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

# Publish Service
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "📦 Publishing File Transfer Service (with .NET 8)..." -ForegroundColor Cyan
dotnet publish ../src/ZLFileRelay.Service/ZLFileRelay.Service.csproj `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -o "$PublishDir/Service"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Service publish failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Service published" -ForegroundColor Green

# Publish Web Portal
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "🌐 Publishing Web Portal (with .NET 8)..." -ForegroundColor Cyan
dotnet publish ../src/ZLFileRelay.WebPortal/ZLFileRelay.WebPortal.csproj `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -o "$PublishDir/WebPortal"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Web Portal publish failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Web Portal published" -ForegroundColor Green

# Publish Config Tool (single file)
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "🔧 Publishing Configuration Tool (single file with .NET 8)..." -ForegroundColor Cyan
dotnet publish ../src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "$PublishDir/ConfigTool"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Config Tool publish failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Config Tool published" -ForegroundColor Green

# Copy configuration file
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "📄 Copying configuration templates..." -ForegroundColor Cyan
Copy-Item "../appsettings.json" "$PublishDir/appsettings.json"
Write-Host "✅ Configuration copied" -ForegroundColor Green

# Copy documentation
Write-Host "`n📚 Copying documentation..." -ForegroundColor Cyan
if (Test-Path "../docs") {
    Copy-Item "../docs" "$PublishDir/docs" -Recurse -Force
    Write-Host "✅ Documentation copied" -ForegroundColor Green
}

# Generate deployment summary
Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  ✅ PUBLISH COMPLETE - SELF-CONTAINED WITH .NET 8           ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green

# Show sizes
Write-Host "`n📊 Published Component Sizes:" -ForegroundColor Cyan
Get-ChildItem $PublishDir -Directory | ForEach-Object {
    $size = (Get-ChildItem $_.FullName -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
    $fileCount = (Get-ChildItem $_.FullName -Recurse -File | Measure-Object).Count
    Write-Host ("  {0,-20} {1,8:N2} MB ({2} files)" -f $_.Name, $size, $fileCount) -ForegroundColor White
}

$totalSize = (Get-ChildItem $PublishDir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
$totalFiles = (Get-ChildItem $PublishDir -Recurse -File | Measure-Object).Count
Write-Host "`n  $('─' * 50)" -ForegroundColor DarkGray
Write-Host ("  {0,-20} {1,8:N2} MB ({2} files)" -f "TOTAL", $totalSize, $totalFiles) -ForegroundColor Green

Write-Host "`n📦 Output Directory:" -ForegroundColor Cyan
Write-Host "  $((Get-Item $PublishDir).FullName)" -ForegroundColor White

Write-Host "`n✅ Ready for Inno Setup Installer!" -ForegroundColor Green
Write-Host "`nℹ️  Deployment Info:" -ForegroundColor Cyan
Write-Host "  • Self-contained with .NET 8 runtime included" -ForegroundColor White
Write-Host "  • No .NET installation required on target" -ForegroundColor White
Write-Host "  • Compatible with air-gapped/DMZ environments" -ForegroundColor White
Write-Host "  • Works on Windows Server 2019+ with no internet" -ForegroundColor White

Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor DarkGray

