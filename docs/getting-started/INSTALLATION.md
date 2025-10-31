# ZL File Relay - Installation Guide

## System Requirements

### Minimum Requirements
- Windows Server 2019 or later (Windows Server 2022 recommended)
- 2 GB RAM
- 500 MB disk space for application
- Additional disk space for file transfers
- .NET 8.0 Runtime (included in self-contained deployment)
- ASP.NET Core 8.0 Runtime (for web portal)

### Required Software
- **IIS 10.0+** (if deploying web portal)
- **.NET 8.0 Runtime** (Download from: https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual C++ Redistributables** (usually pre-installed)

### Network Requirements
- **Inbound**: Port 443 (HTTPS) for web portal
- **Outbound**: Port 22 (SSH) or Port 445 (SMB) to SCADA network
- DNS resolution to SCADA servers
- **Air-Gapped Support**: Works without internet when using local authentication (no Entra ID/Azure AD required)

### Permissions Required
- **Administrative privileges** for installation
- **Modify permissions** on upload directories
- **IIS_IUSRS** access to web portal directories
- **Network Service** access to upload directories

## Pre-Installation Checklist

- [ ] .NET 8.0 Runtime installed
- [ ] ASP.NET Core 8.0 Runtime installed (for web portal)
- [ ] IIS installed and running (for web portal)
- [ ] Firewall rules configured
- [ ] Network connectivity to SCADA servers verified
- [ ] SCADA server SSH access confirmed (or SMB credentials obtained)
- [ ] For air-gapped environments: Configure local authentication (no cloud dependencies)
- [ ] For cloud-connected environments: Optional Entra ID/Azure AD setup (see ENTRA_ID_SETUP.md)

## Installation Steps

### 1. Download Installer
Download `ZLFileRelay-Setup.exe` from the release page.

### 2. Run Installer
1. Right-click `ZLFileRelay-Setup.exe`
2. Select **"Run as administrator"**
3. Follow installation wizard

### 3. Select Components
Choose components to install:
- **File Transfer Service** ✅ (Recommended)
- **Web Upload Portal** ✅ (Requires IIS)
- **Configuration Tool** ✅ (Recommended)

### 4. Choose Installation Directory
Default: `C:\Program Files\ZLFileRelay`

### 5. Configure Data Directories
- **Upload Directory**: `C:\FileRelay\uploads`
- **Log Directory**: `C:\FileRelay\logs`
- **Config Directory**: `C:\ProgramData\ZLFileRelay`

### 6. Complete Installation
Installer will:
- Copy files to installation directory
- Create data directories
- Set proper permissions
- Register Windows Service (if selected)
- Configure IIS site (if web portal selected)
- Create Start Menu shortcuts

## Post-Installation Configuration

### Using Configuration Tool

1. **Launch Configuration Tool**
   - Start Menu → ZL File Relay → Configuration Tool
   - Run as Administrator

2. **Configure General Settings**
   - Set company name and site name
   - Configure log retention policies
   - Set support contact information

3. **Configure File Transfer Service**
   
   **Option A: SSH Transfer (Recommended)**
   - Click "Generate SSH Keys"
   - Copy public key (automatic clipboard copy)
   - Deploy public key to SCADA server
   - Configure SSH connection details
   - Test connection
   
   **Option B: SMB Transfer**
   - Enter UNC path to SCADA share
   - Configure credentials (encrypted with DPAPI)
   - Test connection

4. **Configure Web Portal**
   - **Authentication**: Choose Entra ID (Azure AD) or Local Accounts
     - Air-gapped environments: Use Local Accounts (no internet required)
     - Cloud-connected: Enable Entra ID for SSO
   - Configure admin email addresses and roles
   - Set upload limits and file restrictions
   - Configure branding (logo, colors)
   - Set up email notifications (optional)

5. **Install and Start Services**
   - Click "Install Service"
   - Click "Configure IIS" (for web portal)
   - Click "Start Service"
   - Verify services are running

## Manual Configuration (Advanced)

### Manually Edit Configuration File
Location: `C:\ProgramData\ZLFileRelay\appsettings.json`

See [CONFIGURATION.md](CONFIGURATION.md) for full reference.

### Manual Service Installation
```powershell
# Install service
sc.exe create "ZLFileRelay" binPath= "C:\Program Files\ZLFileRelay\Service\ZLFileRelay.Service.exe" DisplayName= "ZL File Relay Service" start= auto

# Start service
sc.exe start "ZLFileRelay"

# Query service status
sc.exe query "ZLFileRelay"
```

### Manual IIS Configuration
```powershell
# Import IIS module
Import-Module WebAdministration

# Create application pool
New-WebAppPool -Name "ZLFileRelay"
Set-ItemProperty IIS:\AppPools\ZLFileRelay -Name managedRuntimeVersion -Value ''
Set-ItemProperty IIS:\AppPools\ZLFileRelay -Name processModel.identityType -Value ApplicationPoolIdentity

# Create website
New-Website -Name "ZLFileRelay" `
    -ApplicationPool "ZLFileRelay" `
    -PhysicalPath "C:\Program Files\ZLFileRelay\WebPortal" `
    -Port 443 `
    -Ssl `
    -HostHeader "filerelay.yourdomain.com"

# Configure authentication
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" `
    -Name enabled `
    -Value $true `
    -PSPath "IIS:\" `
    -Location "ZLFileRelay"

Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" `
    -Name enabled `
    -Value $false `
    -PSPath "IIS:\" `
    -Location "ZLFileRelay"
```

## Firewall Configuration

### Windows Firewall
```powershell
# Allow HTTPS inbound (for web portal)
New-NetFirewallRule -DisplayName "ZL File Relay - HTTPS" `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort 443 `
    -Action Allow

# Allow SSH outbound (for file transfer)
New-NetFirewallRule -DisplayName "ZL File Relay - SSH" `
    -Direction Outbound `
    -Protocol TCP `
    -RemotePort 22 `
    -Action Allow
```

### Enterprise Firewall Request Template
```
Application: ZL File Relay
Purpose: Secure file transfer between DMZ and SCADA networks
Business Owner: [Your Name]
Technical Contact: [Contact Info]

Inbound Rules:
- Source: Any (authenticated users)
- Destination: [Server IP]
- Port: 443/TCP (HTTPS)
- Protocol: HTTPS
- Justification: Web upload portal for authorized users

Outbound Rules:
- Source: [Server IP]
- Destination: [SCADA Server IP]
- Port: 22/TCP (SSH) OR 445/TCP (SMB)
- Protocol: SSH/SCP OR SMB3
- Justification: Automated file transfer to SCADA network
```

## SSL Certificate Configuration

### Option 1: Domain Certificate
1. Request certificate from your CA
2. Install certificate on server
3. Bind certificate to IIS site

### Option 2: Self-Signed Certificate (Development Only)
```powershell
# Create self-signed certificate
$cert = New-SelfSignedCertificate `
    -DnsName "filerelay.yourdomain.com" `
    -CertStoreLocation "cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)

# Bind to IIS
New-WebBinding -Name "ZLFileRelay" `
    -Protocol https `
    -Port 443 `
    -SslFlags 0

$binding = Get-WebBinding -Name "ZLFileRelay" -Protocol https
$binding.AddSslCertificate($cert.Thumbprint, "my")
```

## Verification

### Verify Service is Running
```powershell
Get-Service "ZLFileRelay"
```

### Verify IIS Site is Running
```powershell
Get-Website | Where-Object {$_.Name -eq "ZLFileRelay"}
```

### Test Web Portal
1. Open browser
2. Navigate to `https://your-server/`
3. Sign in with credentials (Entra ID or local account)
4. Verify upload page loads for authorized users

### Test File Transfer
1. Place test file in watched directory
2. Check logs: `C:\FileRelay\logs\`
3. Verify file appears on SCADA server
4. Check transfer completion in logs

## Troubleshooting

### Service Won't Start
- Check Windows Event Log (Application)
- Verify .NET 8.0 Runtime installed
- Check file permissions on directories
- Review service logs in `C:\FileRelay\logs\`

### Web Portal Not Accessible
- Verify IIS is running
- Check bindings in IIS Manager
- Verify authentication is configured (Entra ID or Local Accounts)
- Check firewall rules
- Review IIS logs and application logs

### File Transfer Failing
- Verify network connectivity to SCADA server
- Test SSH connection manually
- Check credentials are correct
- Verify destination directory exists
- Review transfer logs

See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for detailed troubleshooting guide.

## Uninstallation

### Using Installer
1. Control Panel → Programs and Features
2. Select "ZL File Relay"
3. Click Uninstall
4. Follow prompts

### Manual Uninstallation
```powershell
# Stop and remove service
sc.exe stop "ZLFileRelay"
sc.exe delete "ZLFileRelay"

# Remove IIS site
Remove-Website -Name "ZLFileRelay"
Remove-WebAppPool -Name "ZLFileRelay"

# Remove files
Remove-Item "C:\Program Files\ZLFileRelay" -Recurse -Force

# Optionally remove data (logs, uploads)
Remove-Item "C:\FileRelay" -Recurse -Force
Remove-Item "C:\ProgramData\ZLFileRelay" -Recurse -Force
```

## Next Steps

- [Configuration Reference](CONFIGURATION.md)
- [Deployment Guide](DEPLOYMENT.md)
- [User Guide](USER_GUIDE.md)
- [Troubleshooting](TROUBLESHOOTING.md)
