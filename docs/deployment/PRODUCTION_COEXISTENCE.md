# Production Coexistence Guide - DMZ Server

This guide provides **specific** instructions for running the new ZLFileRelay alongside your existing DMZFileTransferService/DMZUploader production system on your DMZ server.

## Current Production Setup (What NOT to Touch)

Based on your existing systems, here's what's currently running:

### Legacy Windows Service
- **Service Name**: `ZLBridge`
- **Event Log Source**: `DMZFileTransfer`
- **Watch Directory**: `E:\uploads\scada`
- **SSH Target**: `dfw-files-01.dfw.scada:2225`
- **SSH User**: `svc_dmz_transfer`
- **SSH Key**: `C:\Users\svc_dmz_transfer\.ssh\dmz_transfer.pem`

### Legacy Web Portal (DMZUploader)
- **IIS Site Name**: (likely `DMZUploader` or `Default Web Site`)
- **Port**: Likely 80 or 443
- **Upload Paths**: 
  - SCADA: `E:\uploads\scada`
  - DMZ: `E:\uploads\dmz`
  - PDQ: `E:\PDQRepository`
- **Logs**: `E:\uploads\logs`
- **Security Groups**: `DFWDMZ\AG_DMZUploader_Allowed`, `DFWDMZ\Admin_Ring_2`

## New ZLFileRelay Configuration (Safe Test Setup)

Here's your conflict-free configuration:

### Test Directory Structure

Create these new directories on your E: drive:

```powershell
# Run as Administrator
New-Item -ItemType Directory -Force -Path "E:\FileRelay-TEST\uploads"
New-Item -ItemType Directory -Force -Path "E:\FileRelay-TEST\uploads\transfer"
New-Item -ItemType Directory -Force -Path "E:\FileRelay-TEST\logs"
New-Item -ItemType Directory -Force -Path "C:\ProgramData\ZLFileRelay"
New-Item -ItemType Directory -Force -Path "C:\ProgramData\ZLFileRelay\ssh"

# Set permissions for IIS
$acl = Get-Acl "E:\FileRelay-TEST"
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS","FullControl","ContainerInherit,ObjectInherit","None","Allow")
$acl.SetAccessRule($rule)
Set-Acl "E:\FileRelay-TEST" $acl

# Set permissions for service account (if using svc_dmz_transfer for testing too)
$rule2 = New-Object System.Security.AccessControl.FileSystemAccessRule("DFWDMZ\svc_dmz_transfer","FullControl","ContainerInherit,ObjectInherit","None","Allow")
$acl.SetAccessRule($rule2)
Set-Acl "E:\FileRelay-TEST" $acl
```

### Configuration File

**Location**: `C:\ProgramData\ZLFileRelay\appsettings.json`

```json
{
  "ZLFileRelay": {
    "Branding": {
      "CompanyName": "MP Materials",
      "ProductName": "ZL File Relay (TEST)",
      "SiteName": "DFW Independence (TEST)",
      "SupportEmail": "help@mpmaterials.com",
      "LogoPath": "Assets/logo.png"
    },
    "Paths": {
      "UploadDirectory": "E:\\FileRelay-TEST\\uploads",
      "LogDirectory": "E:\\FileRelay-TEST\\logs",
      "ConfigDirectory": "C:\\ProgramData\\ZLFileRelay"
    },
    "Service": {
      "Enabled": true,
      "WatchDirectory": "E:\\FileRelay-TEST\\uploads\\transfer",
      "TransferMethod": "ssh",
      "RetryAttempts": 3,
      "RetryDelaySeconds": 30,
      "PollingIntervalSeconds": 10,
      "FileStabilitySeconds": 5,
      "DeleteSourceAfterTransfer": true
    },
    "WebPortal": {
      "Enabled": true,
      "RequireAuthentication": true,
      "AllowedGroups": [
        "DFWDMZ\\AG_DMZUploader_Allowed",
        "AG_DMZUploader_Allowed",
        "DFWDMZ\\Admin_Ring_2",
        "Admin_Ring_2"
      ],
      "MaxFileSizeBytes": 5368709120,
      "ShowUploadHistory": true
    },
    "Transfer": {
      "Ssh": {
        "Host": "dfw-files-01.dfw.scada",
        "Port": 2225,
        "Username": "svc_dmz_transfer",
        "PrivateKeyPath": "C:\\ProgramData\\ZLFileRelay\\ssh\\test_key.pem",
        "DestinationPath": "/test/uploads",
        "Compression": true,
        "ConnectionTimeoutSeconds": 30,
        "TransferTimeoutSeconds": 300,
        "StrictHostKeyChecking": true
      }
    },
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    }
  }
}
```

**Key Differences from Production:**
- Product name includes "(TEST)"
- Paths use `E:\FileRelay-TEST\` instead of `E:\uploads\`
- SSH destination uses `/test/uploads` instead of `F:\Transfer`
- Uses a separate SSH key for testing (optional, see below)

### SSH Key Options

You have two options for SSH testing:

#### Option A: Use Same Production Key (Quick Test)
```json
"PrivateKeyPath": "C:\\Users\\svc_dmz_transfer\\.ssh\\dmz_transfer.pem"
```
Just point to the existing production key. Files will go to `/test/uploads` on the target.

#### Option B: Create Separate Test Key (Isolated Test)
```powershell
# On the DMZ server, copy the production key to test location
Copy-Item "C:\Users\svc_dmz_transfer\.ssh\dmz_transfer.pem" -Destination "C:\ProgramData\ZLFileRelay\ssh\test_key.pem"

# Set permissions
$acl = Get-Acl "C:\ProgramData\ZLFileRelay\ssh\test_key.pem"
$acl.SetAccessRuleProtection($true, $false)
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("DFWDMZ\svc_dmz_transfer","FullControl","Allow")
$acl.SetAccessRule($rule)
Set-Acl "C:\ProgramData\ZLFileRelay\ssh\test_key.pem" $acl
```

### Windows Service Configuration

The new service will run alongside the old one:

| Aspect | Old (ZLBridge) | New (ZLFileRelay) |
|--------|----------------|-------------------|
| Service Name | `ZLBridge` | `ZLFileRelay` |
| Display Name | ZLBridge | ZL File Relay Service (TEST) |
| Event Log Source | `DMZFileTransfer` | `ZLFileRelay` |
| Watch Path | `E:\uploads\scada` | `E:\FileRelay-TEST\uploads\transfer` |
| Service Account | (current account) | Same or different |

**Installation:**
```powershell
# Stop if already installed
Stop-Service -Name "ZLFileRelay" -ErrorAction SilentlyContinue

# Install directory
$installPath = "C:\Program Files\ZLFileRelay"
New-Item -ItemType Directory -Force -Path $installPath

# Copy your published files to $installPath
# Then install service:

New-Service -Name "ZLFileRelay" `
    -BinaryPathName "$installPath\ZLFileRelay.Service.exe" `
    -DisplayName "ZL File Relay Service (TEST)" `
    -StartupType Automatic `
    -Description "ZL File Relay automated file transfer service - TEST INSTANCE"

# Set service account (use same as production for testing)
sc.exe config ZLFileRelay obj= "DFWDMZ\svc_dmz_transfer" password= "YourPassword"

# Or use Local System for quick test
sc.exe config ZLFileRelay obj= "LocalSystem"

# Start it
Start-Service -Name "ZLFileRelay"

# Verify both services running
Get-Service -Name "ZLBridge", "ZLFileRelay" | Format-Table -AutoSize
```

### IIS Web Portal Configuration

The new web portal will run on a different port:

| Aspect | Old (DMZUploader) | New (ZLFileRelay) |
|--------|-------------------|-------------------|
| IIS Site Name | (existing name) | `ZLFileRelay` |
| Port | 80 or 443 | **8080** (HTTP) or **8443** (HTTPS) |
| Physical Path | (existing path) | `C:\Program Files\ZLFileRelay\WebPortal` |
| App Pool | (existing) | `ZLFileRelay` |
| Upload Path | `E:\uploads\*` | `E:\FileRelay-TEST\uploads` |

**Installation:**
```powershell
Import-Module WebAdministration

# Create app pool
if (-not (Test-Path "IIS:\AppPools\ZLFileRelay")) {
    New-WebAppPool -Name "ZLFileRelay"
    Set-ItemProperty "IIS:\AppPools\ZLFileRelay" -Name "managedRuntimeVersion" -Value ""
}

# Set app pool identity (same as production for testing)
Set-ItemProperty "IIS:\AppPools\ZLFileRelay" -Name "processModel.identityType" -Value "SpecificUser"
Set-ItemProperty "IIS:\AppPools\ZLFileRelay" -Name "processModel.userName" -Value "DFWDMZ\svc_dmz_transfer"
Set-ItemProperty "IIS:\AppPools\ZLFileRelay" -Name "processModel.password" -Value "YourPassword"

# Or use ApplicationPoolIdentity
Set-ItemProperty "IIS:\AppPools\ZLFileRelay" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"

# Create website
$webPath = "C:\Program Files\ZLFileRelay\WebPortal"
New-WebSite -Name "ZLFileRelay" `
    -PhysicalPath $webPath `
    -ApplicationPool "ZLFileRelay" `
    -Port 8080

# Configure Windows Authentication
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" `
    -Name "enabled" -Value $true -PSPath "IIS:\Sites\ZLFileRelay"

Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" `
    -Name "enabled" -Value $false -PSPath "IIS:\Sites\ZLFileRelay"

# Start site
Start-WebSite -Name "ZLFileRelay"

# Verify both sites running
Get-WebSite | Select-Object Name, State, @{Name="Bindings";Expression={$_.Bindings.Collection.bindingInformation}} | Format-Table -AutoSize
```

### Firewall Configuration

Open the test port:
```powershell
New-NetFirewallRule -DisplayName "ZLFileRelay TEST HTTP" `
    -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow

# For HTTPS
New-NetFirewallRule -DisplayName "ZLFileRelay TEST HTTPS" `
    -Direction Inbound -Protocol TCP -LocalPort 8443 -Action Allow
```

## Access Points

After deployment, you'll have:

| System | URL | Status |
|--------|-----|--------|
| **Old DMZUploader** | `http://dmz-server` or `https://dmz-server` | ✅ Production (unchanged) |
| **New ZLFileRelay** | `http://dmz-server:8080` | 🧪 Testing |

Both systems fully functional, zero conflicts.

## Testing Checklist

### Pre-Flight Checks
```powershell
# Verify old service still running
Get-Service -Name "ZLBridge" | Format-List Name, Status, StartType

# Verify old web site still running
Get-WebSite | Where-Object {$_.Name -ne "ZLFileRelay"}

# Verify new directories created
Test-Path "E:\FileRelay-TEST\uploads"

# Verify config file exists
Test-Path "C:\ProgramData\ZLFileRelay\appsettings.json"

# Verify new service installed
Get-Service -Name "ZLFileRelay"

# Verify new website created
Get-WebSite -Name "ZLFileRelay"
```

### Functional Tests

1. **Old System Check** (Should be unaffected)
   - [ ] Access old DMZUploader portal
   - [ ] Upload a test file through old portal
   - [ ] Verify old service picks it up
   - [ ] Verify file transfers successfully

2. **New System Test**
   - [ ] Access new portal at `http://dmz-server:8080`
   - [ ] Verify branding shows "(TEST)"
   - [ ] Upload a test file
   - [ ] Check file appears in `E:\FileRelay-TEST\uploads`
   - [ ] Move file to `E:\FileRelay-TEST\uploads\transfer`
   - [ ] Verify new service picks it up (check logs)
   - [ ] Verify file transfers to test destination

3. **Isolation Verification**
   - [ ] Upload to old portal → file goes to `E:\uploads\scada`
   - [ ] Upload to new portal → file goes to `E:\FileRelay-TEST\uploads`
   - [ ] Old service only processes `E:\uploads\scada`
   - [ ] New service only processes `E:\FileRelay-TEST\uploads\transfer`
   - [ ] No cross-contamination

### Log Monitoring

```powershell
# Watch old service logs (if file-based)
Get-Content "E:\uploads\logs\*" -Tail 20 -Wait

# Watch new service logs
Get-Content "E:\FileRelay-TEST\logs\service-*.log" -Tail 20 -Wait

# Check Event Logs
Get-EventLog -LogName Application -Source "DMZFileTransfer" -Newest 10 | Format-Table -AutoSize
Get-EventLog -LogName Application -Source "ZLFileRelay" -Newest 10 | Format-Table -AutoSize
```

## Remote Management with ConfigTool

The ConfigTool can manage the new service remotely:

1. Run ConfigTool on your workstation
2. Go to **Remote Server** tab
3. Add server: `dmz-server` (or IP)
4. Test connection
5. View/edit configuration
6. Control service remotely

This only affects the NEW service, never touches the old one.

## Migration Path

Once testing is complete (1-2 weeks):

### Phase 1: Dual Operation
- Keep both systems running
- Direct new users to new system
- Monitor for issues

### Phase 2: Port Switchover
When confident:
```powershell
# Stop old site
Stop-WebSite -Name "DMZUploader"

# Change new site to port 80
Set-ItemProperty "IIS:\Sites\ZLFileRelay" -Name "bindings" -Value @{protocol="http";bindingInformation="*:80:"}

# Update branding in config (remove "TEST")
# Restart new site
```

### Phase 3: Service Switchover
```powershell
# Stop and disable old service
Stop-Service -Name "ZLBridge"
Set-Service -Name "ZLBridge" -StartupType Disabled

# Update new service to watch production path (if desired)
# Or keep separate paths and just stop using old path
```

### Phase 4: Cleanup
After 30 days of stable operation:
```powershell
# Remove old service
sc.exe delete ZLBridge

# Remove old website
Remove-WebSite -Name "DMZUploader"
Remove-WebAppPool -Name "DMZUploader"

# Archive old upload directories
Move-Item "E:\uploads" -Destination "E:\uploads-ARCHIVED-$(Get-Date -Format 'yyyyMMdd')"
```

## Rollback Plan

If issues found during testing:

```powershell
# Stop test service
Stop-Service -Name "ZLFileRelay"
Set-Service -Name "ZLFileRelay" -StartupType Disabled

# Stop test website  
Stop-WebSite -Name "ZLFileRelay"

# Production system continues unaffected
```

## Troubleshooting

### Both Services Fighting Over Files
**Symptom**: Files disappearing or duplicate transfers

**Solution**: Verify watch paths are different
```powershell
# Old service should only watch:
E:\uploads\scada

# New service should only watch:
E:\FileRelay-TEST\uploads\transfer
```

### Port Already in Use
**Symptom**: IIS site won't start

**Solution**: Check what's using the port
```powershell
netstat -ano | findstr ":8080"
# Choose different port if needed
```

### Permission Errors
**Symptom**: Upload fails or service can't read files

**Solution**: Verify both services have access
```powershell
# Check effective permissions
Get-Acl "E:\FileRelay-TEST" | Format-List

# Verify service account
Get-Service -Name "ZLFileRelay" | Select-Object Name, @{Name="Account";Expression={(sc.exe qc $_.Name | Select-String "SERVICE_START_NAME").ToString().Split(":")[1].Trim()}}
```

### SSH Key Issues
**Symptom**: Transfer fails with authentication error

**Solution**: Verify key permissions
```powershell
# Check key file access
Get-Acl "C:\ProgramData\ZLFileRelay\ssh\test_key.pem" | Format-List

# Test SSH manually
ssh -i "C:\ProgramData\ZLFileRelay\ssh\test_key.pem" -p 2225 svc_dmz_transfer@dfw-files-01.dfw.scada
```

## Support

### Log Locations
- **Old Service**: `E:\uploads\logs\`
- **Old Service Event Log**: Application log, source "DMZFileTransfer"
- **New Service**: `E:\FileRelay-TEST\logs\service-*.log`
- **New Service Event Log**: Application log, source "ZLFileRelay"  
- **New Web Portal**: `E:\FileRelay-TEST\logs\webportal-*.log`

### Key Differences Summary

| Component | Old System | New System |
|-----------|-----------|------------|
| Service Name | ZLBridge | ZLFileRelay |
| Service Display | ZLBridge | ZL File Relay Service (TEST) |
| Event Source | DMZFileTransfer | ZLFileRelay |
| IIS Site | DMZUploader (or default) | ZLFileRelay |
| Port | 80/443 | 8080/8443 |
| Upload Path | E:\uploads\scada | E:\FileRelay-TEST\uploads |
| Watch Path | E:\uploads\scada | E:\FileRelay-TEST\uploads\transfer |
| Log Path | E:\uploads\logs | E:\FileRelay-TEST\logs |
| SSH Dest | F:\Transfer | /test/uploads |

## Conclusion

With these settings, both systems will run completely independently:
- ✅ **No conflicts** - Different service names, paths, and ports
- ✅ **Production safe** - Old system untouched and unaffected
- ✅ **Easy testing** - Side-by-side comparison possible
- ✅ **Simple rollback** - Just stop new services
- ✅ **Gradual migration** - Move users over at your pace

You now have a complete blueprint for zero-risk testing in your production DMZ environment.

