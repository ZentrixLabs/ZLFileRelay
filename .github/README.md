# ZL File Relay

**ZentrixLabs File Relay** - Enterprise File Transfer Solution for Segmented DMZ to Isolated Network Environments

**This project is now deprecated, and a Linux version has been developed with many improvements. Please head over to [N24 Data Relay](https://github.com/Network24Labs/n24-data-relay)

## Overview

ZL File Relay is a unified enterprise solution for secure, automated file transfer between DMZ and SCADA networks. It combines three powerful components into a single deployable package:

1. **🔄 File Transfer Service** - Automated Windows Service that watches directories and securely transfers files via SSH/SCP or SMB
2. **🌐 Web Upload Portal** - User-friendly web interface with hybrid authentication (Entra ID + Local Accounts)
3. **⚙️ Configuration Tool** - Intuitive WPF application for unified configuration and service management

Designed to solve secure file movement across strict network trust boundaries without breaking segmentation policy.

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
- ✅ Hybrid authentication: Entra ID (Azure AD) SSO + Local Accounts
- ✅ Works in air-gapped networks with local authentication
- ✅ Simplified authorization: All authenticated users can upload
- ✅ Multi-file upload support
- ✅ Real-time upload progress tracking
- ✅ User-specific upload directories
- ✅ Kestrel web server with flexible SSL configuration

### Configuration Tool
- ✅ Unified configuration interface for all components
- ✅ SSH key generation and management
- ✅ Service installation and management
- ✅ Entra ID Setup Wizard with automatic hostname detection
- ✅ Certificate Store Browser for SSL configuration
- ✅ Real-time service status monitoring
- ✅ Credential encryption with Windows DPAPI
- ✅ Configuration validation with mutual exclusivity enforcement

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
Architecture supports strict separation between intake zone and destination network, with configuration shared across components via encrypted configuration store.

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

4. Configure **Web Portal Authentication**:
   - Choose authentication method: Entra ID (Azure AD) or Local Accounts
   - For Entra ID: Use Setup Wizard to configure OAuth/OIDC
   - For Local Accounts: Enable user registration
   - Set site name and branding
   - Configure SSL certificate (via Certificate Store Browser)

5. **Install & Start Services**:
   - Click "Install Service" to register Windows Service
   - Start the Web Portal (runs on Kestrel)
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
    "WebPortal": {
      "Authentication": {
        "EnableEntraId": true,
        "EnableLocalAccounts": false,
        "EntraIdTenantId": "your-tenant-id",
        "EntraIdClientId": "your-client-id"
      }
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

## Design Principles
•	Explicit trust boundaries

•	Least-privilege service execution

•	Deterministic file transfer behavior

•	Auditable operations

•	Secure defaults over convenience

## Security Features

- 🔐 **Windows DPAPI Encryption** - Credentials encrypted at rest
- 🔑 **SSH Key Authentication** - Public key auth preferred over passwords
- 🛡️ **Hybrid Authentication** - Entra ID (Azure AD) OAuth/OIDC + Local Accounts with ASP.NET Core Identity
- 🔒 **Authorization Code Flow** - Secure OAuth 2.0 flow for Entra ID
- ✅ **File Integrity Verification** - SHA-256 checksums for all transfers
- 📝 **Comprehensive Audit Logging** - All operations logged for security monitoring
- 🚧 **Input Validation** - All inputs sanitized and validated
- 🔒 **Secure Defaults** - Security-first configuration out of the box
- 🌐 **SSL/TLS Support** - Certificate store integration for secure HTTPS

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

📚 **[Documentation Hub](docs/)** - Everything you need to get started

### Essential Guides

| Guide | Description |
|-------|-------------|
| 📖 **[Setup Guide](docs/SETUP.md)** | Complete installation and configuration guide |
| 🔐 **[Entra ID Setup](docs/ENTRA_ID_SETUP.md)** | Azure AD (Entra ID) authentication configuration |
| 🔑 **[SSH Target Server](docs/SSH_TARGET_SERVER.md)** | Setting up SSH on destination servers |

### Quick Navigation

**New Installation?** → Start with [Setup Guide](docs/SETUP.md)

**Azure AD Authentication?** → See [Entra ID Setup](docs/ENTRA_ID_SETUP.md)

**SSH Configuration?** → See [SSH Target Server](docs/SSH_TARGET_SERVER.md)

**Troubleshooting?** → Check the Troubleshooting sections in each guide above

## Support

For technical support:
- 📖 Documentation: See `docs/` folder
- 🐛 Issues: GitHub Issues
- 📝 Logs: Check `C:\FileRelay\logs` or Windows Event Log

## License

Copyright © 2025 ZentrixLabs
Licensed under the GNU Lesser General Public License v3.0 or later (LGPL-3.0-or-later) – see [LICENSE](LICENSE) for details.

## Version History

### Version 2.0.0 (Current)
- ✨ Unified product combining Service + Web Portal
- ⬆️ Upgraded to .NET 8.0
- 🔐 **NEW:** Hybrid authentication (Entra ID + Local Accounts)
- 🌐 **NEW:** Switched from HTTP.sys to Kestrel for flexibility
- 🧙 **NEW:** Entra ID Setup Wizard with automatic hostname detection
- 🎨 Professional branding and configuration
- 📦 Single installer for all components
- ⚙️ Unified configuration tool
- 🔄 Improved retry logic and error handling
- 📝 Enhanced logging and monitoring

### Version 1.x (Legacy)
- Separate DMZFileTransferService and DMZUploader products

---

**ZL File Relay** - Secure, Reliable, Professional File Transfer for Industrial Environments
