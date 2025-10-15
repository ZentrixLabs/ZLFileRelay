# ZL File Relay - Deployment Quick Reference

## 🎯 For DMZ/OT/Air-Gapped Environments

**CRITICAL:** Use self-contained deployment with .NET 8 included!

---

## 🚀 Quick Build for Installer

```powershell
# Run the self-contained publish script
.\build\publish-selfcontained.ps1

# Result: publish/ folder with all components + .NET 8 runtime
# Size: ~150-200MB total
# No .NET installation required on target!
```

---

## 📦 What Gets Included

| Component | Size | Contents |
|-----------|------|----------|
| **Service** | ~70MB | File Transfer Service + .NET 8 Runtime |
| **Web Portal** | ~75MB | Web Upload Portal + ASP.NET Core 8 Runtime |
| **Config Tool** | ~65MB | Configuration GUI + .NET 8 Runtime (single .exe) |
| **TOTAL** | ~200MB | Everything needed to run on clean Windows |

**Note:** Inno Setup compresses this to ~150MB installer

---

## ✅ Deployment Workflow

### 1. Build Self-Contained Packages
```powershell
.\build\publish-selfcontained.ps1
```

### 2. (Optional) Security Scan
Submit `publish/` folder contents to security team for scanning

### 3. Create Installer
```powershell
# Use Inno Setup to create single .exe installer
# Points to publish/ folder
# Output: ZLFileRelay-Setup-1.0.0-SelfContained.exe (~150MB)
```

### 4. Deploy to DMZ/OT
```
- Copy installer to target (USB, approved transfer, etc.)
- Run installer
- Configure via Config Tool or appsettings.json
- No internet needed!
```

---

## 🔒 DMZ/OT Requirements Met

✅ **No Internet Required** - .NET 8 runtime included  
✅ **No Framework Dependencies** - Everything bundled  
✅ **Single Installer** - One .exe file  
✅ **Air-Gap Compatible** - Works completely offline  
✅ **Security Scannable** - All files in publish/ folder  
✅ **Consistent Runtime** - Same .NET version everywhere  
✅ **No Registry Dependencies** - Clean install/uninstall  

---

## 📋 Deployment Checklist

**Before deploying to DMZ/OT:**
- [ ] Run `.\build\publish-selfcontained.ps1`
- [ ] Test on clean Windows Server (no .NET)
- [ ] Test on air-gapped VM
- [ ] Submit for security scanning if required
- [ ] Create Inno Setup installer
- [ ] Test installer on clean machine
- [ ] Document target environment requirements
- [ ] Get security/change management approval

---

## 💻 Minimum Requirements (Target System)

- **OS:** Windows Server 2019 or later (2022 recommended)
- **Architecture:** 64-bit (x64)
- **.NET:** **NOT REQUIRED** (included in installer!)
- **Disk Space:** 500MB free
- **RAM:** 2GB minimum, 4GB recommended
- **IIS:** Required for Web Portal (not for Service)

---

## 🎯 Key Commands

```powershell
# Publish self-contained (includes .NET 8)
.\build\publish-selfcontained.ps1

# Test Service locally
publish\Service\ZLFileRelay.Service.exe

# Test Web Portal locally  
publish\WebPortal\ZLFileRelay.WebPortal.exe

# Launch Config Tool
publish\ConfigTool\ZLFileRelay.ConfigTool.exe

# Build installer (requires Inno Setup)
iscc installer\ZLFileRelay.iss
```

---

## ✅ Verification

After creating self-contained build:

```powershell
# Check Service doesn't need .NET
publish\Service\ZLFileRelay.Service.exe --help

# Check Web Portal doesn't need .NET
publish\WebPortal\ZLFileRelay.WebPortal.exe --urls "http://localhost:5000"

# Check Config Tool doesn't need .NET
publish\ConfigTool\ZLFileRelay.ConfigTool.exe
```

All should run on a machine **WITHOUT** .NET 8 installed!

---

## 📊 Size Comparison

| Build Type | Installer Size | Target Environment |
|------------|----------------|-------------------|
| Framework-Dependent | ~15MB | ❌ DMZ/OT (needs .NET) |
| **Self-Contained** | **~150MB** | ✅ **DMZ/OT (includes .NET)** |

**For DMZ/OT: Always use Self-Contained!**

---

**Documentation:** See `docs/DEPLOYMENT_SELF_CONTAINED.md` for complete details.


