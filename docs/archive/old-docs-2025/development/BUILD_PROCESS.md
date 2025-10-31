# Build Process Guide

## Overview

The ZL File Relay project uses a two-stage build process:

1. **Publish Stage** - Builds all components with .NET 8 runtime included (self-contained)
2. **Installer Stage** - Packages everything into an Inno Setup installer

## Directory Structure

```
ZLFileRelay/
├── build/                              # Build scripts
│   ├── publish-selfcontained.ps1      # Stage 1: Publish all components
│   └── build-installer.ps1            # Stage 2: Create installer (calls publish)
│
├── publish/                            # Published binaries (output of stage 1)
│   ├── Service/                       # Windows Service binaries
│   ├── WebPortal/                     # Web Portal binaries
│   ├── ConfigTool/                    # Config Tool binaries
│   └── docs/                          # Documentation
│
└── installer/                          # Installer scripts and assets
    ├── ZLFileRelay.iss                # Inno Setup installer script
    ├── assets/                         # Icons and images
    └── output/                         # Final installer (output of stage 2)
        └── ZLFileRelay-Setup-v1.0.0-SelfContained.exe
```

## Build Workflow

### Stage 1: Publish Components

**Script**: `build/publish-selfcontained.ps1`

**What it does**:
- Publishes Service with .NET 8 runtime → `publish/Service/`
- Publishes WebPortal with .NET 8 runtime → `publish/WebPortal/`
- Publishes ConfigTool as single-file → `publish/ConfigTool/`
- Copies configuration template to `publish/appsettings.json`
- Copies essential user documentation to `publish/docs/` (excludes development & archive folders)

**Run manually**:
```powershell
.\build\publish-selfcontained.ps1
```

**Parameters**:
- `-Configuration` - Build configuration (default: "Release")
- `-Runtime` - Target runtime (default: "win-x64")

### Stage 2: Create Installer

**Script**: `build/build-installer.ps1`

**What it does**:
1. Calls `publish-selfcontained.ps1` to ensure binaries are up-to-date
2. Runs Inno Setup compiler on `installer/ZLFileRelay.iss`
3. Creates installer in `installer/output/`
4. Shows build summary and opens output folder

**Run manually**:
```powershell
.\build\build-installer.ps1
```

**Parameters**:
- `-Configuration` - Build configuration (default: "Release")
- `-Runtime` - Target runtime (default: "win-x64")
- `-InnoSetupPath` - Path to Inno Setup compiler (default: "C:\Program Files (x86)\Inno Setup 6\ISCC.exe")

## Recommended Build Commands

### Quick Build (Recommended)

Build everything in one command:

```powershell
cd build
.\build-installer.ps1
```

This will:
1. pretty-clean publish folder
2. Build all components
3. Create installer
4. Show you where the installer is

### Development Build

Just build the projects without creating installer:

```powershell
dotnet build --configuration Release
```

Or publish individual components:

```powershell
dotnet publish src/ZLFileRelay.Service/ZLFileRelay.Service.csproj -c Release -r win-x64 --self-contained true
dotnet publish src/ZLFileRelay.WebPortal/ZLFileRelay.WebPortal.csproj -c Release -r win-x64 --self-contained true
dotnet publish src/ZLFileRelay.ConfigTool/ZLFileRelay.ConfigTool.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Custom Configuration

Build with different settings:

```powershell
.\build\build-installer.ps1 -Configuration Debug -Runtime win-x64
```

## Prerequisites

### Required Software

1. **.NET 8.0 SDK** - For building projects
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify: `dotnet --version` (should be 8.x.x)

2. **Inno Setup 6.x** - For creating installer
   - Download: https://jrsoftware.org/isinfo.php
   - Default location: `C:\Program Files (x86)\Inno Setup 6\`
   - Verify: Build script checks automatically

### System Requirements

- Windows 10+ (for building)
- PowerShell 7+ (or Windows PowerShell 5.1)
- Administrative privileges (recommended, for service installation testing)

## Build Output

### Installer File

- **Location**: `installer/output/ZLFileRelay-Setup-v1.0.0-SelfContained.exe`
- **Size**: ~150-200 MB (includes .NET 8 runtime)
- **Target**: Self-contained deployment (no .NET installation required)
- **Compatible**: DMZ/OT/air-gapped environments

### Published Components

After running build script, you'll have:

```
publish/
├── Service/                    (~70 MB with .NET 8)
│   ├── ZLFileRelay.Service.exe
│   ├── *.dll
│   └── runtimes/
│
├── WebPortal/                  (~75 MB with .NET 8)
│   ├── ZLFileRelay.WebPortal.exe
│   ├── *.dll
│   └── runtimes/
│
├── ConfigTool/                 (~50 MB, single file)
│   ├── ZLFileRelay.ConfigTool.exe
│   └── runtimes/
│
├── appsettings.json            (Configuration template)
└── docs/                       (Documentation)
```

## Development Workflow

### 1. Make Code Changes

Edit source files in `src/` directory

### 2. Test Changes

Run individual components for testing:

```powershell
# Run ConfigTool
dotnet run --project src/ZLFileRelay.ConfigTool

# Run WebPortal
dotnet run --project src/ZLFileRelay.WebPortal

# Build Service (requires service registration)
dotnet build src/ZLFileRelay.Service
```

### 3. Build Installer

When ready to create installer:

```powershell
.\build\build-installer.ps1
```

### 4. Test Installer

Install on clean VM or test machine to verify complete workflow

### 5. Deploy

Distribute `installer/output/ZLFileRelay-Setup-v1.0.0-SelfContained.exe`

## Troubleshooting

### Build Fails with "Inno Setup not found"

1. Install Inno Setup from https://jrsoftware.org/isinfo.php
2. Or specify custom path:
   ```powershell
   .\build\build-installer.ps1 -InnoSetupPath "C:\Your\Path\To\ISCC.exe"
   ```

### Build Fails with ".NET SDK not found"

1. Install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
2. Verify installation: `dotnet --version`

### Publish Folder Has Old Files

The build script automatically cleans the publish folder. If issues persist:

```powershell
Remove-Item publish -Recurse -Force
.\build\build-installer.ps1
```

### Installer Too Large

The installer includes .NET 8 runtime (~150MB). This is intentional for:
- Air-gapped/DMZ deployments
- Environments without .NET installed
- Self-contained deployments

If smaller size is critical, consider:
- Framework-dependent deployment (requires .NET on target)
- Separate installer for different components

### "Cannot Start Service" Error

This is expected during development. The service can only be installed on Windows with admin privileges. Use ConfigTool for service management.

## CI/CD Integration

For automated builds, use the build script in your pipeline:

```yaml
# Azure DevOps example
- task: PowerShell@2
  displayName: 'Build Installer'
  inputs:
    filePath: 'build/build-installer.ps1'
    arguments: '-Configuration Release -Runtime win-x64'
    pwsh: true
```

## Version Management

To update version numbers:

1. **Inno Setup Script** - Edit `installer/ZLFileRelay.iss`:
   ```iss
   #define MyAppVersion "1.0.1"
   ```

2. **Assembly Info** - Update `.csproj` files:
   ```xml
   <Version>1.0.1</Version>
   ```

3. **Installation Directory** - Output will be:
   ```
   installer/output/ZLFileRelay-Setup-v1.0.1-SelfContained.exe
   ```

## Summary

**For most users**: Just run `.\build\build-installer.ps1` and you're done!

The script handles:
- ✅ Cleaning old builds
- ✅ Publishing all components
- ✅ Creating installer
- ✅ Showing you where it is
- ✅ Opening the folder

You just need .NET 8 SDK and Inno Setup installed.

