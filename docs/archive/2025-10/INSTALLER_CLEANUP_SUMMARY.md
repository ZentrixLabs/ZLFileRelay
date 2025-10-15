# Installer Cleanup Summary - October 2025

## Overview
Removed obsolete IIS configuration scripts from the installer package, since ZL File Relay now runs the web portal as a self-hosted Windows Service using Kestrel instead of requiring IIS.

## What Was Removed

### Deleted Files
1. **installer/scripts/Configure-IIS.ps1** - Script to configure IIS application pool and website
2. **installer/scripts/Remove-IIS.ps1** - Script to remove IIS configuration during uninstall
3. **installer/scripts/** - Entire scripts directory (now empty)

### Why These Were Obsolete
The original design used IIS to host the ASP.NET Core web portal. The current architecture runs the web portal as a **Windows Service** using the built-in Kestrel web server, eliminating the IIS dependency entirely.

## Inno Setup Changes

### Updated `installer/ZLFileRelay.iss`

**Removed:**
```iss
; PowerShell Helper Scripts
Source: "scripts\*"; DestDir: "{app}\scripts"; Flags: ignoreversion recursesubdirs

Name: "{app}\scripts"
```

**Reason:** No scripts to include anymore

### What Remained Correct
The Inno Setup script was **already correctly configured** for no-IIS deployment:
- Web portal installs as Windows Service: `ZLFileRelay.WebPortal`
- Runs on port 8080 via Kestrel
- No IIS installation or configuration required
- Includes firewall configuration for port 8080

## Installer README Updates

### Enhanced Clarity
Updated `installer/README.md` to emphasize:

**Before:**
- "Optionally configures IIS for web portal"
- Listed IIS scripts in file structure

**After:**
- "Optionally installs Web Portal Windows Service (Kestrel - **NO IIS REQUIRED!**)"
- "NO IIS required! (Web portal runs as Windows Service)"
- Removed scripts directory from file structure
- Updated testing checklist to verify "no IIS required"

### New Section Header
Changed from:
```markdown
## ✅ No .NET Installation Required!
```

To:
```markdown
## ✅ No .NET or IIS Installation Required!
```

### Updated File Structure Documentation
Removed obsolete scripts directory, updated to show current documentation structure:
```
C:\Program Files\ZLFileRelay\
├── Service\                    (~70MB - with .NET 8)
├── WebPortal\                  (~75MB - with ASP.NET Core 8 + Kestrel)
│   └── ZLFileRelay.WebPortal.exe (runs as Windows Service!)
├── ConfigTool\                 (~65MB - single file)
└── docs\
    ├── getting-started\
    ├── configuration\
    ├── deployment\
    └── ...
```

### Updated Testing Checklist
Added specific tests for Windows Service deployment:
- [ ] Test on Windows Server Core (headless)
- [ ] Verify Web Portal Service installs and starts (port 8080)
- [ ] Verify web portal accessible via browser (no IIS needed)
- [ ] Verify no IIS required for web portal

## Benefits of This Cleanup

✅ **Simpler Deployment** - No IIS configuration needed  
✅ **Server Core Compatible** - Works on headless Windows Server Core  
✅ **DMZ/OT Friendly** - One less service to manage and secure  
✅ **Consistent Architecture** - Both components run as Windows Services  
✅ **Less Confusion** - No outdated scripts in the installer  
✅ **Easier Troubleshooting** - Fewer moving parts  

## Architecture Clarification

### Current (Correct) Architecture
```
┌─────────────────────────────────────┐
│  ZL File Relay                       │
├─────────────────────────────────────┤
│  ┌────────────────────────────────┐ │
│  │  File Transfer Service         │ │
│  │  (Windows Service)             │ │
│  └────────────────────────────────┘ │
│                                      │
│  ┌────────────────────────────────┐ │
│  │  Web Portal                    │ │
│  │  (Windows Service - Kestrel)   │ │
│  │  Port: 8080                    │ │
│  └────────────────────────────────┘ │
│                                      │
│  ┌────────────────────────────────┐ │
│  │  ConfigTool                    │ │
│  │  (WPF Desktop App)             │ │
│  └────────────────────────────────┘ │
└─────────────────────────────────────┘
```

**NO IIS REQUIRED!**

### Legacy Architecture (Removed)
The old scripts were for this obsolete pattern:
```
┌────────────────┐
│  IIS           │
│  ├─ App Pool   │
│  └─ Website    │
│     └─ Web Portal (hosted in IIS)
└────────────────┘
```

## Impact on Deployment Guides

The deployment documentation still references IIS in some places, but these are **correct** because:

1. **Production Coexistence Guide** - Compares OLD system (uses IIS) vs NEW system (no IIS)
2. **Side-by-Side Testing** - Shows how to run alongside existing IIS-based systems
3. **DMZ Deployment** - May mention IIS as legacy comparison

These references are intentional and help users understand the architectural improvement.

## Files Modified

1. `installer/ZLFileRelay.iss` - Removed scripts inclusion
2. `installer/README.md` - Updated to reflect no-IIS architecture
3. `installer/scripts/Configure-IIS.ps1` - Deleted
4. `installer/scripts/Remove-IIS.ps1` - Deleted
5. `installer/scripts/` - Directory removed

## Verification

After cleanup:
```
installer/
├── README.md          ✅ Updated, no IIS references
└── ZLFileRelay.iss    ✅ Updated, no scripts
```

Clean, simple, correct! 🎉

## Key Takeaways

1. **Web Portal = Windows Service** (using Kestrel)
2. **No IIS dependency** (simplifies deployment)
3. **No IIS scripts needed** (nothing to configure)
4. **Perfect for DMZ/OT** (fewer components, less attack surface)
5. **Server Core compatible** (IIS not even available on Core)

## Testing Required

Before distributing updated installer, verify:
- [ ] Web portal installs as Windows Service
- [ ] Web portal starts on port 8080
- [ ] Web portal accessible via browser
- [ ] No IIS installation attempted
- [ ] Works on Windows Server Core
- [ ] Firewall rule created for port 8080
- [ ] Service runs under correct account
- [ ] Uninstaller removes service correctly

## Conclusion

The installer now accurately reflects the current architecture: a self-contained, Windows Service-based deployment that requires **no IIS, no .NET installation, and no internet connection**. Perfect for DMZ and OT environments.

This cleanup removes confusion and ensures the installer matches the actual deployment model.

**Before**: Installer had obsolete IIS scripts  
**After**: Clean installer with only relevant components  

Mission accomplished! 🚀

