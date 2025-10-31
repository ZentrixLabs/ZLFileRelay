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

# Summary
Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  ✅ BUILD COMPLETE - ALL COMPONENTS                         ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green

Write-Host "`n📂 Output Directories:" -ForegroundColor Cyan
Write-Host "  • Service:    src/ZLFileRelay.Service/bin/$Configuration/net8.0/" -ForegroundColor White
Write-Host "  • WebPortal:  src/ZLFileRelay.WebPortal/bin/$Configuration/net8.0/" -ForegroundColor White
Write-Host "  • ConfigTool: src/ZLFileRelay.ConfigTool/bin/$Configuration/net8.0-windows/" -ForegroundColor White

Write-Host "`n✅ Ready for code signing!" -ForegroundColor Green
Write-Host ""

