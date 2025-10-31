#requires -version 5.1
# ZL File Relay - Automated Version Update
# Updates version across all project files and installer

param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

# Ensure we're in the repo root
try {
    $repoRoot = (& git rev-parse --show-toplevel 2>$null)
    if ($repoRoot) { 
        Set-Location -Path $repoRoot 
    }
} catch { }

Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  🔄 ZL FILE RELAY - VERSION UPDATE                          ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "`nUpdating to version: $Version" -ForegroundColor Yellow

# Validate version format (basic)
if ($Version -notmatch '^\d+\.\d+\.\d+') {
    Write-Host "⚠️  Warning: Version format should be X.Y.Z (e.g., 1.0.0)" -ForegroundColor Yellow
    $continue = Read-Host "Continue anyway? (y/N)"
    if ($continue -ne 'y') {
        exit 0
    }
}

# Define all project files to update
$projectFiles = @(
    "src/ZLFileRelay.Core/ZLFileRelay.Core.csproj",
    "src/ZLFileRelay.Service/ZLFileRelay.Service.csproj",
    "src/ZLFileRelay.WebPortal/ZLFileRelay.WebPortal.csproj",
    "src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj"
)

$issFile = "installer/ZLFileRelay.iss"

$updatedCount = 0
$failedCount = 0

# Function to update .csproj file
function Update-ProjectFile {
    param([string]$Path, [string]$Version)
    
    if (-not (Test-Path $Path)) {
        Write-Host "  ❌ File not found: $Path" -ForegroundColor Red
        return $false
    }
    
    Write-Host "  📝 Updating: $Path" -ForegroundColor Cyan
    
    try {
        $content = Get-Content -Path $Path -Raw -Encoding UTF8
        
        # Update or insert Version in PropertyGroup
        if ($content -match '<Version>.*?</Version>') {
            # Replace existing version
            $content = $content -replace '<Version>[^<]*</Version>', "<Version>$Version</Version>"
            Write-Host "     ✅ Updated existing <Version> tag" -ForegroundColor Green
        } elseif ($content -match '(?s)(<PropertyGroup>)(.*?)(</PropertyGroup>)') {
            # Insert Version after PropertyGroup opening tag
            $content = [System.Text.RegularExpressions.Regex]::Replace(
                $content,
                '(?s)(<PropertyGroup>)(.*?)(</PropertyGroup>)',
                "`$1`r`n    <Version>$Version</Version>`$2`$3",
                [System.Text.RegularExpressions.RegexOptions]::Multiline
            )
            Write-Host "     ✅ Inserted new <Version> tag" -ForegroundColor Green
        } else {
            Write-Host "     ⚠️  Could not find PropertyGroup to insert version" -ForegroundColor Yellow
            return $false
        }
        
        Set-Content -Path $Path -Value $content -Encoding UTF8 -NoNewline
        return $true
        
    } catch {
        Write-Host "     ❌ Error: $_" -ForegroundColor Red
        return $false
    }
}

# Function to update Inno Setup .iss file
function Update-IssFile {
    param([string]$Path, [string]$Version)
    
    if (-not (Test-Path $Path)) {
        Write-Host "  ❌ File not found: $Path" -ForegroundColor Red
        return $false
    }
    
    Write-Host "  📝 Updating: $Path" -ForegroundColor Cyan
    
    try {
        $content = Get-Content -Path $Path -Raw -Encoding UTF8
        
        # Update #define MyAppVersion line
        if ($content -match '(?m)^#define\s+MyAppVersion\s+".*?"') {
            $content = [System.Text.RegularExpressions.Regex]::Replace(
                $content,
                '(?m)^(#define\s+MyAppVersion\s+)"[^"]*"',
                ('$1"{0}"' -f $Version)
            )
            Write-Host "     ✅ Updated MyAppVersion define" -ForegroundColor Green
        } else {
            Write-Host "     ⚠️  Could not find MyAppVersion define" -ForegroundColor Yellow
            return $false
        }
        
        Set-Content -Path $Path -Value $content -Encoding UTF8 -NoNewline
        return $true
        
    } catch {
        Write-Host "     ❌ Error: $_" -ForegroundColor Red
        return $false
    }
}

# Update all project files
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "Updating .csproj files..." -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray

foreach ($projectFile in $projectFiles) {
    if (Update-ProjectFile -Path $projectFile -Version $Version) {
        $updatedCount++
    } else {
        $failedCount++
    }
}

# Update Inno Setup file
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "Updating Inno Setup file..." -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray

if (Update-IssFile -Path $issFile -Version $Version) {
    $updatedCount++
} else {
    $failedCount++
}

# Summary
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray

if ($failedCount -eq 0) {
    Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║  ✅ VERSION UPDATE COMPLETE                                 ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    
    Write-Host "`n📊 Summary:" -ForegroundColor Cyan
    Write-Host "  ✅ Updated: $updatedCount files" -ForegroundColor Green
    Write-Host "  📌 Version: $Version" -ForegroundColor White
    
    Write-Host "`n📝 Files Updated:" -ForegroundColor Cyan
    foreach ($file in $projectFiles) {
        Write-Host "  • $file" -ForegroundColor White
    }
    Write-Host "  • $issFile" -ForegroundColor White
    
    Write-Host "`n⚠️  Don't forget to update:" -ForegroundColor Yellow
    Write-Host "  • CHANGELOG.md (add release notes)" -ForegroundColor White
    
    Write-Host "`n✅ Version updated successfully!" -ForegroundColor Green
    Write-Host ""
    
} else {
    Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║  ❌ VERSION UPDATE INCOMPLETE                               ║" -ForegroundColor Red
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Red
    
    Write-Host "`n📊 Summary:" -ForegroundColor Cyan
    Write-Host "  ✅ Updated: $updatedCount files" -ForegroundColor Green
    Write-Host "  ❌ Failed:  $failedCount files" -ForegroundColor Red
    
    Write-Host "`n⚠️  Some files could not be updated. Please check the errors above." -ForegroundColor Yellow
    Write-Host ""
    
    throw "Version update completed with errors"
}

