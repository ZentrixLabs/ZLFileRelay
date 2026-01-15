#requires -version 5.1
param(
    [Parameter(Mandatory=$true)][string]$Version,
    [string]$InstallerPath = "artifacts\ZLFileRelaySetup.exe",
    [string]$Notes = "Signed release $Version"
)

$ErrorActionPreference = "Stop"

Write-Host "Preparing release v$Version" -ForegroundColor Green

# Ensure we're operating at the repository root so relative paths resolve correctly
$originalLocation = Get-Location
$repoRoot = $null

# Method 1: Try git rev-parse
try {
    $repoRoot = (& git rev-parse --show-toplevel 2>$null)
} catch { }

# Method 2: If that fails, walk up directory tree looking for .git
if (-not $repoRoot) {
    $current = Get-Location
    while ($current -and $current.Path -ne $current.Drive.Root) {
        $gitPath = Join-Path $current.Path ".git"
        if (Test-Path $gitPath) {
            $repoRoot = $current.Path
            break
        }
        $current = $current.Parent
    }
}

# Method 3: Try script location's parent directory
if (-not $repoRoot) {
    $scriptDir = Split-Path -Parent $PSScriptRoot
    if (Test-Path (Join-Path $scriptDir ".git")) {
        $repoRoot = $scriptDir
    }
}

if ($repoRoot) {
    Set-Location -Path $repoRoot
    Write-Host "Repository root: $repoRoot" -ForegroundColor DarkGray
    
    # Verify we're actually in a git repo
    $verify = & git rev-parse --show-toplevel 2>$null
    if (-not $verify) {
        throw "Failed to verify git repository at $repoRoot"
    }
} else {
    throw "Could not find git repository root. Please run from repository directory."
}

# Resolve absolute installer path relative to repo root
$installer = Resolve-Path -Path $InstallerPath -ErrorAction SilentlyContinue
if (-not $installer) {
    throw "Installer not found at $InstallerPath"
}
$installer = $installer.Path

# Validate size (< 2 GB per GitHub limit)
$fileInfo = Get-Item $installer
if ($fileInfo.Length -ge 2GB) {
    throw "Installer exceeds GitHub asset limit (>= 2 GB): $($fileInfo.Length) bytes"
}

# Ensure GitHub CLI is available (auto-detect common install paths if PATH hasn't refreshed)
$gh = Get-Command gh -ErrorAction SilentlyContinue
if (-not $gh) {
    $ghCandidates = @(
        (Join-Path ${env:ProgramFiles} "GitHub CLI\gh.exe"),
        (Join-Path ${env:LOCALAPPDATA} "Programs\GitHub CLI\gh.exe")
    )
    foreach ($cand in $ghCandidates) {
        if (Test-Path $cand) {
            $env:Path = (Split-Path $cand) + ";" + $env:Path
            $gh = Get-Command gh -ErrorAction SilentlyContinue
            if ($gh) { break }
        }
    }
}
if (-not $gh) {
    throw "GitHub CLI (gh) not found. Open a new PowerShell window or add it to PATH."
}

$tag = "v$Version"

# Compute SHA256 and write alongside installer (if not already exists)
$shaFile = "$installer.sha256"
if (-not (Test-Path $shaFile)) {
	$sha = (Get-FileHash -Algorithm SHA256 $installer).Hash
	Set-Content -Path $shaFile -NoNewline -Value $sha
	Write-Host "Created checksum file: $shaFile" -ForegroundColor DarkGray
}

# Verify we're in repo root for git/gh to pick the correct remote
$currentRepoRoot = $null
try {
    $currentRepoRoot = (& git rev-parse --show-toplevel 2>$null)
    if ($currentRepoRoot) { 
        Set-Location -Path $currentRepoRoot
        Write-Host "Verified repository root: $currentRepoRoot" -ForegroundColor DarkGray
    } else {
        throw "Not in a git repository"
    }
} catch { 
    Write-Host "ERROR: Not in a git repository. Current directory: $(Get-Location)" -ForegroundColor Red
    Write-Host "Please run this script from the repository root or ensure .git directory exists." -ForegroundColor Red
    throw "Git repository not found"
}

# Verify we're still in the repo before running git commands
$verifyRepo = & git rev-parse --show-toplevel 2>$null
if (-not $verifyRepo) {
    throw "Lost git repository context. Current directory: $(Get-Location)"
}

# Check if tag exists locally or remotely
$tagExists = $false
try {
    $null = & git rev-parse "$tag" 2>$null
    $tagExists = $true
} catch { }

if (-not $tagExists) {
    try {
        $remoteTags = & git ls-remote --tags origin "$tag" 2>$null
        if ($remoteTags -match $tag) {
            $tagExists = $true
        }
    } catch { }
}

if ($tagExists) {
    Write-Host "Tag $tag exists. Replacing it (force)." -ForegroundColor Yellow
    # Move local tag to current HEAD and force-push
    & git tag -a $tag -f -m "ZL File Relay $Version"
    if ($LASTEXITCODE -ne 0) { throw "Failed to create tag" }
    
    & git push origin ":refs/tags/$tag" 2>&1 | Out-Null  # Ignore errors if tag doesn't exist remotely
    & git push origin $tag
    if ($LASTEXITCODE -ne 0) { throw "Failed to push tag" }
} else {
    Write-Host "Creating tag $tag and pushing to origin." -ForegroundColor Yellow
    & git tag -a $tag -m "ZL File Relay $Version"
    if ($LASTEXITCODE -ne 0) { throw "Failed to create tag" }
    
    & git push origin $tag
    if ($LASTEXITCODE -ne 0) { throw "Failed to push tag" }
}

# Determine if a release already exists for the tag
$releaseExists = $false
try {
    gh release view $tag 1>$null 2>$null
    $releaseExists = $true
} catch { }

if ($releaseExists) {
    Write-Host "Release $tag exists. Uploading assets (clobber)." -ForegroundColor Yellow
    gh release upload $tag "$installer" "$shaFile" --clobber
} else {
    Write-Host "Creating release $tag and uploading assets." -ForegroundColor Yellow
    gh release create $tag "$installer" "$shaFile" --title "ZL File Relay $tag" --notes "$Notes"
}

Write-Host "Release $tag published successfully." -ForegroundColor Green

