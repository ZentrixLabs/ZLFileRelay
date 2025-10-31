# ZL File Relay Setup Guide

Complete guide for installing and configuring ZL File Relay.

---

## Table of Contents

1. [System Requirements](#system-requirements)
2. [Installation](#installation)
3. [Initial Configuration](#initial-configuration)
4. [Authentication Setup](#authentication-setup)
5. [Transfer Configuration](#transfer-configuration)
6. [Service Management](#service-management)
7. [Testing Your Setup](#testing-your-setup)
8. [Troubleshooting](#troubleshooting)

---

## System Requirements

### Server Requirements
- **OS**: Windows Server 2019 or later (2022 recommended)
- **RAM**: 4 GB minimum, 8 GB recommended
- **Disk**: 10 GB free space minimum
- **Network**: Access to target transfer destination (SSH/SMB)

### Software Prerequisites
- **.NET 8.0 Runtime** (included in self-contained installer)
- **ASP.NET Core 8.0 Runtime** (included in self-contained installer)
- **Administrative privileges** for installation

### Optional Requirements
- **SSL Certificate** for HTTPS (can use Windows certificate store)
- **Azure AD Tenant** if using Entra ID authentication

---

## Installation

### 1. Download Installer

Download `ZLFileRelay-Setup.exe` from your distribution source.

### 2. Run Installer

1. **Right-click** `ZLFileRelay-Setup.exe` → **Run as Administrator**
2. Click **Next** on welcome screen
3. **Accept** license agreement
4. **Select installation directory** (default: `C:\Program Files\ZLFileRelay`)
5. **Select components** to install:
   - ☑️ **File Transfer Service** (recommended)
   - ☑️ **Web Upload Portal** (recommended)
   - ☑️ **Configuration Tool** (recommended)
6. Click **Install**
7. Wait for installation to complete
8. Click **Finish**

### 3. Post-Installation

After installation:
- Configuration Tool shortcut added to Desktop
- Services installed but **not started** (configure first)
- Configuration file created at: `C:\ProgramData\ZLFileRelay\appsettings.json`
- Installation directory: `C:\Program Files\ZLFileRelay`

---

## Initial Configuration

### Launch Configuration Tool

1. **Right-click** Desktop shortcut → **Run as Administrator**
2. The Dashboard tab shows service status (both services should be stopped)

### Configure Paths

1. Go to **File Transfer** tab
2. Set directories:
   - **Upload Directory**: `C:\FileRelay\uploads` (where web portal saves files)
   - **Transfer Directory**: `C:\FileRelay\uploads\transfer` (where service watches for files)
   - **Archive Directory**: `C:\FileRelay\archive` (where completed transfers are moved)
   - **Log Directory**: `C:\FileRelay\logs` (application logs)
3. Click **Save Configuration**

> **Note**: Directories are created automatically on first run if they don't exist.

---

## Authentication Setup

The Web Portal supports two authentication methods. Choose one:

### Option A: Entra ID (Azure AD) Authentication

**Best for**: Organizations with Microsoft 365/Azure AD

1. Go to **Web Portal** tab
2. Under **Authentication**, check **Enable Entra ID**
3. Click **📋 Open Azure Portal - Setup Guide** button
4. Follow the wizard to:
   - Get Tenant ID and Client ID
   - Configure redirect URIs
   - Create client secret
5. Paste credentials into Configuration Tool
6. Click **Save Configuration**

**Detailed Instructions**: See [ENTRA_ID_SETUP.md](ENTRA_ID_SETUP.md)

### Option B: Local Accounts

**Best for**: Air-gapped networks or standalone deployments

1. Go to **Web Portal** tab
2. Under **Authentication**, check **Enable Local Accounts**
3. Optionally check **Require Admin Approval** if you want to manually approve new users
4. Click **Save Configuration**

> **Note**: Only one authentication method can be enabled at a time.

### SSL Certificate Setup

The Web Portal requires an SSL certificate for HTTPS:

1. Go to **Web Portal** tab → **Kestrel Server Settings**
2. Click **Browse Certificate Store**
3. Select your SSL certificate from the list
4. Certificate thumbprint is automatically filled
5. Click **Save Configuration**

**Alternatively**, you can manually enter:
- **Certificate Thumbprint**: From Windows certificate store
- **Store Location**: `LocalMachine` or `CurrentUser`
- **Store Name**: Usually `My`

---

## Transfer Configuration

Choose your transfer method:

### Option A: SSH/SCP Transfer (Recommended)

**Best for**: Secure transfers to Linux/Unix servers

#### 1. Generate SSH Keys

1. Go to **File Transfer** tab → **SSH Settings**
2. Click **Generate SSH Key Pair**
3. Keys saved to: `C:\ProgramData\ZLFileRelay\.ssh\`
4. Click **Copy Public Key to Clipboard**

#### 2. Configure Target Server

Copy the public key to the target server. See [SSH_TARGET_SERVER.md](SSH_TARGET_SERVER.md) for detailed instructions.

**Quick Steps** (on target server):
```bash
# Create .ssh directory if it doesn't exist
mkdir -p ~/.ssh
chmod 700 ~/.ssh

# Add public key to authorized_keys
echo "YOUR_PUBLIC_KEY_HERE" >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

#### 3. Configure SSH Settings

In Configuration Tool:
1. **SSH Host**: IP or hostname of target server (e.g., `192.168.1.100` or `scada-server.local`)
2. **SSH Port**: Usually `22`
3. **SSH Username**: Username on target server (e.g., `svc_filetransfer`)
4. **Destination Path**: Where files should be copied (e.g., `/data/incoming`)
5. Click **Test Connection** to verify
6. Click **Save Configuration**

### Option B: SMB (Windows File Share)

**Best for**: Transfers to Windows servers

1. Go to **File Transfer** tab
2. Select **Transfer Method**: `SMB`
3. Configure:
   - **SMB Server**: `\\server\share` or `\\192.168.1.100\share`
   - **Share Path**: Path within the share (e.g., `incoming`)
   - **Use Credentials**: Check if authentication required
   - **Username**: Domain\Username (if needed)
   - **Password**: SMB password (encrypted with DPAPI)
4. Click **Save Configuration**

### Retry and Timeout Settings

Configure retry behavior (optional):

- **Retry Attempts**: Number of times to retry failed transfers (default: 3)
- **Retry Delay**: Seconds between retries (default: 30)
- **Connection Timeout**: SSH connection timeout in seconds (default: 30)
- **Operation Timeout**: Transfer operation timeout in seconds (default: 300)

---

## Service Management

### Install Services

1. Go to **Dashboard** tab
2. Click **Install Service** (registers Windows Service)
3. Wait for "Service installed successfully" message

### Start Services

1. **Start Transfer Service**: Click **Start** button in Transfer Service section
2. **Start Web Portal**: Click **Start** button in Web Portal section
3. Verify status shows "Running" for both

### Stop/Restart Services

- **Stop**: Click **Stop** button for the service
- **Restart**: Click **Stop**, wait 2 seconds, then click **Start**

### Windows Services

Services can also be managed via Windows Services:
- **Service Name**: `ZLFileRelay`
- **Display Name**: `ZL File Relay Service`
- **Startup Type**: Automatic (recommended)

To configure:
1. Open **services.msc**
2. Find **ZL File Relay Service**
3. Right-click → **Properties**
4. Set **Startup type** to **Automatic**
5. Set **Log On As** to service account if needed

---

## Testing Your Setup

### Test File Transfer Service

1. **Start the Transfer Service** (if not already running)
2. **Copy a test file** to the transfer directory:
   ```
   C:\FileRelay\uploads\transfer\test.txt
   ```
3. **Watch the logs**:
   - Open: `C:\FileRelay\logs\zlfilerelay-service-[date].log`
   - Look for: `File transferred successfully`
4. **Verify on target**:
   - SSH to target server: `ssh user@server`
   - Check destination path: `ls /data/incoming/`
   - Confirm test.txt exists

### Test Web Portal

1. **Start the Web Portal** (if not already running)
2. **Open browser** and navigate to:
   - HTTPS: `https://localhost:8443`
   - HTTP: `http://localhost:8080`
3. **Sign in**:
   - **Entra ID**: Click "Sign in with Microsoft"
   - **Local Account**: Enter email/password or click "Register"
4. **Upload a test file**:
   - Click "Upload" button
   - Select file
   - Choose destination (DMZ or SCADA)
   - Click "Upload"
5. **Verify upload**:
   - Check upload directory: `C:\FileRelay\uploads\`
   - If SCADA selected, file should be in transfer directory: `C:\FileRelay\uploads\transfer\`
   - File should automatically transfer within seconds

### Expected Behavior

**DMZ Upload** (files stay in DMZ):
- File saved to: `C:\FileRelay\uploads\`
- File does **not** transfer automatically

**SCADA Upload** (files transfer to SCADA):
- File saved to: `C:\FileRelay\uploads\transfer\`
- Service detects file within seconds
- File transferred via SSH/SMB
- Original moved to: `C:\FileRelay\archive\`
- Transfer logged in: `C:\FileRelay\logs\`

---

## Troubleshooting

### Service Won't Start

**Problem**: "Error 1053: The service did not respond in a timely fashion"

**Solution**:
1. Check Event Viewer (Application log) for errors
2. Verify configuration file exists: `C:\ProgramData\ZLFileRelay\appsettings.json`
3. Check file permissions on service directories
4. Ensure .NET 8.0 Runtime is installed

---

### Web Portal Shows Login Error

**Problem**: Can't access web portal at https://localhost:8443

**Solutions**:

1. **Check if service is running**:
   - Open Configuration Tool → Dashboard
   - Verify Web Portal status is "Running"

2. **Check SSL certificate**:
   - Certificate must be in Windows certificate store
   - Certificate must have private key accessible
   - See [ENTRA_ID_SETUP.md](ENTRA_ID_SETUP.md) for certificate troubleshooting

3. **Check firewall**:
   - Windows Firewall may block ports 8080/8443
   - Add inbound rules if needed

4. **Try HTTP instead**:
   - Navigate to: `http://localhost:8080`
   - If this works, SSL certificate is the issue

---

### Entra ID Login Fails

**Problem**: "AADSTS50011: The reply URL specified in the request does not match"

**Solution**:
- Add redirect URI in Azure Portal: `https://your-server:8443/signin-oidc`
- See [ENTRA_ID_SETUP.md](ENTRA_ID_SETUP.md) for detailed troubleshooting

---

### SSH Connection Failed

**Problem**: "Connection timeout" or "Authentication failed"

**Solutions**:

1. **Test SSH manually**:
   ```bash
   ssh -i C:\ProgramData\ZLFileRelay\.ssh\id_rsa user@target-server
   ```

2. **Check SSH key permissions**:
   - Public key must be in target server's `~/.ssh/authorized_keys`
   - File permissions must be: `chmod 600 ~/.ssh/authorized_keys`

3. **Check network connectivity**:
   ```powershell
   Test-NetConnection -ComputerName target-server -Port 22
   ```

4. **Check SSH service on target**:
   ```bash
   sudo systemctl status sshd
   ```

See [SSH_TARGET_SERVER.md](SSH_TARGET_SERVER.md) for more SSH troubleshooting.

---

### Files Not Transferring

**Problem**: Files appear in transfer directory but don't transfer

**Solutions**:

1. **Check service status**:
   - Verify Transfer Service is "Running"

2. **Check logs**:
   - Open: `C:\FileRelay\logs\zlfilerelay-service-[date].log`
   - Look for error messages

3. **Check transfer method configuration**:
   - Verify SSH/SMB settings are correct
   - Test connection from Configuration Tool

4. **Check file permissions**:
   - Service account must have read/write access to directories

---

### SMB Transfer Failed

**Problem**: "Access denied" or "Network path not found"

**Solutions**:

1. **Test SMB share manually**:
   ```powershell
   # Test connectivity
   Test-NetConnection -ComputerName server -Port 445
   
   # Test access
   dir \\server\share
   ```

2. **Check credentials**:
   - Use format: `DOMAIN\Username`
   - Password must be correct

3. **Check SMB version**:
   - Target server must support SMB3
   - Check: `Get-SmbConnection` (PowerShell)

---

### Configuration Changes Not Applied

**Problem**: Changed configuration but service still uses old settings

**Solution**:
1. Click **Save Configuration** in Configuration Tool
2. **Restart** the affected service (Transfer Service or Web Portal)
3. Configuration file updates at: `C:\ProgramData\ZLFileRelay\appsettings.json`

---

### User Can't Upload (Local Accounts)

**Problem**: "Not Authorized" after login

**Solution**:
1. If **Require Approval** is enabled:
   - Admin must approve user in database
   - Currently: Manually edit SQLite database at `C:\ProgramData\ZLFileRelay\webportal.db`
   - Set `IsApproved = 1` for the user
2. Or disable **Require Approval** in Configuration Tool

---

### View Logs

**Service Logs**:
- Location: `C:\FileRelay\logs\zlfilerelay-service-[date].log`
- Rolling daily logs, retained for 30 days

**Web Portal Logs**:
- Location: `C:\FileRelay\logs\zlfilerelay-web-[date].log`
- Rolling daily logs, retained for 30 days

**Windows Event Log**:
- Source: `ZLFileRelay`
- Location: `Application` log
- View: `eventvwr.msc`

---

## Advanced Configuration

### Remote Server Management

To manage ZL File Relay on remote servers using the Configuration Tool:

1. Enable WinRM on target server:
   ```powershell
   Enable-PSRemoting -Force
   ```

2. In Configuration Tool:
   - Go to **Dashboard** → **Remote Connections**
   - Enter server name/IP
   - Enter credentials
   - Click **Connect**

### Service Account Configuration

To run services under a specific service account:

1. Create service account in Active Directory
2. Grant **Log on as a service** right:
   ```powershell
   # Run as Administrator
   secedit /export /cfg C:\temp\secpol.cfg
   # Edit file: Add account to SeServiceLogonRight
   secedit /configure /db secedit.sdb /cfg C:\temp\secpol.cfg
   ```
3. Grant permissions to service directories:
   ```powershell
   icacls "C:\FileRelay" /grant "DOMAIN\ServiceAccount:(OI)(CI)F" /T
   icacls "C:\ProgramData\ZLFileRelay" /grant "DOMAIN\ServiceAccount:(OI)(CI)F" /T
   ```
4. Change service logon via **services.msc** → Properties → Log On

### Multiple Transfer Destinations

To transfer to multiple destinations:

1. **Option 1**: Run multiple instances with different configurations
2. **Option 2**: Use subdirectories in transfer folder (future feature)

---

## Configuration File Reference

Main configuration: `C:\ProgramData\ZLFileRelay\appsettings.json`

**Example configuration**:

```json
{
  "ZLFileRelay": {
    "Branding": {
      "CompanyName": "Your Company",
      "SiteName": "Production Site",
      "SupportEmail": "support@example.com"
    },
    "Paths": {
      "UploadDirectory": "C:\\FileRelay\\uploads",
      "TransferDirectory": "C:\\FileRelay\\uploads\\transfer",
      "ArchiveDirectory": "C:\\FileRelay\\archive",
      "LogDirectory": "C:\\FileRelay\\logs"
    },
    "Service": {
      "Enabled": true,
      "TransferMethod": "ssh",
      "RetryAttempts": 3,
      "RetryDelaySeconds": 30
    },
    "WebPortal": {
      "Enabled": true,
      "Authentication": {
        "EnableEntraId": true,
        "EnableLocalAccounts": false,
        "EntraIdTenantId": "your-tenant-id",
        "EntraIdClientId": "your-client-id",
        "EntraIdClientSecret": "your-client-secret",
        "RequireApproval": false
      },
      "Kestrel": {
        "HttpPort": 8080,
        "HttpsPort": 8443,
        "CertificateThumbprint": "YOUR_CERT_THUMBPRINT",
        "CertificateStoreLocation": "LocalMachine",
        "CertificateStoreName": "My"
      }
    },
    "Transfer": {
      "Ssh": {
        "Host": "192.168.1.100",
        "Port": 22,
        "Username": "svc_filetransfer",
        "PrivateKeyPath": "C:\\ProgramData\\ZLFileRelay\\.ssh\\id_rsa",
        "DestinationPath": "/data/incoming",
        "ConnectionTimeoutSeconds": 30,
        "OperationTimeoutSeconds": 300
      },
      "Smb": {
        "Server": "\\\\server\\share",
        "SharePath": "incoming",
        "UseCredentials": true,
        "Username": "DOMAIN\\ServiceAccount"
      }
    }
  }
}
```

> **Warning**: Do not manually edit passwords/secrets in the config file. Use the Configuration Tool to encrypt credentials properly.

---

## Security Best Practices

1. **Use SSH key authentication** (not passwords)
2. **Enable Entra ID authentication** for enterprise SSO
3. **Use SSL certificates** from trusted CA
4. **Run services under service account** (not local system)
5. **Restrict directory permissions** to service account only
6. **Enable Windows Firewall** rules only for necessary ports
7. **Monitor logs regularly** for suspicious activity
8. **Rotate client secrets** annually (for Entra ID)
9. **Keep .NET runtime updated** via Windows Update
10. **Disable local accounts** if using Entra ID exclusively

---

## Getting Help

**Documentation**:
- Entra ID Setup: [ENTRA_ID_SETUP.md](ENTRA_ID_SETUP.md)
- SSH Server Setup: [SSH_TARGET_SERVER.md](SSH_TARGET_SERVER.md)

**Logs**:
- Service: `C:\FileRelay\logs\zlfilerelay-service-[date].log`
- Web Portal: `C:\FileRelay\logs\zlfilerelay-web-[date].log`
- Windows Events: `eventvwr.msc` → Application log → Source: ZLFileRelay

**Support**:
- Email: support@yourdomain.com
- Configuration file: `C:\ProgramData\ZLFileRelay\appsettings.json`

---

**ZL File Relay** - Secure, Reliable File Transfer for Industrial Environments

