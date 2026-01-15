param(
	[string]$Configuration = "Release",
	[string]$Thumbprint = $env:CODESIGN_CERT_SHA1,
	[string]$TimestampUrl = $(if ($env:CODESIGN_TIMESTAMP_URL) { $env:CODESIGN_TIMESTAMP_URL } else { "http://timestamp.sectigo.com/rfc3161" }),
	[string]$Description = "ZL File Relay",
	[string]$DescriptionUrl = "https://github.com/ZentrixLabs/ZLFileRelay"
)

$ErrorActionPreference = 'Stop'

# Resolve relative path
$scriptRoot = Split-Path -Parent $PSScriptRoot
$root = Resolve-Path "$scriptRoot\.."
Set-Location $root

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

Write-Host "Using signtool at: $SigntoolPath" -ForegroundColor Cyan

if ([string]::IsNullOrWhiteSpace($Thumbprint)) {
	throw "Provide a certificate thumbprint via -Thumbprint or CODESIGN_CERT_SHA1 environment variable."
}

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

foreach ($file in $filesToSign) {
	$filePath = Join-Path (Get-Location) $file
	$fileName = Split-Path $file -Leaf
	
	if (-not (Test-Path $filePath)) {
		Write-Host "File not found, skipping: $file" -ForegroundColor Yellow
		$skippedCount++
		continue
	}
	
	try {
		Write-Host "Signing: $fileName" -ForegroundColor Yellow
		Write-Host "  Path: $file" -ForegroundColor DarkGray
		
		& $SigntoolPath sign /fd SHA256 /td SHA256 /tr $TimestampUrl /sha1 $Thumbprint /d $Description /du $DescriptionUrl "$filePath"
		
		if ($LASTEXITCODE -ne 0) {
			throw "SignTool failed with exit code $LASTEXITCODE"
		}
		
		Write-Host "Verifying signature..." -ForegroundColor Yellow
		& $SigntoolPath verify /pa /all $filePath
		if ($LASTEXITCODE -ne 0) {
			throw "Signature verification failed"
		}
		
		Write-Host "Signed and verified: $filePath" -ForegroundColor Green
		$signedCount++
		
	} catch {
		Write-Host "Failed: $_" -ForegroundColor Red
		$failedCount++
	}
}

if ($failedCount -gt 0) {
	throw "Code signing completed with errors. Signed: $signedCount, Failed: $failedCount, Skipped: $skippedCount"
}

Write-Host "`nAll files signed successfully. Signed: $signedCount, Skipped: $skippedCount" -ForegroundColor Green

