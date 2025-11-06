param(
	[Parameter(Mandatory=$true)][string]$Version,
	[switch]$Sign,
	[switch]$Draft
)

$ErrorActionPreference = 'Stop'

# Ensure we operate from repository root
$root = Resolve-Path "$PSScriptRoot\.."
Set-Location $root

$artifacts = Join-Path $root 'artifacts'
if (-not (Test-Path $artifacts)) {
	New-Item -ItemType Directory -Path $artifacts | Out-Null
}

Write-Host "=== ZL File Relay Release Workflow ===" -ForegroundColor Green

# Step 1: Update version in project files
Write-Host "`nStep 1: Updating version to $Version..." -ForegroundColor Cyan
& "$PSScriptRoot\update-version.ps1" -Version $Version

if ($Sign) {
	# Step 2: Build and sign the app
	Write-Host "`nStep 2: Please build in Visual Studio (Release), then we'll sign the executables..." -ForegroundColor Yellow
	Read-Host "Press Enter after building in Visual Studio"
	
	Write-Host "Signing executables..." -ForegroundColor Yellow
	& "$PSScriptRoot\sign-app.ps1"
} else {
	# Step 2: Just build
	Write-Host "`nStep 2: Please build in Visual Studio (Release)..." -ForegroundColor Yellow
	Read-Host "Press Enter after building in Visual Studio"
}

# Step 3: Compile installer in Inno Setup GUI
Write-Host "`nStep 3: Please compile the installer in Inno Setup GUI..." -ForegroundColor Yellow
Write-Host "  Open installer/ZLFileRelay.iss and compile it" -ForegroundColor DarkGray
Write-Host "  The installer will be signed automatically via SignTool (already configured in IDE)" -ForegroundColor DarkGray
Read-Host "Press Enter after the installer has been generated in 'artifacts' or 'installer/output'"

# Step 4: Find the installer
$installer = $null
$searchPaths = @(
	(Join-Path $artifacts 'ZLFileRelaySetup*.exe'),
	(Join-Path $artifacts 'ZLFileRelay-Setup*.exe'),
	(Join-Path (Join-Path $root 'installer') 'output\ZLFileRelaySetup*.exe'),
	(Join-Path (Join-Path $root 'installer') 'output\ZLFileRelay-Setup*.exe')
)

foreach ($pattern in $searchPaths) {
	$found = Get-ChildItem -LiteralPath (Split-Path $pattern -Parent) -Filter (Split-Path $pattern -Leaf) -ErrorAction SilentlyContinue | 
		Sort-Object LastWriteTime -Descending | 
		Select-Object -First 1
	if ($found) {
		$installer = $found
		break
	}
}

if (-not $installer) { 
	throw "No installer found. Ensure Inno Setup output is configured to 'artifacts' or 'installer/output'." 
}

Write-Host "Found installer: $($installer.Name)" -ForegroundColor Green

# Step 5: Create checksum
Write-Host "`nStep 4: Creating checksum..." -ForegroundColor Cyan
$shaFile = "$($installer.FullName).sha256"
$sha = (Get-FileHash -Algorithm SHA256 $installer.FullName).Hash
Set-Content -Path $shaFile -NoNewline -Value $sha
Write-Host "Checksum: $shaFile" -ForegroundColor Green

# Step 6: Upload to GitHub
Write-Host "`nStep 5: Uploading to GitHub..." -ForegroundColor Cyan
$tag = "v$Version"
$title = "ZL File Relay $Version"
$notes = "Release $Version"

if (Test-Path "$PSScriptRoot\upload-release.ps1") {
	& "$PSScriptRoot\upload-release.ps1" -Version $Version -InstallerPath $installer.FullName -Notes $notes
} else {
	Write-Host "upload-release.ps1 not found. Skipping GitHub upload." -ForegroundColor Yellow
	Write-Host "To upload manually, run:" -ForegroundColor Yellow
	Write-Host "  .\build\upload-release.ps1 -Version $Version -InstallerPath `"$($installer.FullName)`"" -ForegroundColor DarkGray
}

Write-Host "`n=== Release complete ===" -ForegroundColor Green
Write-Host "Release: $tag" -ForegroundColor Cyan
Write-Host "Installer: $($installer.FullName)" -ForegroundColor Cyan
if ($Draft) {
	Write-Host "Published as draft release" -ForegroundColor Yellow
}

