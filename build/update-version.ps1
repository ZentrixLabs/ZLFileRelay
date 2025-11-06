param(
	[Parameter(Mandatory=$true)][string]$Version,
	[string]$IssPath = "${PSScriptRoot}\..\installer\ZLFileRelay.iss"
)

$ErrorActionPreference = 'Stop'

# Ensure we operate from repository root
$root = Resolve-Path "$PSScriptRoot\.."
Set-Location $root

if (-not (Test-Path -LiteralPath $IssPath)) {
	throw "ISS file not found at: $IssPath"
}

# Define all project files to update
$projectFiles = @(
	"src/ZLFileRelay.Core/ZLFileRelay.Core.csproj",
	"src/ZLFileRelay.Service/ZLFileRelay.Service.csproj",
	"src/ZLFileRelay.WebPortal/ZLFileRelay.WebPortal.csproj",
	"src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj"
)

# Update .csproj files
Write-Host "Updating version in project files..." -ForegroundColor Cyan
foreach ($projectFile in $projectFiles) {
	if (-not (Test-Path $projectFile)) {
		Write-Host "Project file not found: $projectFile" -ForegroundColor Yellow
		continue
	}
	
	$content = Get-Content -LiteralPath $projectFile -Raw
	
	# Update or insert Version in PropertyGroup
	if ($content -match '<Version>.*?</Version>') {
		$content = $content -replace '<Version>[^<]*</Version>', "<Version>$Version</Version>"
	} elseif ($content -match '(?s)(<PropertyGroup>)(.*?)(</PropertyGroup>)') {
		$content = [System.Text.RegularExpressions.Regex]::Replace(
			$content,
			'(?s)(<PropertyGroup>)(.*?)(</PropertyGroup>)',
			"`$1`r`n    <Version>$Version</Version>`$2`$3",
			[System.Text.RegularExpressions.RegexOptions]::Multiline
		)
	}
	
	Set-Content -LiteralPath $projectFile -Value $content -Encoding UTF8 -NoNewline
	Write-Host "  Updated $projectFile" -ForegroundColor Green
}

# Update ISS file
Write-Host "Updating version in $IssPath..." -ForegroundColor Cyan
$issContent = Get-Content -LiteralPath $IssPath -Raw

# Update or insert the MyAppVersion define
if ($issContent -match '(?m)^#define\s+MyAppVersion\s+".*?"') {
	$issContent = [System.Text.RegularExpressions.Regex]::Replace(
		$issContent,
		'(?m)^(#define\s+MyAppVersion\s+)"[^"]*"',
		('$1"{0}"' -f $Version)
	)
} else {
	# Insert after MyAppName define
	$issContent = [System.Text.RegularExpressions.Regex]::Replace(
		$issContent,
		'(?m)^(#define\s+MyAppName\s+".*?"\r?\n)',
		("`$1#define MyAppVersion `"$Version`"`r`n")
	)
}

Set-Content -LiteralPath $IssPath -Value $issContent -Encoding UTF8 -NoNewline
Write-Host "  Updated MyAppVersion to $Version in ISS" -ForegroundColor Green

Write-Host "`nVersion $Version synchronized across project files and installer" -ForegroundColor Green

