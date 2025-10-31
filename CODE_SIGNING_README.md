# Code Signing Setup Complete! ✅

**ZL File Relay now has complete code signing infrastructure** based on your SrtExtractor reference.

---

## 🎯 What's Been Done

### ✅ New Build Scripts (3 files)

1. **`build/build-app.ps1`** - Builds all components (Service, WebPortal, ConfigTool)
2. **`build/sign-app.ps1`** - Signs all executables and main DLL
3. **`build/release.ps1`** - Complete release orchestration

### ✅ Updated Files (3 files)

1. **`installer/ZLFileRelay.iss`** - Added SignTool configuration
2. **`build/README.md`** - Added comprehensive documentation
3. **`installer/README.md`** - Added code signing section

### ✅ New Documentation (3 files)

1. **`docs/CODE_SIGNING.md`** - Complete guide (prerequisites, workflows, troubleshooting)
2. **`docs/CODE_SIGNING_SETUP_COMPLETE.md`** - Setup summary
3. **`build/QUICK_REFERENCE.md`** - One-page cheat sheet

---

## 🚀 Quick Start (3 Steps)

### Step 1: Get Your Certificate Thumbprint

```powershell
# List your code signing certificates
Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert
```

Copy the **Thumbprint** (40-character hex string).

### Step 2: Set Environment Variable

```powershell
# Set permanently (recommended)
[System.Environment]::SetEnvironmentVariable(
    "CODESIGN_CERT_SHA1",
    "YOUR_THUMBPRINT_HERE",
    "User"
)

# Then restart PowerShell
```

### Step 3: Test It

```powershell
cd build

# Build all components
.\build-app.ps1

# Sign them
.\sign-app.ps1

# Should see: ✅ CODE SIGNING COMPLETE - ALL FILES SIGNED
```

---

## 📦 What Gets Signed

| Component | File | Purpose |
|-----------|------|---------|
| **Service** | `ZLFileRelay.Service.exe` | Windows Service |
| **WebPortal** | `ZLFileRelay.WebPortal.exe` | Web Application |
| **ConfigTool** | `ZLFileRelay.ConfigTool.exe` | WPF GUI |
| **Core Library** | `ZLFileRelay.Core.dll` | Shared DLL |
| **Installer** | `ZLFileRelaySetup-*.exe` | Via Inno Setup |

---

## 🎯 Your Release Workflow

### Complete Release (Recommended)

```powershell
cd build
.\release.ps1 -Version "1.0.0" -Sign
```

This single command:
1. ✅ Reminds you to update version numbers
2. ✅ Builds all components
3. ✅ Signs executables
4. ✅ Publishes self-contained
5. ✅ Prompts to build installer
6. ✅ Creates checksums
7. ✅ Opens output folder

### Manual Workflow

```powershell
# 1. Update versions in .csproj files and ZLFileRelay.iss

# 2. Build
cd build
.\build-app.ps1

# 3. Sign
.\sign-app.ps1

# 4. Publish
.\publish-selfcontained.ps1

# 5. Build installer (you'll sign it via Inno Setup)
# Open installer/ZLFileRelay.iss and compile
```

---

## 🔐 Signing the Installer

### One-Time Setup in Inno Setup

1. **Open Inno Setup IDE**
2. **Go to:** Tools → Configure Sign Tools...
3. **Click:** Add
4. **Name:** `signtool`
5. **Command:**
```
"C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe" sign /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com /sha1 YOUR_CERT_THUMBPRINT /d "ZL File Relay Setup" /du "https://github.com/ZentrixLabs/ZLFileRelay" $f
```
   - Replace `YOUR_CERT_THUMBPRINT` with your actual thumbprint
   - Adjust Windows SDK path if different

6. **Enable in script:**
   - Edit `installer/ZLFileRelay.iss`
   - Find the `[Setup]` section
   - Uncomment: `SignTool=signtool`

Now your installer will auto-sign when you compile it!

---

## 📚 Documentation

| Document | What It's For |
|----------|---------------|
| **`build/QUICK_REFERENCE.md`** | Quick commands cheat sheet |
| **`docs/CODE_SIGNING.md`** | Complete guide (read first!) |
| **`docs/CODE_SIGNING_SETUP_COMPLETE.md`** | Setup walkthrough |
| **`build/README.md`** | Build scripts documentation |
| **`installer/README.md`** | Installer + signing docs |

---

## ✅ Verification

### Check if files are signed:

```powershell
# Check one file
Get-AuthenticodeSignature "src\ZLFileRelay.Service\bin\Release\net8.0\ZLFileRelay.Service.exe"

# Check all
Get-AuthenticodeSignature "src\*\bin\Release\**\*.exe"

# Should show: Status = Valid
```

---

## 🆘 Troubleshooting

| Problem | Solution |
|---------|----------|
| **signtool not found** | Install Windows SDK |
| **Certificate not found** | Check thumbprint is correct |
| **Timestamp failed** | Retry, or use different server |
| **Access denied** | Run PowerShell as Admin |

See **`docs/CODE_SIGNING.md`** for detailed troubleshooting.

---

## 🎉 You're Ready!

Everything is set up. You can now:

✅ Build with code signing  
✅ Create signed releases  
✅ Sign your installer  
✅ Distribute with confidence  

### Next Steps:

1. **Set your certificate thumbprint** (see Step 1 above)
2. **Test the signing workflow** (see Step 3 above)
3. **Configure Inno Setup signing** (see Signing the Installer above)
4. **Run a test release**: `.\release.ps1 -Version "1.0.0-test" -Sign`
5. **Read** `docs/CODE_SIGNING.md` for full details

---

## 📖 Quick Command Reference

```powershell
# Build all
.\build-app.ps1

# Sign all
.\sign-app.ps1

# Complete release
.\release.ps1 -Version "1.0.0" -Sign

# Check signatures
Get-AuthenticodeSignature src\*\bin\Release\**\*.exe
```

---

**Questions?** See `docs/CODE_SIGNING.md` for comprehensive documentation.

**Ready to sign!** 🚀✍️

