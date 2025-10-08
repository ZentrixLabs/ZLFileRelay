# Self-Contained Deployment for DMZ/OT Environments

**Critical Requirement:** ZL File Relay must deploy to air-gapped, restricted DMZ and OT (Operational Technology) networks.

---

## 🎯 Requirements

### DMZ/OT Environment Constraints
- ❌ **No Internet Access** - Cannot download .NET runtime from Microsoft
- ❌ **No Framework Dependencies** - May not have .NET installed
- ❌ **Security Restrictions** - Installing frameworks requires approvals
- ❌ **Air-Gapped** - Completely isolated from external networks
- ✅ **Single Installer** - Must include everything needed

### Solution: Self-Contained Deployment

**Self-Contained** means:
- ✅ .NET 8 runtime **bundled with the application**
- ✅ No external dependencies to install
- ✅ Single installer includes everything
- ✅ Works on clean Windows install
- ✅ ~60-80MB larger but worth it for DMZ/OT

---

## 📦 Publishing Configuration

### For Service (Windows Service)
```bash
dotnet publish src/ZLFileRelay.Service/ZLFileRelay.Service.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -o publish/Service
```

**Why not single file?**
- Windows Services work better as multi-file
- Easier debugging if needed
- Slightly faster startup

### For Web Portal (IIS)
```bash
dotnet publish src/ZLFileRelay.WebPortal/ZLFileRelay.WebPortal.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishTrimmed=false `
    -o publish/WebPortal
```

**Note:** IIS hosting requires multi-file deployment

### For Config Tool (Desktop App)
```bash
dotnet publish src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o publish/ConfigTool
```

**Single file is OK for desktop app** - easier for users to run

---

## 📏 Size Comparison

| Deployment Type | Size | DMZ/OT Compatible? |
|-----------------|------|-------------------|
| Framework-Dependent | ~5MB | ❌ Requires .NET 8 installed |
| Self-Contained | ~65MB | ✅ Includes .NET 8 |
| Self-Contained + Trimmed | ~45MB | ⚠️ May break reflection |
| Self-Contained + Single File | ~65MB | ✅ Best for desktop apps |

**Recommendation:** Self-contained, **NOT trimmed** (trimming can break dependency injection)

---

## 🔧 Project Configuration

### Add to Each .csproj

```xml
<PropertyGroup>
    <!-- Existing properties -->
    <TargetFramework>net8.0</TargetFramework>
    
    <!-- Self-Contained Deployment Settings -->
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>false</PublishSingleFile>
    <PublishTrimmed>false</PublishTrimmed>
    <PublishReadyToRun>true</PublishReadyToRun>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
</PropertyGroup>
```

**OR** specify at publish time (more flexible):
- Don't modify .csproj
- Specify flags in publish command
- Different builds for different scenarios

**Recommended:** Specify at publish time for flexibility

---

## 📦 Inno Setup Installer Configuration

### Files to Include

```inno
[Files]
; Service (self-contained with .NET 8)
Source: "publish\Service\*"; DestDir: "{app}\Service"; Flags: ignoreversion recursesubdirs

; Web Portal (self-contained with .NET 8)
Source: "publish\WebPortal\*"; DestDir: "{app}\WebPortal"; Flags: ignoreversion recursesubdirs

; Config Tool (self-contained with .NET 8, can be single file)
Source: "publish\ConfigTool\*"; DestDir: "{app}\ConfigTool"; Flags: ignoreversion recursesubdirs

; Configuration file
Source: "appsettings.json"; DestDir: "{commonappdata}\ZLFileRelay"; Flags: onlyifdoesntexist

; Documentation
Source: "docs\*"; DestDir: "{app}\docs"; Flags: ignoreversion recursesubdirs
```

### Installer Size Estimate

| Component | Size (approx) |
|-----------|---------------|
| Service + .NET 8 | ~70MB |
| Web Portal + .NET 8 | ~75MB |
| Config Tool + .NET 8 | ~65MB |
| Shared .NET (optimized) | ~60MB |
| **Total Installer** | **~150-200MB** |

**Note:** Actual size will be ~150MB due to shared runtime files (not 210MB)

---

## ✅ Verification Checklist

### Before Creating Installer

- [ ] Test self-contained Service on clean Windows Server
- [ ] Test self-contained Web Portal on clean Windows Server
- [ ] Test self-contained Config Tool on clean Windows install
- [ ] Verify no .NET installation required
- [ ] Test on Windows Server 2019 (minimum target)
- [ ] Test on Windows Server 2022
- [ ] Test in VM without internet
- [ ] Test complete workflow end-to-end

### After Creating Installer

- [ ] Install on clean Windows Server (no .NET)
- [ ] Verify all three components work
- [ ] Test service installation
- [ ] Test web portal in IIS
- [ ] Test config tool launch
- [ ] Verify file transfer works
- [ ] Test uninstall (clean removal)

---

## 🚀 Build Script

Create `build/publish-selfcontained.ps1`:

```powershell
# Self-Contained Publish Script for DMZ/OT Deployment
# Includes .NET 8 runtime - no external dependencies needed

$ErrorActionPreference = "Stop"
$PublishDir = "publish"

Write-Host "🚀 Publishing ZL File Relay (Self-Contained with .NET 8)" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray

# Clean previous publish
if (Test-Path $PublishDir) {
    Write-Host "`nCleaning previous publish..." -ForegroundColor Yellow
    Remove-Item $PublishDir -Recurse -Force
}

# Publish Service
Write-Host "`n📦 Publishing File Transfer Service..." -ForegroundColor Cyan
dotnet publish src/ZLFileRelay.Service/ZLFileRelay.Service.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -o "$PublishDir/Service"

# Publish Web Portal
Write-Host "`n🌐 Publishing Web Portal..." -ForegroundColor Cyan
dotnet publish src/ZLFileRelay.WebPortal/ZLFileRelay.WebPortal.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -o "$PublishDir/WebPortal"

# Publish Config Tool
Write-Host "`n🔧 Publishing Configuration Tool..." -ForegroundColor Cyan
dotnet publish src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "$PublishDir/ConfigTool"

# Copy configuration file
Write-Host "`n📄 Copying configuration..." -ForegroundColor Cyan
Copy-Item "appsettings.json" "$PublishDir/appsettings.json"

# Copy documentation
Write-Host "`n📚 Copying documentation..." -ForegroundColor Cyan
Copy-Item "docs" "$PublishDir/docs" -Recurse

Write-Host "`n✅ PUBLISH COMPLETE!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray

# Show sizes
Write-Host "`n📊 Published Sizes:" -ForegroundColor Cyan
Get-ChildItem $PublishDir -Directory | ForEach-Object {
    $size = (Get-ChildItem $_.FullName -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
    Write-Host ("  {0,-20} {1,8:N2} MB" -f $_.Name, $size) -ForegroundColor White
}

$totalSize = (Get-ChildItem $PublishDir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host ("  {0,-20} {1,8:N2} MB" -f "TOTAL", $totalSize) -ForegroundColor Green

Write-Host "`n📦 Output directory: $PublishDir" -ForegroundColor Cyan
Write-Host "Ready for Inno Setup installer!" -ForegroundColor Green
```

---

## 🎁 Inno Setup Configuration

Update `installer/ZLFileRelay.iss`:

```inno
#define MyAppName "ZL File Relay"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Your Company"
#define MyAppURL "https://yourcompany.com"

[Setup]
AppId={{YOUR-GUID-HERE}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\ZLFileRelay
DefaultGroupName={#MyAppName}
OutputDir=installer\output
OutputBaseFilename=ZLFileRelay-Setup-{#MyAppVersion}-SelfContained
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
UninstallDisplayIcon={app}\ConfigTool\ZLFileRelay.ConfigTool.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full"; Description: "Full Installation (Service + Web Portal + Config Tool)"
Name: "service"; Description: "Service Only"
Name: "custom"; Description: "Custom Installation"; Flags: iscustom

[Components]
Name: "service"; Description: "File Transfer Service"; Types: full service custom; Flags: fixed
Name: "webportal"; Description: "Web Upload Portal"; Types: full custom
Name: "configtool"; Description: "Configuration Tool"; Types: full custom

[Files]
; *** SELF-CONTAINED - INCLUDES .NET 8 RUNTIME ***
; Service (with .NET 8 runtime ~70MB)
Source: "publish\Service\*"; DestDir: "{app}\Service"; Components: service; Flags: ignoreversion recursesubdirs createallsubdirs

; Web Portal (with .NET 8 runtime ~75MB)
Source: "publish\WebPortal\*"; DestDir: "{app}\WebPortal"; Components: webportal; Flags: ignoreversion recursesubdirs createallsubdirs

; Config Tool (with .NET 8 runtime ~65MB single file)
Source: "publish\ConfigTool\ZLFileRelay.ConfigTool.exe"; DestDir: "{app}\ConfigTool"; Components: configtool; Flags: ignoreversion

; Configuration
Source: "publish\appsettings.json"; DestDir: "{commonappdata}\ZLFileRelay"; Flags: onlyifdoesntexist uninsneveruninstall

; Documentation
Source: "publish\docs\*"; DestDir: "{app}\docs"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{commonappdata}\ZLFileRelay"
Name: "C:\FileRelay\uploads"
Name: "C:\FileRelay\uploads\transfer"
Name: "C:\FileRelay\logs"
Name: "C:\FileRelay\archive"

[Icons]
Name: "{group}\ZL File Relay Configuration"; Filename: "{app}\ConfigTool\ZLFileRelay.ConfigTool.exe"
Name: "{group}\Documentation"; Filename: "{app}\docs\README.md"
Name: "{group}\Uninstall"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ZL File Relay Config"; Filename: "{app}\ConfigTool\ZLFileRelay.ConfigTool.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create desktop shortcut for Configuration Tool"

[Run]
; Open Config Tool after install
Filename: "{app}\ConfigTool\ZLFileRelay.ConfigTool.exe"; Description: "Launch Configuration Tool"; Flags: postinstall skipifsilent nowait

[UninstallRun]
; Stop and remove service before uninstall
Filename: "sc.exe"; Parameters: "stop ZLFileRelay"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete ZLFileRelay"; Flags: runhidden

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  
  // Check Windows version
  if not IsWin64 then
  begin
    MsgBox('This application requires 64-bit Windows.', mbCritical, MB_OK);
    Result := False;
    Exit;
  end;
  
  // Note: No need to check for .NET - it's included!
  MsgBox('This installer includes .NET 8 runtime.' + #13#10 + 
         'No additional framework installation needed!', 
         mbInformation, MB_OK);
end;
```

---

## 📋 Publish Commands for Each Component

### Quick Reference

```powershell
# Service
dotnet publish src/ZLFileRelay.Service -c Release -r win-x64 --self-contained true -o publish/Service

# Web Portal
dotnet publish src/ZLFileRelay.WebPortal -c Release -r win-x64 --self-contained true -o publish/WebPortal

# Config Tool (single file)
dotnet publish src/ZLFileRelay.ConfigTool -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/ConfigTool
```

---

## 📊 Deployment Sizes

### Self-Contained (.NET 8 Included)

| Component | Files | Size | Notes |
|-----------|-------|------|-------|
| Service | ~150 files | 65-70MB | Multi-file |
| Web Portal | ~200 files | 70-75MB | Multi-file (IIS) |
| Config Tool | 1 file | 60-65MB | Single .exe |
| Shared Runtime | Optimized | - | Inno Setup dedupes |
| **Installer** | - | **150-200MB** | Compressed |

### Framework-Dependent (NOT for DMZ/OT)

| Component | Files | Size | Requirement |
|-----------|-------|------|-------------|
| Service | ~20 files | 2MB | .NET 8 Runtime |
| Web Portal | ~30 files | 3MB | ASP.NET Core 8 |
| Config Tool | ~15 files | 1MB | .NET 8 Desktop Runtime |
| **Installer** | - | **10-15MB** | ❌ Needs .NET pre-installed |

---

## ✅ Benefits of Self-Contained

### For DMZ/OT Environments
- ✅ Works on air-gapped networks
- ✅ No internet access required
- ✅ No framework installation needed
- ✅ Single approval process
- ✅ Consistent runtime version
- ✅ No "works on my machine" issues
- ✅ Easier security auditing

### For General Deployment
- ✅ Simpler deployment process
- ✅ No .NET version conflicts
- ✅ Guaranteed compatibility
- ✅ Side-by-side with other apps
- ✅ Easier rollback

---

## ⚠️ Considerations

### Disk Space
- **Requirement:** ~300MB per installation
- **Recommendation:** Ensure 500MB free space

### Update Process
- Must replace entire runtime with each update
- Installer handles this automatically
- Old versions can be uninstalled cleanly

### Windows Server Core
- Self-contained works on Server Core
- No Desktop Experience needed for service
- Config Tool requires Desktop Experience

---

## 🔒 Security Scanning

### For OT/SCADA Environments

Many OT environments require security scanning of all executables:

1. **Publish self-contained build**
2. **Submit entire publish folder for scanning**
   - Include ALL .dll files
   - Include ALL .exe files
   - Include configuration samples
3. **Get security approval**
4. **Create installer from approved files**

**Tip:** Self-contained makes this easier - submit once, deploy everywhere

---

## 📝 Deployment Checklist

### Pre-Deployment
- [ ] Build self-contained packages
- [ ] Test on clean Windows install
- [ ] Test on Windows Server Core (service only)
- [ ] Test on air-gapped VM
- [ ] Verify no internet required
- [ ] Check total size < 250MB
- [ ] Security scan if required

### Installer Creation
- [ ] Run publish script
- [ ] Verify all files present
- [ ] Create Inno Setup installer
- [ ] Test installer on clean machine
- [ ] Test uninstaller
- [ ] Verify no registry leftovers

### Documentation
- [ ] Update README with installer size
- [ ] Document minimum requirements
- [ ] Note: No .NET installation needed
- [ ] Include offline deployment guide

---

## 🎯 Recommendation

**For ZL File Relay:**

### Production Builds
```powershell
# Always use self-contained for DMZ/OT compatibility
dotnet publish -c Release -r win-x64 --self-contained true
```

### Development Builds
```powershell
# Can use framework-dependent for faster builds
dotnet build
dotnet run
```

### Installer
```
- Size: 150-200MB (with .NET 8)
- Format: Single .exe installer (Inno Setup)
- Contents: Service + Web Portal + Config Tool + Runtime
- Target: Windows Server 2019+ (no .NET required)
```

---

## 📦 Next Steps

1. **Create** `build/publish-selfcontained.ps1` script
2. **Test** self-contained publishing
3. **Update** Inno Setup script
4. **Build** installer
5. **Test** on clean Windows Server
6. **Document** deployment process

---

**Critical for DMZ/OT:** Self-contained deployment is **mandatory**, not optional.


