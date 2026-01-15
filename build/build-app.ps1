#requires -version 5.1
# ZL File Relay - Build All Components
# Builds Service, WebPortal, and ConfigTool for signing

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  🔨 ZL FILE RELAY - BUILD ALL COMPONENTS                    ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green

Write-Host "`nConfiguration: $Configuration" -ForegroundColor Cyan
Write-Host "Runtime: $Runtime" -ForegroundColor Cyan

# Ensure we're in the repo root
try {
    $repoRoot = (& git rev-parse --show-toplevel 2>$null)
    if ($repoRoot) { 
        Set-Location -Path $repoRoot 
        Write-Host "Working Directory: $repoRoot" -ForegroundColor DarkGray
    }
} catch { }

# Restore solution
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "📦 Restoring solution..." -ForegroundColor Cyan
dotnet restore "ZLFileRelay.sln"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Restore failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Restore complete" -ForegroundColor Green

# Build Core library (shared)
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "📚 Building ZLFileRelay.Core..." -ForegroundColor Cyan
dotnet build "src/ZLFileRelay.Core/ZLFileRelay.Core.csproj" `
    --configuration $Configuration `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Core build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Core built" -ForegroundColor Green

# Build Service
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "🔧 Building ZLFileRelay.Service..." -ForegroundColor Cyan
dotnet build "src/ZLFileRelay.Service/ZLFileRelay.Service.csproj" `
    --configuration $Configuration `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Service build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Service built" -ForegroundColor Green

# Build WebPortal
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "🌐 Building ZLFileRelay.WebPortal..." -ForegroundColor Cyan
dotnet build "src/ZLFileRelay.WebPortal/ZLFileRelay.WebPortal.csproj" `
    --configuration $Configuration `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ WebPortal build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ WebPortal built" -ForegroundColor Green

# Build ConfigTool
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "🖥️  Building ZLFileRelay.ConfigTool..." -ForegroundColor Cyan
dotnet build "src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj" `
    --configuration $Configuration `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ConfigTool build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ ConfigTool built" -ForegroundColor Green

# Publish self-contained components to publish folder for installer
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "📦 Publishing self-contained components for installer..." -ForegroundColor Cyan
Write-Host "   (Includes .NET 8 runtime - required for installer)" -ForegroundColor DarkGray

# Restore with runtime identifier for self-contained publish
Write-Host "`n   Restoring packages for runtime $Runtime..." -ForegroundColor Cyan
dotnet restore "ZLFileRelay.sln" -r $Runtime
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Restore failed!" -ForegroundColor Red
    exit 1
}

# Publish Service (self-contained)
Write-Host "`n   Publishing Service..." -ForegroundColor Cyan
dotnet publish "src/ZLFileRelay.Service/ZLFileRelay.Service.csproj" `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -o "publish/Service"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Service publish failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Service published (self-contained)" -ForegroundColor Green

# Publish WebPortal (self-contained)
Write-Host "`n   Publishing WebPortal..." -ForegroundColor Cyan
dotnet publish "src/ZLFileRelay.WebPortal/ZLFileRelay.WebPortal.csproj" `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -o "publish/WebPortal"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ WebPortal publish failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ WebPortal published (self-contained)" -ForegroundColor Green

# Publish ConfigTool (self-contained single file)
Write-Host "`n   Publishing ConfigTool (single file)..." -ForegroundColor Cyan
$publishConfigDir = "publish/ConfigTool"
if (-not (Test-Path $publishConfigDir)) {
    New-Item -ItemType Directory -Force -Path $publishConfigDir | Out-Null
}

dotnet publish "src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj" `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishConfigDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ConfigTool publish failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ ConfigTool published (self-contained single file)" -ForegroundColor Green

# Copy appsettings.json if it doesn't exist in publish
if (-not (Test-Path "publish/appsettings.json")) {
    if (Test-Path "appsettings.json") {
        Copy-Item -Path "appsettings.json" -Destination "publish/appsettings.json" -Force
        Write-Host "✅ appsettings.json copied to publish folder" -ForegroundColor Green
    }
}

# Copy docs folder (excluding archive and development folders)
$docsSource = "docs"
$docsDest = "publish/docs"
if (Test-Path $docsSource) {
    if (Test-Path $docsDest) {
        Remove-Item $docsDest -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $docsDest | Out-Null
    
    # Copy all .md files from docs root, excluding archive folder
    Get-ChildItem -Path $docsSource -Filter "*.md" -File | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $docsDest -Force
    }
    Write-Host "✅ Documentation copied to publish folder (excluding archive)" -ForegroundColor Green
} else {
    Write-Host "⚠️  Docs folder not found at $docsSource - skipping" -ForegroundColor Yellow
}

# Summary
Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  ✅ BUILD COMPLETE - ALL COMPONENTS                         ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green

Write-Host "`n📂 Build Output Directories:" -ForegroundColor Cyan
Write-Host "  • Service:    src/ZLFileRelay.Service/bin/$Configuration/net8.0/" -ForegroundColor White
Write-Host "  • WebPortal:  src/ZLFileRelay.WebPortal/bin/$Configuration/net8.0/" -ForegroundColor White
Write-Host "  • ConfigTool: src/ZLFileRelay.ConfigTool/bin/$Configuration/net8.0-windows/" -ForegroundColor White

Write-Host "`n📦 Published Components (Self-Contained with .NET 8):" -ForegroundColor Cyan
Get-ChildItem "publish" -Directory | ForEach-Object {
    $size = (Get-ChildItem $_.FullName -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB
    $fileCount = (Get-ChildItem $_.FullName -Recurse -File -ErrorAction SilentlyContinue | Measure-Object).Count
    Write-Host ("  • {0,-15} {1,8:N2} MB ({2} files)" -f $_.Name, $size, $fileCount) -ForegroundColor White
}

Write-Host "`n✅ Ready for code signing!" -ForegroundColor Green
Write-Host "`n💡 Note: All components published as self-contained to publish/ folder" -ForegroundColor Cyan
Write-Host "   • Includes .NET 8 runtime (no external dependencies)" -ForegroundColor DarkGray
Write-Host "   • Ready for Inno Setup installer packaging" -ForegroundColor DarkGray
Write-Host ""

