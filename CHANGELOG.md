# Changelog

All notable changes to ZL File Relay will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-10-08

### Added

#### Core Components
- **ZLFileRelay.Service** - Windows service for automated file watching and transfer
- **ZLFileRelay.WebPortal** - ASP.NET Core web portal for file uploads (Kestrel standalone mode)
- **ZLFileRelay.ConfigTool** - WPF configuration tool for unified management
- **ZLFileRelay.Core** - Shared library with models, interfaces, and services

#### Features
- SSH/SCP file transfer with ED25519 key authentication
- SMB/CIFS file transfer support
- File watching with stability detection
- Automatic retry with exponential backoff
- Archive after successful transfer
- Comprehensive audit logging with Serilog
- Windows DPAPI credential encryption
- Health monitoring and status endpoints
- Multi-transfer concurrency support
- Disk space checking before transfer

#### Configuration Tool Features
- Service management (install/start/stop/uninstall)
- SSH key generation (ED25519)
- SSH connection testing
- Service account management
- Folder permission management
- Complete configuration management (all settings)
- Configuration import/export (JSON)
- **Remote management support** - Manage Server Core installations from workstation
- **Multi-server support** - Switch between managing different servers

#### Web Portal Features
- Drag-and-drop file upload
- Multiple file upload support
- Real-time progress tracking
- Upload to transfer queue
- Windows Authentication
- Configurable file size limits
- Modern responsive UI
- Upload history display

#### Deployment Options
- **Full Installation** - Service + WebPortal + ConfigTool (traditional servers)
- **Server Core Installation** - Service + WebPortal only (no GUI)
- **ConfigTool Only** - Just ConfigTool for remote management
- **Custom Installation** - Select components individually
- Self-contained deployment with .NET 8 runtime included
- No external dependencies required
- Air-gapped/DMZ environment support

#### Documentation
- Comprehensive installation guide
- Configuration reference
- Deployment guide (self-contained)
- Remote management guide
- IIS deployment guide (DMZ/OT scenarios)
- ConfigTool quick start guide
- API documentation

#### Security
- Windows DPAPI encryption for credentials
- SSH public key authentication (preferred)
- Windows Authentication for web portal
- Audit logging for security events
- Input validation throughout
- Secure credential storage
- HTTPS support for web portal (with certificate)

### Technical Details
- Built on .NET 8.0
- ASP.NET Core 8.0 (Web Portal)
- WPF .NET 8.0 (ConfigTool)
- SSH.NET for SSH/SCP operations
- Serilog for structured logging
- Modern C# features (nullable reference types, pattern matching)
- MVVM architecture in ConfigTool
- Dependency injection throughout
- Async/await for all I/O operations

### Deployment Scenarios Supported
- Windows Server 2016+ (with GUI)
- Windows Server Core 2016+
- Air-gapped environments
- DMZ/OT networks
- Multi-site deployments
- Centralized management from admin workstation

## [Unreleased]

### Planned Features
- REST API for configuration (alternative to file-based)
- Multi-server dashboard
- PowerShell module for automation
- Batch operations across multiple servers
- Enhanced SSH key management via PowerShell Remoting
- Web-based configuration UI
- Email notifications for transfer failures
- File transfer scheduling
- Bandwidth throttling
- File filtering by pattern

---

## Version History

### Version 1.0.0 (October 8, 2025)
**Initial Release**

Built from ground-up as a unified enterprise solution combining:
- Legacy DMZFileTransferService/ZLBridge (Windows Service)
- Legacy DMZUploader (Web Portal)
- New unified ConfigTool
- Complete remote management capability

**Key Improvements Over Legacy:**
- Modern .NET 8.0 (from .NET Framework 4.8)
- Fully configurable (no hardcoded values)
- Unified configuration across all components
- Single installer for all components
- Remote management support (Server Core ready)
- Professional architecture with SOLID principles
- Comprehensive documentation
- Production-ready with proper error handling

**Migration Path:**
Organizations can migrate from legacy DMZ solutions to this unified platform with improved maintainability, better security, and enterprise deployment support.

---

## Links
- [Installation Guide](docs/INSTALLATION.md)
- [Configuration Reference](docs/CONFIGURATION.md)
- [Remote Management Guide](docs/REMOTE_MANAGEMENT.md)
- [Quick Start Guide](docs/CONFIGTOOL_QUICK_START.md)
- [GitHub Repository](https://github.com/yourcompany/ZLFileRelay)

---

## Support
For issues, feature requests, or questions:
- Open an issue on GitHub
- Contact: support@example.com
- Documentation: See `docs/` folder

---

**Legend:**
- `Added` - New features
- `Changed` - Changes in existing functionality
- `Deprecated` - Soon-to-be removed features
- `Removed` - Removed features
- `Fixed` - Bug fixes
- `Security` - Security fixes

