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

# Copy built components to publish folder for installer
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "📦 Copying built components to publish folder for installer..." -ForegroundColor Cyan

# Copy Service
$serviceSource = "src/ZLFileRelay.Service/bin/$Configuration/net8.0"
$serviceDest = "publish/Service"
if (Test-Path $serviceSource) {
    if (Test-Path $serviceDest) {
        Remove-Item $serviceDest -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $serviceDest | Out-Null
    Copy-Item -Path "$serviceSource\*" -Destination $serviceDest -Recurse -Force
    Write-Host "✅ Service copied to publish folder" -ForegroundColor Green
} else {
    Write-Host "⚠️  Service not found at $serviceSource - skipping" -ForegroundColor Yellow
}

# Copy WebPortal
$webPortalSource = "src/ZLFileRelay.WebPortal/bin/$Configuration/net8.0"
$webPortalDest = "publish/WebPortal"
if (Test-Path $webPortalSource) {
    if (Test-Path $webPortalDest) {
        Remove-Item $webPortalDest -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $webPortalDest | Out-Null
    Copy-Item -Path "$webPortalSource\*" -Destination $webPortalDest -Recurse -Force
    Write-Host "✅ WebPortal copied to publish folder" -ForegroundColor Green
} else {
    Write-Host "⚠️  WebPortal not found at $webPortalSource - skipping" -ForegroundColor Yellow
}

# Copy ConfigTool
$configToolSource = "src/ZLFileRelay.ConfigTool/bin/$Configuration/net8.0-windows/ZLFileRelay.ConfigTool.exe"
$configToolDest = "publish/ConfigTool/ZLFileRelay.ConfigTool.exe"
if (Test-Path $configToolSource) {
    $publishConfigDir = "publish/ConfigTool"
    if (-not (Test-Path $publishConfigDir)) {
        New-Item -ItemType Directory -Force -Path $publishConfigDir | Out-Null
    }
    Copy-Item -Path $configToolSource -Destination $configToolDest -Force
    Write-Host "✅ ConfigTool copied to publish folder" -ForegroundColor Green
} else {
    Write-Host "⚠️  ConfigTool not found at $configToolSource - skipping" -ForegroundColor Yellow
}

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

Write-Host "`n📂 Output Directories:" -ForegroundColor Cyan
Write-Host "  • Service:    src/ZLFileRelay.Service/bin/$Configuration/net8.0/" -ForegroundColor White
Write-Host "  • WebPortal:  src/ZLFileRelay.WebPortal/bin/$Configuration/net8.0/" -ForegroundColor White
Write-Host "  • ConfigTool: src/ZLFileRelay.ConfigTool/bin/$Configuration/net8.0-windows/" -ForegroundColor White

Write-Host "`n✅ Ready for code signing!" -ForegroundColor Green
Write-Host "`n💡 Note: All components copied to publish/ folder for installer" -ForegroundColor Cyan
Write-Host "   • Service:    publish/Service/" -ForegroundColor DarkGray
Write-Host "   • WebPortal:  publish/WebPortal/" -ForegroundColor DarkGray
Write-Host "   • ConfigTool: publish/ConfigTool/" -ForegroundColor DarkGray
Write-Host ""

