# ZL File Relay - DMZ Deployment Guide

## Quick Start for DMZ/OT Network Deployment

This guide provides step-by-step instructions for deploying ZL File Relay in a DMZ environment for secure file transfer to SCADA/OT networks.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Architecture Overview](#architecture-overview)
3. [Pre-Deployment Checklist](#pre-deployment-checklist)
4. [Installation Steps](#installation-steps)
5. [Network Configuration](#network-configuration)
6. [Security Hardening](#security-hardening)
7. [Testing & Validation](#testing--validation)
8. [Monitoring & Maintenance](#monitoring--maintenance)
9. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Hardware Requirements

- **Minimum:**
  - 2 CPU cores
  - 4 GB RAM
  - 100 GB disk space
  
- **Recommended:**
  - 4 CPU cores
  - 8 GB RAM
  - 250 GB disk space

### Software Requirements

- Windows Server 2019 or later (2022 recommended)
- .NET 8.0 Runtime (ASP.NET Core Runtime)
- PowerShell 5.1 or later
- Windows OpenSSH Client (included in Windows Server 2019+)

### Network Requirements

- **Two Network Interfaces:**
  - NIC1: Corporate/DMZ network (for web access)
  - NIC2: SCADA/OT network (for file transfer)
  
- **Firewall Rules:**
  - Inbound: TCP 8080 from corporate network
  - Outbound: TCP 22 to SCADA server
  - All other traffic blocked

### Accounts & Permissions

- Domain account for web portal users (in allowed AD group)
- Service account for running services (least privilege)
- SSH credentials for SCADA server access

---

## Architecture Overview

```
┌─────────────────┐         DMZ Server          ┌─────────────────┐
│                 │                              │                 │
│   Corporate     │──────NIC1─────┐             │   SCADA/OT      │
│   Network       │   (8080/HTTP)  │             │   Network       │
│   (Users)       │                │             │   (Destination) │
│                 │                │             │                 │
└─────────────────┘                │             └─────────────────┘
                                   │                      ▲
                         ┌─────────▼────────┐            │
                         │                  │            │
                         │  ZL File Relay   │            │
                         │                  │            │
                         │  - Web Portal    │────NIC2────┘
                         │  - Transfer Svc  │   (22/SSH)
                         │                  │
                         └──────────────────┘
```

**Data Flow:**
1. Users upload files via web portal (NIC1)
2. Files saved to transfer directory
3. Service watches for new files
4. Service transfers files via SSH (NIC2)
5. Files archived after successful transfer

---

## Pre-Deployment Checklist

### Network Team

- [ ] DMZ server provisioned with 2 NICs
- [ ] Firewall rules configured
- [ ] DNS entry created (optional)
- [ ] Network segmentation verified
- [ ] SSH connectivity to SCADA server tested

### AD/Identity Team

- [ ] AD group created for file upload users
- [ ] Service account created
- [ ] Appropriate permissions granted
- [ ] Group membership configured

### SCADA/OT Team

- [ ] SSH service account created on SCADA server
- [ ] SSH key pair generated
- [ ] Public key installed on SCADA server
- [ ] Destination directory created and permissions set
- [ ] Firewall rules on SCADA side configured

### Application Team

- [ ] Installation media obtained
- [ ] Configuration file prepared
- [ ] SSL certificate obtained (if using HTTPS)
- [ ] Change control approved
- [ ] Rollback plan documented

---

## Installation Steps

### Step 1: Install Prerequisites

```powershell
# Run as Administrator

# Install .NET 8.0 Runtime
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0
# Or use winget:
winget install Microsoft.DotNet.Runtime.8

# Verify OpenSSH Client is installed
Get-WindowsCapability -Online | Where-Object Name -like 'OpenSSH.Client*'

# If not installed:
Add-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0
```

### Step 2: Deploy Application Files

```powershell
# Create installation directory
New-Item -ItemType Directory -Path "C:\Program Files\ZLFileRelay" -Force

# Copy application files (from build/deployment package)
Copy-Item -Path "\\deployment-share\ZLFileRelay\*" -Destination "C:\Program Files\ZLFileRelay\" -Recurse

# Create data directories
New-Item -ItemType Directory -Path "C:\FileRelay\uploads" -Force
New-Item -ItemType Directory -Path "C:\FileRelay\uploads\transfer" -Force
New-Item -ItemType Directory -Path "C:\FileRelay\archive" -Force
New-Item -ItemType Directory -Path "C:\FileRelay\logs" -Force
New-Item -ItemType Directory -Path "C:\ProgramData\ZLFileRelay" -Force
New-Item -ItemType Directory -Path "C:\ProgramData\ZLFileRelay\ssh" -Force
```

### Step 3: Configure Application

```powershell
# Copy DMZ configuration template
Copy-Item -Path "C:\Program Files\ZLFileRelay\appsettings.DMZ.json" `
          -Destination "C:\Program Files\ZLFileRelay\appsettings.Production.json"

# Edit configuration
notepad "C:\Program Files\ZLFileRelay\appsettings.Production.json"
```

**Edit these critical settings:**

1. **AllowedGroups**: Replace `YOURDOMAIN\SCADA_Operators` with your AD group
2. **SSH Host**: Replace `10.x.x.x` with your SCADA server IP
3. **SSH Username**: Replace `svc_filetransfer` with your SSH account
4. **SupportEmail**: Update to your support email
5. **SiteName**: Customize site name

### Step 4: Install SSH Keys

```powershell
# Copy SSH private key to secure location
Copy-Item -Path "\\secure-share\id_ed25519" `
          -Destination "C:\ProgramData\ZLFileRelay\ssh\id_ed25519"

# Set restrictive permissions on SSH key (SYSTEM and Admins only)
$acl = Get-Acl "C:\ProgramData\ZLFileRelay\ssh\id_ed25519"
$acl.SetAccessRuleProtection($true, $false)  # Disable inheritance

# Remove all existing permissions
$acl.Access | ForEach-Object { $acl.RemoveAccessRule($_) }

# Add SYSTEM
$systemSid = New-Object System.Security.Principal.SecurityIdentifier("S-1-5-18")
$systemRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $systemSid, "FullControl", "Allow")
$acl.AddAccessRule($systemRule)

# Add Administrators
$adminsSid = New-Object System.Security.Principal.SecurityIdentifier("S-1-5-32-544")
$adminsRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $adminsSid, "FullControl", "Allow")
$acl.AddAccessRule($adminsRule)

Set-Acl -Path "C:\ProgramData\ZLFileRelay\ssh\id_ed25519" -AclObject $acl

# Verify permissions
Get-Acl "C:\ProgramData\ZLFileRelay\ssh\id_ed25519" | Format-List
```

### Step 5: Test SSH Connectivity

```powershell
# Test SSH connection manually
ssh -i "C:\ProgramData\ZLFileRelay\ssh\id_ed25519" `
    -o StrictHostKeyChecking=yes `
    svc_filetransfer@10.x.x.x `
    "echo 'Connection successful'"

# You should see: Connection successful
# If you see host key prompt, accept it (this adds to known_hosts)
```

### Step 6: Install Windows Services

```powershell
# Navigate to application directory
cd "C:\Program Files\ZLFileRelay"

# Install Transfer Service
New-Service -Name "ZLFileRelay" `
            -DisplayName "ZL File Relay Service" `
            -Description "Automated file transfer from DMZ to SCADA networks" `
            -BinaryPathName "C:\Program Files\ZLFileRelay\ZLFileRelay.Service.exe" `
            -StartupType Automatic `
            -Credential (Get-Credential -Message "Enter service account credentials")

# Install Web Portal Service
New-Service -Name "ZLFileRelay.WebPortal" `
            -DisplayName "ZL File Relay Web Portal" `
            -Description "Web upload portal for ZL File Relay" `
            -BinaryPathName "C:\Program Files\ZLFileRelay\ZLFileRelay.WebPortal.exe" `
            -StartupType Automatic `
            -Credential (Get-Credential -Message "Enter service account credentials")

# Configure service recovery options
sc.exe failure ZLFileRelay reset= 86400 actions= restart/5000/restart/10000/restart/30000
sc.exe failure ZLFileRelay.WebPortal reset= 86400 actions= restart/5000/restart/10000/restart/30000
```

### Step 7: Set Directory Permissions

```powershell
# Grant service account write access to data directories
$serviceAccount = "DOMAIN\svc_zlfilerelay"  # Replace with your service account

$directories = @(
    "C:\FileRelay\uploads",
    "C:\FileRelay\archive",
    "C:\FileRelay\logs",
    "C:\ProgramData\ZLFileRelay"
)

foreach ($dir in $directories) {
    $acl = Get-Acl $dir
    $permission = New-Object System.Security.AccessControl.FileSystemAccessRule(
        $serviceAccount, "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.AddAccessRule($permission)
    Set-Acl $dir $acl
    Write-Host "Permissions set for $dir"
}
```

### Step 8: Start Services

```powershell
# Start services
Start-Service -Name "ZLFileRelay"
Start-Service -Name "ZLFileRelay.WebPortal"

# Verify services are running
Get-Service -Name "ZLFileRelay*" | Format-Table -AutoSize

# Check service logs
Get-Content "C:\FileRelay\logs\zlfilerelay-*.log" -Tail 50
Get-Content "C:\FileRelay\logs\zlfilerelay-web-*.log" -Tail 50
```

---

## Network Configuration

### Firewall Rules - DMZ Server

```powershell
# Run as Administrator

# Allow inbound HTTP from corporate network
New-NetFirewallRule -DisplayName "ZL File Relay - HTTP Inbound" `
                    -Direction Inbound `
                    -Protocol TCP `
                    -LocalPort 8080 `
                    -Action Allow `
                    -Profile Domain `
                    -RemoteAddress "10.1.0.0/16"  # Replace with your corporate network

# Allow outbound SSH to SCADA network
New-NetFirewallRule -DisplayName "ZL File Relay - SSH Outbound" `
                    -Direction Outbound `
                    -Protocol TCP `
                    -RemotePort 22 `
                    -Action Allow `
                    -Profile Domain `
                    -RemoteAddress "10.2.0.100"  # Replace with your SCADA server IP

# Block all other outbound traffic (optional - be careful!)
# New-NetFirewallRule -DisplayName "ZL File Relay - Block Other Outbound" `
#                     -Direction Outbound `
#                     -Action Block `
#                     -Profile Domain
```

### Network Interface Configuration

```powershell
# Verify network interfaces
Get-NetAdapter | Format-Table Name, InterfaceDescription, Status, LinkSpeed

# Example output:
# Name   InterfaceDescription                     Status LinkSpeed
# ----   ---------------------                     ------ ---------
# Eth0   Intel(R) Ethernet Adapter #1             Up     1 Gbps    <- Corporate
# Eth1   Intel(R) Ethernet Adapter #2             Up     1 Gbps    <- SCADA

# Set static routes if needed (prevent routing between networks)
# Route corporate traffic via NIC1
New-NetRoute -DestinationPrefix "10.1.0.0/16" -InterfaceAlias "Eth0" -NextHop "10.1.x.x"

# Route SCADA traffic via NIC2
New-NetRoute -DestinationPrefix "10.2.0.0/16" -InterfaceAlias "Eth1" -NextHop "10.2.x.x"

# Disable IP forwarding (critical!)
Set-NetIPInterface -InterfaceAlias "Eth0" -Forwarding Disabled
Set-NetIPInterface -InterfaceAlias "Eth1" -Forwarding Disabled
```

---

## Security Hardening

### Windows Server Hardening

```powershell
# Disable unnecessary services
$servicesToDisable = @(
    "RemoteRegistry",
    "SSDPSRV",          # SSDP Discovery
    "upnphost",         # UPnP Device Host
    "WMPNetworkSvc"     # Windows Media Player Network Sharing
)

foreach ($service in $servicesToDisable) {
    Stop-Service -Name $service -Force -ErrorAction SilentlyContinue
    Set-Service -Name $service -StartupType Disabled -ErrorAction SilentlyContinue
}

# Enable Windows Defender
Set-MpPreference -DisableRealtimeMonitoring $false
Update-MpSignature
```

### Secure Configuration File

```powershell
# Restrict access to configuration directory
$configPath = "C:\ProgramData\ZLFileRelay"
$acl = Get-Acl $configPath

# Remove inheritance
$acl.SetAccessRuleProtection($true, $false)

# Remove all existing permissions
$acl.Access | ForEach-Object { $acl.RemoveAccessRule($_) }

# Add SYSTEM
$systemRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "NT AUTHORITY\SYSTEM", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.AddAccessRule($systemRule)

# Add Administrators
$adminsRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "BUILTIN\Administrators", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.AddAccessRule($adminsRule)

# Apply ACL
Set-Acl -Path $configPath -AclObject $acl
```

### Audit Policy Configuration

```powershell
# Enable security auditing
auditpol /set /category:"Logon/Logoff" /success:enable /failure:enable
auditpol /set /category:"Object Access" /success:enable /failure:enable
auditpol /set /category:"Policy Change" /success:enable /failure:enable

# Verify audit settings
auditpol /get /category:*
```

---

## Testing & Validation

### Pre-Production Testing

```powershell
# 1. Test web portal access
$testUrl = "http://dmz-server:8080"
$response = Invoke-WebRequest -Uri $testUrl -UseDefaultCredentials -UseBasicParsing
Write-Host "Web Portal Status: $($response.StatusCode)"

# 2. Test file upload via web portal
# - Login via browser: http://dmz-server:8080
# - Upload a small test file
# - Verify file appears in C:\FileRelay\uploads\transfer

# 3. Monitor transfer service logs
Get-Content "C:\FileRelay\logs\zlfilerelay-*.log" -Wait

# 4. Verify file transferred to SCADA server
ssh -i "C:\ProgramData\ZLFileRelay\ssh\id_ed25519" `
    svc_filetransfer@10.x.x.x `
    "ls -la /data/incoming"

# 5. Check service status
Get-Service -Name "ZLFileRelay*"
Get-EventLog -LogName Application -Source "ZL File Relay" -Newest 20

# 6. Verify archive functionality
dir "C:\FileRelay\archive"
```

### Load Testing (Optional)

```powershell
# Upload multiple files concurrently
$files = @(
    "C:\temp\test1.txt",
    "C:\temp\test2.txt",
    "C:\temp\test3.txt"
)

$jobs = $files | ForEach-Object {
    Start-Job -ScriptBlock {
        param($file)
        # Simulate file upload (implement based on your web portal API)
        Copy-Item $file "C:\FileRelay\uploads\transfer\"
    } -ArgumentList $_
}

$jobs | Wait-Job
$jobs | Receive-Job
$jobs | Remove-Job
```

---

## Monitoring & Maintenance

### Health Checks

```powershell
# Create scheduled task for health monitoring
$action = New-ScheduledTaskAction -Execute "PowerShell.exe" `
    -Argument "-File C:\Scripts\ZLFileRelay-HealthCheck.ps1"

$trigger = New-ScheduledTaskTrigger -Daily -At "08:00AM"

Register-ScheduledTask -TaskName "ZL File Relay Health Check" `
    -Action $action -Trigger $trigger -User "SYSTEM"
```

**Health Check Script** (`C:\Scripts\ZLFileRelay-HealthCheck.ps1`):

```powershell
# Check if services are running
$services = Get-Service -Name "ZLFileRelay*"
foreach ($svc in $services) {
    if ($svc.Status -ne "Running") {
        Write-EventLog -LogName Application -Source "ZL File Relay" `
            -EntryType Error -EventId 1001 `
            -Message "Service $($svc.Name) is not running!"
        
        # Attempt to restart
        Start-Service -Name $svc.Name
    }
}

# Check disk space
$drive = Get-Volume -DriveLetter C
if ($drive.SizeRemaining -lt 10GB) {
    Write-EventLog -LogName Application -Source "ZL File Relay" `
        -EntryType Warning -EventId 1002 `
        -Message "Low disk space: $($drive.SizeRemaining / 1GB) GB remaining"
}

# Check log file size
$logFiles = Get-ChildItem "C:\FileRelay\logs" -Filter "*.log"
$totalLogSize = ($logFiles | Measure-Object -Property Length -Sum).Sum / 1MB
if ($totalLogSize -gt 1000) {
    Write-EventLog -LogName Application -Source "ZL File Relay" `
        -EntryType Warning -EventId 1003 `
        -Message "Large log file size: $totalLogSize MB"
}

# Check SSH connectivity
$sshTest = ssh -i "C:\ProgramData\ZLFileRelay\ssh\id_ed25519" `
               -o ConnectTimeout=10 `
               -o StrictHostKeyChecking=yes `
               svc_filetransfer@10.x.x.x `
               "echo 'ok'" 2>&1

if ($sshTest -notmatch "ok") {
    Write-EventLog -LogName Application -Source "ZL File Relay" `
        -EntryType Error -EventId 1004 `
        -Message "SSH connectivity test failed: $sshTest"
}

Write-Host "Health check completed successfully"
```

### Log Monitoring

```powershell
# Set up log forwarding to SIEM (example using NXLog)
# Install NXLog or similar log forwarder
# Configure to monitor: C:\FileRelay\logs\*.log

# Or use Windows Event Forwarding
wecutil qc
```

### Maintenance Tasks

**Weekly:**
```powershell
# Review logs
Get-Content "C:\FileRelay\logs\zlfilerelay-*.log" | Select-String "ERROR|WARN"

# Check archive size
$archiveSize = (Get-ChildItem "C:\FileRelay\archive" -Recurse | Measure-Object -Property Length -Sum).Sum / 1GB
Write-Host "Archive size: $archiveSize GB"

# Verify services
Get-Service -Name "ZLFileRelay*"
```

**Monthly:**
```powershell
# Archive old logs
$oldLogs = Get-ChildItem "C:\FileRelay\logs" -Filter "*.log" | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) }
if ($oldLogs) {
    Compress-Archive -Path $oldLogs.FullName -DestinationPath "C:\FileRelay\logs\archive-$(Get-Date -Format 'yyyyMM').zip"
    $oldLogs | Remove-Item
}

# Review user access
# Check AD group membership, remove inactive users
```

**Quarterly:**
```powershell
# Apply Windows updates
Install-WindowsUpdate -AcceptAll -AutoReboot

# Review and update blocked file extensions
# Edit appsettings.Production.json if needed

# Test disaster recovery
# Backup configuration, test service restoration
```

---

## Troubleshooting

### Service Won't Start

```powershell
# Check service status
Get-Service -Name "ZLFileRelay"

# Check Windows Event Log
Get-EventLog -LogName Application -Source "ZL File Relay" -Newest 10

# Check application logs
Get-Content "C:\FileRelay\logs\zlfilerelay-*.log" -Tail 100

# Verify configuration file
Test-Path "C:\Program Files\ZLFileRelay\appsettings.Production.json"

# Check service account permissions
icacls "C:\FileRelay"
icacls "C:\ProgramData\ZLFileRelay"
```

### File Transfer Failures

```powershell
# Test SSH connectivity manually
ssh -i "C:\ProgramData\ZLFileRelay\ssh\id_ed25519" `
    -v `  # Verbose mode
    svc_filetransfer@10.x.x.x

# Check SSH key permissions
icacls "C:\ProgramData\ZLFileRelay\ssh\id_ed25519"

# Verify SCADA server reachability
Test-NetConnection -ComputerName 10.x.x.x -Port 22

# Check service logs for errors
Get-Content "C:\FileRelay\logs\zlfilerelay-*.log" | Select-String "ERROR"

# Verify source file is stable (not still being written)
# Check FileStabilitySeconds in configuration
```

### Authentication Issues

```powershell
# Verify Windows Authentication is working
# Check IIS Authentication settings (if using IIS)
Get-WindowsFeature | Where-Object Name -like "*Windows-Auth*"

# Verify user is in allowed AD group
$user = "DOMAIN\username"
$group = "DOMAIN\SCADA_Operators"

$adUser = Get-ADUser -Identity $user.Split('\')[1]
$adGroup = Get-ADGroup -Identity $group.Split('\')[1]
Get-ADGroupMember -Identity $adGroup | Where-Object { $_.SamAccountName -eq $adUser.SamAccountName }

# Check service configuration
$config = Get-Content "C:\Program Files\ZLFileRelay\appsettings.Production.json" | ConvertFrom-Json
$config.ZLFileRelay.WebPortal.AllowedGroups
```

### High CPU/Memory Usage

```powershell
# Check for large file queue
Get-ChildItem "C:\FileRelay\uploads\transfer" -Recurse | Measure-Object

# Monitor process resources
Get-Process -Name "ZLFileRelay*" | Format-Table Name, CPU, WorkingSet

# Check for file watcher issues
# Restart service if watching too many files
Restart-Service -Name "ZLFileRelay"

# Review MaxConcurrentTransfers setting
# May need to reduce if system is constrained
```

### Network Connectivity Issues

```powershell
# Trace route to SCADA server
tracert 10.x.x.x

# Check firewall rules
Get-NetFirewallRule -DisplayName "*ZL File Relay*" | Format-Table -AutoSize

# Test network port
Test-NetConnection -ComputerName 10.x.x.x -Port 22

# Check routing table
route print

# Verify no routing between NICs
Get-NetIPInterface | Format-Table InterfaceAlias, Forwarding
```

---

## Emergency Procedures

### Service Shutdown

```powershell
# Stop services gracefully
Stop-Service -Name "ZLFileRelay.WebPortal" -Force
Stop-Service -Name "ZLFileRelay" -Force

# Verify services stopped
Get-Service -Name "ZLFileRelay*"

# Wait for ongoing transfers to complete (check logs)
Get-Content "C:\FileRelay\logs\zlfilerelay-*.log" -Tail 50
```

### Rollback Procedure

```powershell
# Stop services
Stop-Service -Name "ZLFileRelay*" -Force

# Restore previous version
Copy-Item -Path "C:\Backup\ZLFileRelay\*" `
          -Destination "C:\Program Files\ZLFileRelay\" `
          -Recurse -Force

# Restore configuration
Copy-Item -Path "C:\Backup\appsettings.Production.json" `
          -Destination "C:\Program Files\ZLFileRelay\" -Force

# Start services
Start-Service -Name "ZLFileRelay"
Start-Service -Name "ZLFileRelay.WebPortal"
```

### Incident Response

**Security Incident:**
1. Stop services immediately
2. Preserve logs: `Copy-Item "C:\FileRelay\logs" -Destination "C:\Incident\logs" -Recurse`
3. Check for unauthorized access in logs
4. Review uploaded files in archive
5. Contact security team

**Service Outage:**
1. Check Windows Event Log
2. Review application logs
3. Test network connectivity
4. Verify service account status
5. Restart services if safe to do so
6. Contact support if issue persists

---

## Support & Contacts

**Application Support:**
- Email: support@example.com
- Internal Wiki: https://wiki.example.com/ZLFileRelay

**Escalation:**
- Critical: Call on-call engineer
- Non-Critical: Submit ticket to application support

**Vendor Support:**
- Product: ZL File Relay
- Documentation: See `/docs` folder

---

## Appendix

### Configuration Reference

See `docs/CONFIGURATION.md` for complete configuration options.

### Security Assessment

See `SECURITY_PERFORMANCE_ASSESSMENT.md` for security review results.

### Architecture Details

See `docs/README.md` for architectural overview.

---

**Document Version:** 1.0  
**Last Updated:** October 9, 2025  
**Next Review:** Post-deployment +30 days


