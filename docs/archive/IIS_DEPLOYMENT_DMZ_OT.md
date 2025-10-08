# IIS Deployment for DMZ/OT Environments

**Challenge:** Air-gapped OT networks can't download ASP.NET Core Hosting Bundle from Microsoft

**Solutions:** Multiple approaches depending on your needs

---

## 🎯 The IIS + ASP.NET Core Situation

### What IIS Needs for ASP.NET Core

For **framework-dependent** apps:
- ❌ ASP.NET Core Runtime (~60MB download)
- ❌ ASP.NET Core Hosting Bundle
- ❌ Requires internet or manual install

For **self-contained** apps (what we're doing):
- ✅ App includes .NET runtime
- ⚠️ Still needs ASP.NET Core Module (ANCM) for IIS
- ✅ BUT can run without IIS!

---

## 🚀 Option 1: Standalone Kestrel (No IIS) ⭐ RECOMMENDED for DMZ/OT

### Run web portal as Windows Service (no IIS needed!)

**Advantages:**
- ✅ No IIS required
- ✅ No ASP.NET Core Hosting Bundle needed
- ✅ Truly zero dependencies
- ✅ Works on Windows Server Core
- ✅ Simpler deployment
- ✅ Easier troubleshooting

**Implementation:**

```csharp
// In WebPortal Program.cs - already supports this!
builder.WebHost.UseUrls("http://*:8080", "https://*:8443");

// Configure as Windows Service
if (OperatingSystem.IsWindows())
{
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "ZLFileRelay.WebPortal";
    });
}
```

**Deployment:**
```powershell
# Install web portal as Windows Service (alongside file transfer service)
sc create ZLFileRelay.WebPortal binPath="C:\Program Files\ZLFileRelay\WebPortal\ZLFileRelay.WebPortal.exe" start=auto
sc start ZLFileRelay.WebPortal

# Access at: http://servername:8080
```

**Pros:**
- ✅ Zero IIS dependency
- ✅ Perfect for DMZ/OT
- ✅ Built-in Windows Authentication support
- ✅ Runs on Server Core

**Cons:**
- ⚠️ Direct Kestrel exposure (use with firewall rules)
- ⚠️ No IIS features (URL rewrite, etc.) - usually not needed

---

## 📦 Option 2: Bundle ASP.NET Core Hosting Bundle

### Include the installer in your package

**What to include:**
- File: `dotnet-hosting-8.0.x-win.exe` (~60MB)
- Download from: https://dotnet.microsoft.com/download/dotnet/8.0
- Version: Match your .NET 8 SDK version

**Inno Setup Configuration:**

```inno
[Files]
; Include ASP.NET Core Hosting Bundle for IIS
Source: "dependencies\dotnet-hosting-8.0.10-win.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Run]
; Install ASP.NET Core Hosting Bundle (for IIS)
Filename: "{tmp}\dotnet-hosting-8.0.10-win.exe"; Parameters: "/quiet /norestart"; StatusMsg: "Installing ASP.NET Core Module for IIS..."; Components: webportal; Check: NeedsHostingBundle

[Code]
function NeedsHostingBundle(): Boolean;
var
  Version: String;
begin
  // Check if ASP.NET Core Module is already installed
  Result := not RegQueryStringValue(HKLM, 
    'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost', 
    'Version', Version);
end;
```

**Installer Size:** ~210MB total (~150MB app + ~60MB hosting bundle)

---

## 🔧 Option 3: Extract ANCM Module Only

### Include just the ASP.NET Core Module files

**What to extract from Hosting Bundle:**
```
- aspnetcorev2.dll
- aspnetcorev2_inprocess.dll  
- aspnetcorev2_outofprocess.dll
```

**Inno Setup:**
```inno
[Files]
; ASP.NET Core Module (extracted from Hosting Bundle)
Source: "dependencies\ancm\aspnetcorev2.dll"; DestDir: "{sys}\inetsrv"; Components: webportal; Flags: restartreplace

[Registry]
; Register ANCM module in IIS
Root: HKLM; Subkey: "SOFTWARE\Microsoft\InetStp\Components"; ValueType: dword; ValueName: "AspNetCoreModule"; ValueData: 1
```

**Pros:** Smaller (only ~5MB instead of 60MB)  
**Cons:** More complex, may miss dependencies

---

## 💡 RECOMMENDATION for DMZ/OT

### Use **Option 1: Standalone Kestrel** (No IIS)

**Why:**
1. ✅ **Truly zero dependencies** - No IIS, no ANCM needed
2. ✅ **Simpler** - One less thing to configure
3. ✅ **More reliable** - Direct process, easier to troubleshoot
4. ✅ **Works everywhere** - Including Server Core
5. ✅ **Smaller installer** - No hosting bundle needed

### Updated Architecture

```
┌─────────────────────────────────────────────────┐
│  Windows Server (DMZ/OT - Air-Gapped)          │
├─────────────────────────────────────────────────┤
│                                                 │
│  Windows Service #1: ZLFileRelay               │
│    - File watching                              │
│    - SSH/SMB transfer                           │
│    - Port: N/A (file-based)                     │
│                                                 │
│  Windows Service #2: ZLFileRelay.WebPortal     │
│    - Web upload interface                       │
│    - Kestrel web server                         │
│    - Port: 8080 (HTTP) / 8443 (HTTPS)          │
│    - Windows Authentication                     │
│                                                 │
│  Desktop App: ZLFileRelay.ConfigTool           │
│    - Configuration GUI                          │
│    - Service management                         │
│                                                 │
└─────────────────────────────────────────────────┘

Total: 2 Windows Services + 1 GUI
No IIS Required!
No ASP.NET Core Hosting Bundle Required!
Truly Self-Contained!
```

---

## 🔄 Comparison Matrix

| Approach | IIS Needed? | Extra Downloads | Installer Size | Complexity | OT Compatible |
|----------|-------------|-----------------|----------------|------------|---------------|
| **Kestrel Service** | ❌ No | None | ~150MB | Low | ✅ ✅ ✅ |
| IIS + Bundle | ✅ Yes | 60MB bundle | ~210MB | Medium | ✅ ✅ |
| IIS + Extracted ANCM | ✅ Yes | Extract files | ~155MB | High | ✅ |

---

## 🛠️ Implementation: Kestrel as Windows Service

### 1. Update WebPortal to Support Windows Service

Already done! Just need to configure URLs:

```csharp
// src/ZLFileRelay.WebPortal/Program.cs
builder.WebHost.UseUrls("http://*:8080");

// Add Windows Service support
if (OperatingSystem.IsWindows())
{
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "ZLFileRelay.WebPortal";
    });
}
```

### 2. Update Installer to Install Two Services

```inno
[Run]
; Install File Transfer Service
Filename: "sc.exe"; Parameters: "create ZLFileRelay binPath=""{app}\Service\ZLFileRelay.Service.exe"" start=auto DisplayName=""ZL File Relay - File Transfer"""; StatusMsg: "Installing File Transfer Service..."; Flags: runhidden; Components: service

; Install Web Portal Service (NO IIS NEEDED!)
Filename: "sc.exe"; Parameters: "create ZLFileRelay.WebPortal binPath=""{app}\WebPortal\ZLFileRelay.WebPortal.exe --urls http://*:8080"" start=auto DisplayName=""ZL File Relay - Web Portal"""; StatusMsg: "Installing Web Portal Service..."; Flags: runhidden; Components: webportal

; Start services
Filename: "sc.exe"; Parameters: "start ZLFileRelay"; Flags: runhidden; Components: service
Filename: "sc.exe"; Parameters: "start ZLFileRelay.WebPortal"; Flags: runhidden; Components: webportal

[UninstallRun]
; Stop and remove both services
Filename: "sc.exe"; Parameters: "stop ZLFileRelay"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete ZLFileRelay"; Flags: runhidden
Filename: "sc.exe"; Parameters: "stop ZLFileRelay.WebPortal"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete ZLFileRelay.WebPortal"; Flags: runhidden
```

### 3. Configure Firewall

```powershell
# Allow inbound on port 8080
New-NetFirewallRule -DisplayName "ZL File Relay Web Portal" `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort 8080 `
    -Action Allow
```

---

## 🎯 Final Recommendation

### For Maximum DMZ/OT Compatibility

**Use Standalone Kestrel (No IIS):**
- ✅ Installer size: ~150MB (no hosting bundle)
- ✅ Zero IIS dependency
- ✅ Works on Server Core
- ✅ Simpler deployment
- ✅ Easier troubleshooting
- ✅ Two Windows Services (clean architecture)

**Benefits:**
- Users access at `http://server:8080/Upload`
- Windows Authentication still works
- All security features functional
- One less dependency to manage

**If you MUST use IIS:**
- Bundle the ASP.NET Core Hosting Bundle installer
- Add ~60MB to installer size
- Add installation step
- More complexity but works

---

## 📋 Next Steps

**I recommend:**
1. Update WebPortal to run as Windows Service (Kestrel)
2. Update installer to install both services
3. Skip IIS completely for DMZ/OT deployments
4. Simpler, cleaner, more reliable!

Want me to implement the Kestrel Windows Service approach?


