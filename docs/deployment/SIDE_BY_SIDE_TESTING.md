# Side-by-Side Testing Guide

This guide explains how to run ZLFileRelay alongside your existing production file transfer setup for safe testing.

## Overview

ZLFileRelay is designed to coexist with your legacy DMZFileTransferService/DMZUploader installation, allowing you to:
- Test the new system without disrupting production
- Compare functionality side-by-side
- Gradually migrate users to the new system
- Roll back easily if needed

## Prerequisites

- Your existing production system running on standard ports (80/443)
- Available port for testing (recommended: 8080 or 8443)
- Separate test directory structure
- Administrative access to the server

## Step-by-Step Setup

### 1. Choose Your Test Port

Pick a port that's not in use:
- **HTTP**: 8080, 8888, 9080
- **HTTPS**: 8443, 8444, 9443

To check if a port is available:
```powershell
Test-NetConnection -ComputerName localhost -Port 8080
```
If it fails to connect, the port is available.

### 2. Configure Test Paths

Create a test configuration that doesn't interfere with production:

**appsettings.json** (in C:\ProgramData\ZLFileRelay\):
```json
{
  "ZLFileRelay": {
    "Branding": {
      "CompanyName": "Your Company",
      "ProductName": "ZL File Relay (TEST)",
      "SiteName": "Test Site",
      "SupportEmail": "support@example.com"
    },
    "Paths": {
      "UploadDirectory": "C:\\FileRelay\\TEST\\uploads",
      "LogDirectory": "C:\\FileRelay\\TEST\\logs",
      "ConfigDirectory": "C:\\ProgramData\\ZLFileRelay"
    },
    "Service": {
      "Enabled": true,
      "WatchDirectory": "C:\\FileRelay\\TEST\\uploads\\transfer",
      "TransferMethod": "ssh",
      "RetryAttempts": 3,
      "RetryDelaySeconds": 30,
      "PollingIntervalSeconds": 10
    },
    "WebPortal": {
      "Enabled": true,
      "RequireAuthentication": true,
      "AllowedGroups": [],
      "MaxFileSizeBytes": 4294967295,
      "ShowUploadHistory": true
    },
    "Transfer": {
      "Ssh": {
        "Host": "your-test-server.com",
        "Port": 22,
        "Username": "testuser",
        "PrivateKeyPath": "C:\\ProgramData\\ZLFileRelay\\ssh\\test_key",
        "DestinationPath": "/test/uploads"
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

**Note the TEST indicators:**
- Product name includes "(TEST)" so users know it's not production
- All paths use `C:\FileRelay\TEST\` instead of production paths
- SSH destination uses `/test/uploads` instead of production path

### 3. Create Test Directory Structure

Run in PowerShell as Administrator:
```powershell
# Create test directories
New-Item -ItemType Directory -Force -Path "C:\FileRelay\TEST\uploads"
New-Item -ItemType Directory -Force -Path "C:\FileRelay\TEST\uploads\transfer"
New-Item -ItemType Directory -Force -Path "C:\FileRelay\TEST\logs"
New-Item -ItemType Directory -Force -Path "C:\ProgramData\ZLFileRelay"

# Set permissions (adjust domain/users as needed)
$acl = Get-Acl "C:\FileRelay\TEST"
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS","FullControl","ContainerInherit,ObjectInherit","None","Allow")
$acl.SetAccessRule($rule)
Set-Acl "C:\FileRelay\TEST" $acl

# Verify
Get-ChildItem "C:\FileRelay\TEST" -Recurse
```

### 4. Deploy the Service

The Windows Service will run alongside your existing service:

```powershell
# Stop if already running
Stop-Service -Name "ZLFileRelay" -ErrorAction SilentlyContinue

# Copy files to install directory
$installPath = "C:\Program Files\ZLFileRelay"
New-Item -ItemType Directory -Force -Path $installPath

# Copy your published service files here
Copy-Item ".\publish\service\*" -Destination $installPath -Recurse -Force

# Install/Update the service
New-Service -Name "ZLFileRelay" -BinaryPathName "$installPath\ZLFileRelay.Service.exe" `
    -DisplayName "ZL File Relay Service (TEST)" `
    -StartupType Automatic `
    -Description "ZL File Relay automated file transfer service (TEST INSTANCE)" `
    -ErrorAction SilentlyContinue

# Or update if exists
sc.exe config ZLFileRelay DisplayName= "ZL File Relay Service (TEST)"

# Start the service
Start-Service -Name "ZLFileRelay"

# Verify
Get-Service -Name "ZLFileRelay"
```

### 5. Configure IIS for Test Site

The Web Portal will run on a different port:

```powershell
# Import IIS module
Import-Module WebAdministration

# Stop site if exists
Stop-WebSite -Name "ZLFileRelay" -ErrorAction SilentlyContinue

# Create application pool (if not exists)
if (-not (Test-Path "IIS:\AppPools\ZLFileRelay")) {
    New-WebAppPool -Name "ZLFileRelay"
    Set-ItemProperty "IIS:\AppPools\ZLFileRelay" -Name "managedRuntimeVersion" -Value ""
    Set-ItemProperty "IIS:\AppPools\ZLFileRelay" -Name "enable32BitAppOnWin64" -Value $false
}

# Create website (if not exists)
$webPath = "C:\Program Files\ZLFileRelay\WebPortal"
if (-not (Test-Path "IIS:\Sites\ZLFileRelay")) {
    New-WebSite -Name "ZLFileRelay" `
        -PhysicalPath $webPath `
        -ApplicationPool "ZLFileRelay" `
        -Port 8080
} else {
    # Update binding to use test port
    Set-ItemProperty "IIS:\Sites\ZLFileRelay" -Name "bindings" -Value @{protocol="http";bindingInformation="*:8080:"}
}

# Configure authentication
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" `
    -Name "enabled" -Value $true -PSPath "IIS:\Sites\ZLFileRelay"

Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" `
    -Name "enabled" -Value $false -PSPath "IIS:\Sites\ZLFileRelay"

# Start the site
Start-WebSite -Name "ZLFileRelay"

# Verify
Get-WebSite -Name "ZLFileRelay" | Select-Object Name, State, PhysicalPath, @{Name="Bindings";Expression={$_.Bindings.Collection.bindingInformation}}
```

### 6. Test Access

1. **Web Portal**: Navigate to `http://yourserver:8080`
2. **Service Status**: Check Services.msc for "ZL File Relay Service (TEST)"
3. **Logs**: Check `C:\FileRelay\TEST\logs\` for activity

## Verification Checklist

- [ ] Old production service still running (check Services.msc)
- [ ] Old production website still accessible on original port
- [ ] New ZLFileRelay service running (check Services.msc)
- [ ] New web portal accessible on test port (e.g., http://server:8080)
- [ ] Test upload through new portal works
- [ ] Test file appears in `C:\FileRelay\TEST\uploads`
- [ ] Service picks up file from watch directory
- [ ] File transfers successfully (check logs)
- [ ] Production traffic unaffected

## Testing Workflow

### Phase 1: Smoke Test (1-2 hours)
1. Access web portal on test port
2. Upload a small test file
3. Verify file appears in upload directory
4. Move file to transfer directory
5. Verify service picks it up and transfers
6. Check logs for any errors

### Phase 2: Functional Test (1 day)
1. Test with various file types and sizes
2. Test with multiple concurrent uploads
3. Test error scenarios (bad credentials, network issues)
4. Verify retry logic works
5. Test the ConfigTool remote management

### Phase 3: Load Test (1 week)
1. Run alongside production
2. Have select users test the new portal
3. Monitor performance and logs
4. Compare reliability with old system

### Phase 4: Cutover
Once confident:
1. Update production port from old to new (or switch DNS/load balancer)
2. Decommission old service
3. Remove old IIS site
4. Clean up old directories

## Port Configuration Options

### Option A: HTTP Testing (Quick Start)
```
IIS Binding: *:8080:
Access: http://yourserver:8080
```

### Option B: HTTPS Testing (Recommended)
```powershell
# Create/get certificate
$cert = Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object {$_.Subject -like "*yourserver*"}

# Add HTTPS binding
New-WebBinding -Name "ZLFileRelay" -Protocol "https" -Port 8443
$binding = Get-WebBinding -Name "ZLFileRelay" -Protocol "https"
$binding.AddSslCertificate($cert.Thumbprint, "my")
```
Access: `https://yourserver:8443`

### Option C: Hostname Binding (Most Professional)
```powershell
# Add hostname binding (requires DNS)
New-WebBinding -Name "ZLFileRelay" -Protocol "http" -Port 80 -HostHeader "filerelay-test.yourdomain.com"

# Or HTTPS
New-WebBinding -Name "ZLFileRelay" -Protocol "https" -Port 443 -HostHeader "filerelay-test.yourdomain.com"
```
Access: `https://filerelay-test.yourdomain.com`

## Firewall Configuration

If accessing from other machines:
```powershell
# Open test port
New-NetFirewallRule -DisplayName "ZLFileRelay Test HTTP" `
    -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow

# For HTTPS
New-NetFirewallRule -DisplayName "ZLFileRelay Test HTTPS" `
    -Direction Inbound -Protocol TCP -LocalPort 8443 -Action Allow
```

## Troubleshooting

### Web Portal Not Accessible
```powershell
# Check IIS site status
Get-WebSite -Name "ZLFileRelay"

# Check application pool
Get-WebAppPoolState -Name "ZLFileRelay"

# Check port binding
netstat -ano | findstr ":8080"

# Check firewall
Test-NetConnection -ComputerName localhost -Port 8080
```

### Service Not Starting
```powershell
# Check service status
Get-Service -Name "ZLFileRelay"

# Check event log
Get-EventLog -LogName Application -Source "ZLFileRelay" -Newest 10

# Check log files
Get-Content "C:\FileRelay\TEST\logs\service-*.log" -Tail 50
```

### File Not Transferring
1. Check service is running
2. Verify watch directory path in config
3. Check SSH credentials (use ConfigTool "Test Connection")
4. Review service logs for errors
5. Verify file permissions on directories

### Port Conflict
```powershell
# Check what's using a port
netstat -ano | findstr ":8080"

# Find the process
Get-Process -Id <PID from netstat>

# Choose a different port if needed
```

## Rollback Plan

If you need to revert:

```powershell
# Stop test service
Stop-Service -Name "ZLFileRelay"
sc.exe config ZLFileRelay start= disabled

# Stop test website
Stop-WebSite -Name "ZLFileRelay"

# Your production system continues running unchanged
```

## Migration to Production

When ready to replace the old system:

1. **Schedule maintenance window**
2. **Update DNS/bookmarks** to point to new URL
3. **Stop old service and site**
4. **Reconfigure new site** to use production port (80/443)
5. **Update paths** to production directories
6. **Test thoroughly**
7. **Monitor for 24-48 hours**
8. **Decommission old system** once stable

## Comparison Testing

Run both systems simultaneously to compare:

| Aspect | Old System | New System (Test) |
|--------|-----------|------------------|
| URL | http://server | http://server:8080 |
| Service | DMZFileTransferService | ZLFileRelay |
| Upload Dir | C:\DMZUploader\uploads | C:\FileRelay\TEST\uploads |
| Logs | (old location) | C:\FileRelay\TEST\logs |
| SSH Key | (old location) | C:\ProgramData\ZLFileRelay\ssh |

Upload the same file to both systems and compare:
- Upload time
- Transfer success rate
- Error handling
- Log quality
- User experience

## Benefits of Side-by-Side Testing

✅ **Zero risk** to production  
✅ **Direct comparison** of old vs new  
✅ **Gradual migration** path  
✅ **Easy rollback** if issues found  
✅ **User training** opportunity  
✅ **Performance validation** under real conditions  

## Next Steps

Once testing is complete and you're ready for production:
1. See [DEPLOYMENT.md](DEPLOYMENT.md) for full production deployment
2. See [CONFIGURATION.md](CONFIGURATION.md) for production config tuning
3. See [REMOTE_MANAGEMENT.md](REMOTE_MANAGEMENT.md) for managing from ConfigTool

## Support

If you encounter issues during side-by-side testing:
1. Check logs in `C:\FileRelay\TEST\logs`
2. Check Windows Event Viewer (Application log, source: ZLFileRelay)
3. Review this document's troubleshooting section
4. Check [DEPLOYMENT.md](DEPLOYMENT.md) for additional guidance

