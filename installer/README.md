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

## ✍️ Code Signing (Optional but Recommended)

### Setup Code Signing in Inno Setup

1. **Configure SignTool in Inno Setup IDE**
   - Open Inno Setup IDE
   - Go to **Tools → Configure Sign Tools...**
   - Click **Add**
   - Name: `signtool`
   - Command:
     ```
     "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe" sign /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com /sha1 YOUR_CERT_THUMBPRINT /d "ZL File Relay Setup" /du "https://github.com/ZentrixLabs/ZLFileRelay" $f
     ```
   - Replace `YOUR_CERT_THUMBPRINT` with your certificate thumbprint
   - Adjust Windows SDK version path if different

2. **Enable Signing in Inno Script**
   - Open `installer/ZLFileRelay.iss`
   - Find the `[Setup]` section
   - Uncomment this line: `SignTool=signtool`

3. **Build Signed Installer**
   ```powershell
   # Build and sign app components first
   .\build\build-app.ps1
   .\build\sign-app.ps1
   
   # Publish signed components
   .\build\publish-selfcontained.ps1
   
   # Build installer (will auto-sign if configured)
   iscc installer\ZLFileRelay.iss
   ```

### Finding Your Certificate Thumbprint

```powershell
# List code signing certificates
Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert

# Copy the Thumbprint (40-character hex string)
```

### Verify Installer Signature

```powershell
# Using signtool
signtool verify /pa installer\output\ZLFileRelay-Setup-*.exe

# Or right-click installer → Properties → Digital Signatures tab
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
4. ✅ **Optionally** installs File Transfer Windows Service
5. ✅ **Optionally** installs Web Portal Windows Service (Kestrel - **NO IIS REQUIRED!**)
6. ✅ **Optionally** configures Windows Firewall (port 8080)
7. ✅ Creates Start Menu shortcuts
8. ✅ **Optionally** creates desktop shortcut

---

## ✅ No .NET or IIS Installation Required!

This is a **self-contained installer** that includes:
- ✅ .NET 8 Runtime
- ✅ ASP.NET Core 8 Runtime  
- ✅ All dependencies
- ✅ Built-in Kestrel web server
- ✅ SQLite database for local authentication

**Target machines need:**
- ✅ Windows Server 2019+ (or Windows 10/11)
- ✅ 500MB disk space
- ❌ **NO .NET framework required!**
- ❌ **NO IIS required!** (Web portal runs as Windows Service)
- ❌ **NO internet connection required!** (Local authentication works offline)
- ❌ **NO Active Directory required!** (Role-based auth with local accounts)

---

## 🔒 DMZ/OT Deployment

Perfect for:
- Air-gapped networks (local authentication, no cloud dependencies)
- DMZ environments (works across domains without trust)
- OT/SCADA networks (isolated operational technology)
- Restricted security zones (classified environments)
- Offline installations (no internet required for local auth)

**Process:**
1. Build installer on development machine
2. Copy `ZLFileRelay-Setup-v1.0.0-SelfContained.exe` to USB/approved transfer
3. Run installer on target (no internet needed)
4. Configure via Config Tool
   - Enable **Local Authentication** for air-gapped deployment
   - (Optional) Configure **Entra ID** for cloud-connected deployments
5. Done!

---

## 🎯 File Structure

```
C:\Program Files\ZLFileRelay\
├── Service\                    (~70MB - with .NET 8)
│   ├── ZLFileRelay.Service.exe
│   ├── *.dll (including .NET runtime)
│   └── ...
├── WebPortal\                  (~75MB - with ASP.NET Core 8 + Kestrel)
│   ├── ZLFileRelay.WebPortal.exe (runs as Windows Service!)
│   ├── wwwroot\
│   ├── *.dll (including ASP.NET Core runtime)
│   └── ...
├── ConfigTool\                 (~65MB - single file)
│   └── ZLFileRelay.ConfigTool.exe (includes .NET 8)
└── docs\
    ├── README.md
    ├── getting-started\
    ├── configuration\
    ├── deployment\
    └── ...

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


## 📝 Testing Checklist

Before distributing installer:

- [ ] Test on Windows Server 2019 (no .NET installed)
- [ ] Test on Windows Server 2022 (no .NET installed)
- [ ] Test on Windows Server Core (headless)
- [ ] Test on air-gapped VM (no internet)
- [ ] Verify File Transfer Service installs and starts
- [ ] Verify Web Portal Service installs and starts (port 8080)
- [ ] Verify web portal accessible via browser (no IIS needed)
- [ ] Test local authentication (air-gapped mode)
  - [ ] User registration works
  - [ ] User login works
  - [ ] Admin approval workflow works (if enabled)
- [ ] Test Entra ID authentication (if cloud-connected)
  - [ ] SSO works with Microsoft account
  - [ ] Configuration via wizard works
- [ ] Verify ConfigTool launches and configures auth
- [ ] Test complete file transfer workflow
- [ ] Test uninstaller (clean removal)
- [ ] Verify no .NET installation required
- [ ] Verify no IIS required for web portal
- [ ] Verify no internet required for local authentication
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


