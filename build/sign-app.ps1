#requires -version 5.1
# ZL File Relay - Code Signing Script
# Signs all executables and main DLLs

param(
    [string]$Configuration = "Release",
    [string]$Thumbprint = $env:CODESIGN_CERT_SHA1,
    [string]$TimestampUrl = "http://timestamp.digicert.com",
    [string]$Description = "ZL File Relay",
    [string]$DescriptionUrl = "https://github.com/ZentrixLabs/ZLFileRelay"
)

$ErrorActionPreference = 'Stop'

Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  ✍️  ZL FILE RELAY - CODE SIGNING                           ║" -ForegroundColor Yellow
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow

# Ensure we're in the repo root
try {
    $repoRoot = (& git rev-parse --show-toplevel 2>$null)
    if ($repoRoot) { 
        Set-Location -Path $repoRoot 
        Write-Host "`nWorking Directory: $repoRoot" -ForegroundColor DarkGray
    }
} catch { }

# Find signtool.exe
function Get-LatestSigntoolPath {
    $candidates = @()
    $kitsRoot = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
    if (Test-Path $kitsRoot) {
        Get-ChildItem -Path $kitsRoot -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match '^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$' } |
            Sort-Object { [version]$_.Name } -Descending |
            ForEach-Object {
                $p = Join-Path $_.FullName "x64\signtool.exe"
                if (Test-Path $p) { $candidates += $p }
            }
    }
    $alt = Join-Path ${env:ProgramFiles} "Windows Kits\10\bin\x64\signtool.exe"
    if (Test-Path $alt) { $candidates += $alt }
    try {
        $where = (where.exe signtool 2>$null | Select-Object -First 1)
        if ($where) { $candidates += $where }
    } catch {}
    $candidates | Select-Object -Unique | Select-Object -First 1
}

$SigntoolPath = Get-LatestSigntoolPath
if (-not $SigntoolPath) {
    throw "signtool.exe not found. Please install Windows 10/11 SDK."
}

Write-Host "Using signtool: $SigntoolPath" -ForegroundColor Cyan

if ([string]::IsNullOrWhiteSpace($Thumbprint)) {
    throw "Provide a certificate thumbprint via -Thumbprint or CODESIGN_CERT_SHA1 environment variable."
}

Write-Host "Certificate Thumbprint: $Thumbprint" -ForegroundColor DarkGray
Write-Host "Timestamp Server: $TimestampUrl" -ForegroundColor DarkGray

# Define files to sign
$filesToSign = @(
    # Service executable
    "src/ZLFileRelay.Service/bin/$Configuration/net8.0/ZLFileRelay.Service.exe",
    
    # WebPortal executable
    "src/ZLFileRelay.WebPortal/bin/$Configuration/net8.0/ZLFileRelay.WebPortal.exe",
    
    # ConfigTool executable
    "src/ZLFileRelay.ConfigTool/bin/$Configuration/net8.0-windows/ZLFileRelay.ConfigTool.exe",
    
    # Core library (shared across all components)
    "src/ZLFileRelay.Core/bin/$Configuration/net8.0/ZLFileRelay.Core.dll"
)

# Sign each file
$signedCount = 0
$failedCount = 0
$skippedCount = 0

Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "Starting code signing process..." -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray

foreach ($file in $filesToSign) {
    $filePath = Join-Path (Get-Location) $file
    $fileName = Split-Path $file -Leaf
    
    Write-Host "`n🔍 Processing: $fileName" -ForegroundColor White
    Write-Host "   Path: $file" -ForegroundColor DarkGray
    
    if (-not (Test-Path $filePath)) {
        Write-Host "   ⚠️  File not found - skipping" -ForegroundColor Yellow
        $skippedCount++
        continue
    }
    
    try {
        Write-Host "   ✍️  Signing..." -ForegroundColor Cyan
        
        & $SigntoolPath sign `
            /fd SHA256 `
            /td SHA256 `
            /tr $TimestampUrl `
            /sha1 $Thumbprint `
            /d $Description `
            /du $DescriptionUrl `
            "$filePath" 2>&1 | Out-Null
        
        if ($LASTEXITCODE -ne 0) {
            throw "SignTool failed with exit code $LASTEXITCODE"
        }
        
        Write-Host "   ✅ Signed successfully" -ForegroundColor Green
        
        # Verify signature
        Write-Host "   🔍 Verifying..." -ForegroundColor DarkGray
        & $SigntoolPath verify /pa /all $filePath 2>&1 | Out-Null
        
        if ($LASTEXITCODE -ne 0) {
            throw "Signature verification failed"
        }
        
        Write-Host "   ✅ Verified" -ForegroundColor Green
        $signedCount++
        
    } catch {
        Write-Host "   ❌ Failed: $_" -ForegroundColor Red
        $failedCount++
    }
}

# Summary
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray

if ($failedCount -eq 0 -and $signedCount -gt 0) {
    Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║  ✅ CODE SIGNING COMPLETE - ALL FILES SIGNED                ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    
    Write-Host "`n📊 Summary:" -ForegroundColor Cyan
    Write-Host "  ✅ Signed:  $signedCount files" -ForegroundColor Green
    if ($skippedCount -gt 0) {
        Write-Host "  ⚠️  Skipped: $skippedCount files" -ForegroundColor Yellow
    }
    
    Write-Host "`n✅ All executables are code signed and verified!" -ForegroundColor Green
    Write-Host ""
    
} else {
    Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║  ❌ CODE SIGNING FAILED                                     ║" -ForegroundColor Red
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Red
    
    Write-Host "`n📊 Summary:" -ForegroundColor Cyan
    Write-Host "  ✅ Signed:  $signedCount files" -ForegroundColor Green
    Write-Host "  ❌ Failed:  $failedCount files" -ForegroundColor Red
    if ($skippedCount -gt 0) {
        Write-Host "  ⚠️  Skipped: $skippedCount files" -ForegroundColor Yellow
    }
    
    Write-Host ""
    throw "Code signing completed with errors"
}

