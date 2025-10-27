# ZL File Relay

**ZentrixLabs File Relay** - Enterprise File Transfer Solution for DMZ to SCADA Networks

## Overview

ZL File Relay is a unified enterprise solution for secure, automated file transfer between DMZ and SCADA networks. It combines three powerful components into a single deployable package:

1. **🔄 File Transfer Service** - Automated Windows Service that watches directories and securely transfers files via SSH/SCP or SMB
2. **🌐 Web Upload Portal** - User-friendly web interface for file uploads with Windows Authentication
3. **⚙️ Configuration Tool** - Intuitive WPF application for unified configuration and service management

## Key Features

### File Transfer Service
- ✅ Real-time file system monitoring with `FileSystemWatcher`
- ✅ Secure SSH/SCP transfer (primary method)
- ✅ SMB3 fallback with authenticated connections
- ✅ Automatic retry logic with exponential backoff
- ✅ File integrity verification
- ✅ Comprehensive audit logging
- ✅ Runs as Windows Service for reliability

### Web Upload Portal
- ✅ Modern responsive web interface
- ✅ Windows Authentication (NTLM/Kerberos)
- ✅ Active Directory group authorization
- ✅ Multi-file upload support
- ✅ Upload progress tracking
- ✅ User-specific upload directories
- ✅ IIS hosted for enterprise reliability

### Configuration Tool
- ✅ Unified configuration interface for both components
- ✅ SSH key generation and management
- ✅ Service installation and management
- ✅ IIS configuration automation
- ✅ Real-time service status monitoring
- ✅ Credential encryption with Windows DPAPI
- ✅ Configuration validation

## Architecture

```
┌──────────────────────────────────────────────────────────┐
│                    ZL File Relay                          │
├──────────────────────────────────────────────────────────┤
│                                                           │
│  ┌────────────────┐         ┌──────────────────┐        │
│  │  Web Portal    │         │  Transfer Service│        │
│  │  (IIS/ASP.NET) │         │  (Windows Service)│        │
│  └────────┬───────┘         └────────┬─────────┘        │
│           │                          │                   │
│           └──────────┬───────────────┘                   │
│                      │                                   │
│           ┌──────────▼──────────┐                        │
│           │  Shared Config      │                        │
│           │  (appsettings.json) │                        │
│           └─────────────────────┘                        │
└──────────────────────────────────────────────────────────┘
                      │
                      │ SSH/SCP or SMB
                      ▼
          ┌───────────────────────┐
          │   SCADA File Server   │
          └───────────────────────┘
```

## Quick Start

### System Requirements
- Windows Server 2019 or later (2022 recommended)
- .NET 8.0 Runtime (included in self-contained deployment)
- ASP.NET Core 8.0 Runtime (for web portal)
- Administrative privileges for installation

### Installation

1. **Download** the installer: `ZLFileRelay-Setup.exe`
2. **Run as Administrator** to begin installation
3. **Select Components** during installation:
   - File Transfer Service (recommended)
   - Web Upload Portal (requires IIS)
   - Configuration Tool (recommended)
4. **Complete** installation wizard
5. **Launch** Configuration Tool from Desktop or Start Menu

### Initial Configuration

1. Open **ZL File Relay Configuration Tool** as Administrator
2. Configure **Upload Paths**:
   - Upload Directory: Where files are saved (default: `C:\FileRelay\uploads`)
   - Transfer Directory: Where service watches for files (default: `C:\FileRelay\uploads\transfer`)
   - Log Directory: Log file location (default: `C:\FileRelay\logs`)

3. Configure **SSH Transfer** (recommended):
   - Click **Generate SSH Keys**
   - Copy public key to SCADA server (`~/.ssh/authorized_keys`)
   - Enter SSH host, username, and destination path
   - Test connection

4. Configure **Web Portal**:
   - Set site name and branding
   - Configure Active Directory groups for access
   - Set upload limits and policies

5. **Install & Start Services**:
   - Click "Install Service" to register Windows Service
   - Click "Configure IIS" to set up web portal
   - Click "Start Service" to begin file monitoring

## Configuration

### Shared Configuration File
All components share a unified `appsettings.json` file located at:
```
C:\ProgramData\ZLFileRelay\appsettings.json
```

### Key Settings

```json
{
  "ZLFileRelay": {
    "Branding": {
      "CompanyName": "Your Company",
      "SiteName": "Your Site",
      "SupportEmail": "support@example.com"
    },
    "Paths": {
      "UploadDirectory": "C:\\FileRelay\\uploads",
      "LogDirectory": "C:\\FileRelay\\logs"
    },
    "Service": {
      "TransferMethod": "ssh",
      "RetryAttempts": 3
    },
    "Transfer": {
      "Ssh": {
        "Host": "scada-server.example.com",
        "Port": 22,
        "Username": "svc_filetransfer",
        "DestinationPath": "/data/incoming"
      }
    }
  }
}
```

See [Configuration Reference](docs/configuration/CONFIGURATION.md) for complete details.

## Security Features

- 🔐 **Windows DPAPI Encryption** - Credentials encrypted at rest
- 🔑 **SSH Key Authentication** - Public key auth preferred over passwords
- 🛡️ **Windows Authentication** - Web portal integrated with Active Directory
- ✅ **File Integrity Verification** - SHA-256 checksums for all transfers
- 📝 **Comprehensive Audit Logging** - All operations logged for security monitoring
- 🚧 **Input Validation** - All inputs sanitized and validated
- 🔒 **Secure Defaults** - Security-first configuration out of the box

## Deployment Scenarios

### Scenario 1: DMZ to SCADA Transfer
Users upload files via web portal → Service automatically transfers to SCADA network

### Scenario 2: Automated Directory Monitoring
Applications drop files in monitored directory → Service transfers automatically

### Scenario 3: Multi-Site Deployment
Deploy at multiple sites with site-specific configurations

See [Deployment Guide](docs/deployment/DEPLOYMENT.md) for detailed deployment scenarios.

## Project Structure

```
ZLFileRelay/
├── src/
│   ├── ZLFileRelay.Core/           # Shared models and services
│   ├── ZLFileRelay.Service/        # Windows Service
│   ├── ZLFileRelay.WebPortal/      # ASP.NET Core web app
│   └── ZLFileRelay.ConfigTool/     # WPF configuration tool
├── installer/
│   ├── ZLFileRelay.iss             # Inno Setup installer
│   └── scripts/                    # Installation scripts
├── docs/                           # Documentation
└── tests/                          # Unit tests
```

## Building from Source

For a complete build that creates an installer:

```powershell
# Clone repository
git clone https://github.com/your-org/ZLFileRelay.git
cd ZLFileRelay

# Build installer (requires Inno Setup and .NET 8 SDK)
.\build\build-installer.ps1
```

This will:
1. Publish all components with .NET 8 runtime included
2. Create installer in `installer/output/`

**📖 See [Build Process Guide](docs/development/BUILD_PROCESS.md) for complete details**

### Quick Commands

```powershell
# Just build projects (no installer)
dotnet build --configuration Release

# Run tests
dotnet test

# Run ConfigTool for development
dotnet run --project src/ZLFileRelay.ConfigTool

# Run WebPortal for development
dotnet run --project src/ZLFileRelay.WebPortal
```

## Documentation

📚 **[Complete Documentation](docs/)** - All guides organized by category

### Quick Access
- 🚀 **[Quick Start](docs/getting-started/QUICK_START.md)** - Get started in 15 minutes
- 📦 **[Installation](docs/getting-started/INSTALLATION.md)** - Step-by-step installation
- ⚙️ **[Configuration](docs/configuration/CONFIGURATION.md)** - Configuration reference
- 🚀 **[Deployment](docs/deployment/DEPLOYMENT.md)** - Production deployment
- 🔐 **[Security](docs/configuration/SECURITY.md)** - Security best practices
- 👤 **[User Guides](docs/user-guides/)** - Using ConfigTool and Web Portal
- 🌐 **[DMZ Deployment](docs/deployment/DMZ_DEPLOYMENT.md)** - Air-gapped deployment
- 🧪 **[Testing Guide](docs/deployment/SIDE_BY_SIDE_TESTING.md)** - Test alongside existing systems

### Documentation Structure
```
docs/
├── getting-started/     # Installation and quick start
├── configuration/       # Configuration and security
├── deployment/          # Deployment scenarios
├── user-guides/        # End-user documentation
├── development/        # Developer resources
└── reference/          # Technical references
```

### ⚠️ Remote Management Note
To manage remote servers with ConfigTool, **WinRM must be enabled** on target servers. See [WinRM Setup Guide](docs/configuration/WINRM_SETUP.md) for details.

## Support

For technical support:
- 📧 Email: support@yourdomain.com
- 📖 Documentation: See `docs/` folder
- 🐛 Issues: GitHub Issues
- 📝 Logs: Check `C:\FileRelay\logs` or Windows Event Log

## License

Copyright © 2025 ZentrixLabs
Licensed under the MIT License - see [LICENSE](LICENSE) for details.

## Version History

### Version 2.0.0 (Current)
- ✨ Unified product combining Service + Web Portal
- ⬆️ Upgraded to .NET 8.0
- 🎨 Professional branding and configuration
- 📦 Single installer for all components
- ⚙️ Unified configuration tool
- 🔄 Improved retry logic and error handling
- 📝 Enhanced logging and monitoring

### Version 1.x (Legacy)
- Separate DMZFileTransferService and DMZUploader products

---

**ZL File Relay** - Secure, Reliable, Professional File Transfer for Industrial Environments
