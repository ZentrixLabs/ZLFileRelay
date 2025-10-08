# ZL File Relay - Installer

**Type:** Self-Contained with .NET 8 Runtime  
**Tool:** Inno Setup 6.x  
**Size:** ~150MB (includes .NET 8)  
**Target:** Windows Server 2019+ (DMZ/OT compatible)

---

## 📦 Building the Installer

### Prerequisites
1. **Inno Setup 6.x** - Download from https://jrsoftware.org/isinfo.php
2. **Published Applications** - Run publish script first

### Steps

```powershell
# 1. Publish all components (self-contained with .NET 8)
.\build\publish-selfcontained.ps1

# 2. Build installer with Inno Setup
iscc installer\ZLFileRelay.iss

# Output: installer\output\ZLFileRelay-Setup-v1.0.0-SelfContained.exe
```

---

## 📊 Installer Details

### What's Included
- **File Transfer Service** + .NET 8 Runtime (~70MB)
- **Web Upload Portal** + ASP.NET Core 8 Runtime (~75MB)
- **Configuration Tool** + .NET 8 Runtime (~65MB single file)
- **Documentation**
- **Configuration Templates**
- **PowerShell Helper Scripts**

### Installation Options
- **Full** - All components (recommended)
- **Service Only** - Just file transfer service
- **Custom** - Choose components

### What Happens During Install
1. ✅ Copies all files to `C:\Program Files\ZLFileRelay`
2. ✅ Creates data directories (`C:\FileRelay\*`)
3. ✅ Copies config to `C:\ProgramData\ZLFileRelay`
4. ✅ **Optionally** installs Windows Service
5. ✅ **Optionally** configures IIS for web portal
6. ✅ Creates Start Menu shortcuts
7. ✅ **Optionally** creates desktop shortcut

---

## ✅ No .NET Installation Required!

This is a **self-contained installer** that includes:
- ✅ .NET 8 Runtime
- ✅ ASP.NET Core 8 Runtime  
- ✅ All dependencies

**Target machines need:**
- ✅ Windows Server 2019+ (or Windows 10/11)
- ✅ 500MB disk space
- ❌ **NO .NET framework required!**
- ❌ **NO internet connection required!**

---

## 🔒 DMZ/OT Deployment

Perfect for:
- Air-gapped networks
- DMZ environments
- OT/SCADA networks
- Restricted security zones
- Offline installations

**Process:**
1. Build installer on development machine
2. Copy `ZLFileRelay-Setup-v1.0.0-SelfContained.exe` to USB/approved transfer
3. Run installer on target (no internet needed)
4. Configure via Config Tool
5. Done!

---

## 🎯 File Structure

```
C:\Program Files\ZLFileRelay\
├── Service\                    (~70MB - with .NET 8)
│   ├── ZLFileRelay.Service.exe
│   ├── *.dll (including .NET runtime)
│   └── ...
├── WebPortal\                  (~75MB - with ASP.NET Core 8)
│   ├── ZLFileRelay.WebPortal.exe
│   ├── web.config
│   ├── wwwroot\
│   ├── *.dll (including ASP.NET Core runtime)
│   └── ...
├── ConfigTool\                 (~65MB - single file)
│   └── ZLFileRelay.ConfigTool.exe (includes .NET 8)
├── docs\
│   ├── README.md
│   ├── INSTALLATION.md
│   ├── CONFIGURATION.md
│   └── ...
└── scripts\
    ├── Configure-IIS.ps1
    └── Remove-IIS.ps1

C:\ProgramData\ZLFileRelay\
└── appsettings.json

C:\FileRelay\
├── uploads\
│   └── transfer\
├── logs\
├── archive\
└── temp\
```

---

## 🛠️ Assets Needed

Create these files in `installer/assets/`:

1. **icon.ico** - Application icon (256x256)
2. **WizardImage.bmp** - Large wizard image (164x314)
3. **WizardSmallImage.bmp** - Small wizard image (55x58)

**Tip:** Use your company branding for professional appearance

---

## 📝 Testing Checklist

Before distributing installer:

- [ ] Test on Windows Server 2019 (no .NET installed)
- [ ] Test on Windows Server 2022 (no .NET installed)
- [ ] Test on air-gapped VM
- [ ] Verify service installs and starts
- [ ] Verify web portal works in IIS
- [ ] Verify config tool launches
- [ ] Test complete file transfer workflow
- [ ] Test uninstaller (clean removal)
- [ ] Verify no .NET installation required
- [ ] Check installer size acceptable (~150MB)

---

## 🚀 Distribution

### Internal Distribution
- Share via network drive
- USB distribution
- Internal software repository

### External Distribution
- Direct download (if public)
- Customer portal
- Email to customers (if < 200MB)

---

**Installer Script:** `installer/ZLFileRelay.iss`  
**Build Command:** `iscc installer\ZLFileRelay.iss`  
**Output:** `installer\output\ZLFileRelay-Setup-v1.0.0-SelfContained.exe`


