# Changelog

All notable changes to ZL File Relay will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Released]

## [1.1.1] - 2025-10-31

### Fixed
- Upload settings not respecting app config for large uploads

### Fixed
- **Upload Timeout Issue** - Fixed timeouts on large file uploads (multi-GB files)
  - Disabled Kestrel's minimum data rate requirement (was 240 bytes/sec)
  - Increased keep-alive timeout from 130 seconds to 10 minutes
  - Increased request headers timeout to 2 minutes
  - Configured form options for large multipart uploads
  - Now supports slow connections and multi-hour uploads
  - See `docs/UPLOAD_TIMEOUT_FIX.md` for detailed information

## [1.1.0] - 2025-10-31

### 🚀 Major Architecture Changes

#### Changed - Web Portal Hosting (Breaking Change)
- **REMOVED: IIS dependency** - Web Portal no longer requires IIS installation
- **ADDED: Kestrel standalone mode** - Web Portal now runs as a native Windows Service using Kestrel
- Web Portal runs on configurable ports (default: 8080 HTTP, 8443 HTTPS)
- HTTP.sys URL reservations managed automatically by ConfigTool
- Perfect for DMZ/OT/Server Core environments where IIS is not available
- Self-hosted architecture eliminates IIS configuration complexity
- SSL certificate management handled via ConfigTool (optional)

#### Changed - Authentication System (Breaking Change)
- **REMOVED: Windows Authentication only** - No longer limited to domain-joined scenarios
- **ADDED: Hybrid authentication** - Choose between Local Accounts, Entra ID (Microsoft 365), or both
- **Local Accounts** - Built-in user database with SQLite storage
  - User registration with optional admin approval
  - Role-based access control (Admin/User roles)
  - Perfect for air-gapped/DMZ environments with no domain
  - Works completely offline with no internet dependency
- **Entra ID (Microsoft 365)** - Cloud-based SSO
  - Single Sign-On with Microsoft accounts
  - Azure AD integration via OAuth 2.0/OpenID Connect
  - No domain trust required across DMZ boundaries
  - Automatic user synchronization
- **Flexible deployment** - Enable local accounts, Entra ID, or both simultaneously
- ConfigTool wizard for easy authentication setup

### Added

#### Authentication Features
- SQLite-based local user database
- User registration page (can be disabled)
- Admin approval workflow for new accounts (optional)
- Role-based authorization (Admin, User)
- Entra ID configuration wizard in ConfigTool
- Authentication fallback (if Entra ID unavailable, local accounts work)
- Session management with configurable timeout
- "Remember me" functionality for local accounts

#### Configuration Tool Enhancements
- Web Portal authentication configuration tab
- Entra ID setup wizard (clientId, tenantId, clientSecret)
- Local accounts configuration (enable/disable registration, require approval)
- Test authentication button
- HTTP.sys URL reservation management
- SSL certificate import and binding (optional HTTPS)
- Web Portal service management

#### Deployment Improvements
- No IIS required for any deployment scenario
- Works on Windows Server Core without GUI
- Air-gapped environment support (local accounts only mode)
- Simplified firewall rules (just ports 8080/8443)
- Self-contained .NET 8 runtime included
- Automatic service installation and configuration

#### Code Signing & Build Automation
- Automated version update script (`update-version.ps1`)
- Complete code signing infrastructure for all executables
- Build, sign, and release scripts following industry best practices
- Automated installer creation workflow

### Changed

#### Web Portal
- Runs as Windows Service `ZLFileRelay.WebPortal` instead of IIS application
- Default ports: 8080 (HTTP), 8443 (HTTPS) - configurable
- Authentication UI updated with local/Entra ID login options
- Registration page added (optional)
- User profile management
- Admin interface for user approval (if enabled)

#### Configuration
- New `Authentication` section in `appsettings.json`
  - `AuthMode`: LocalAccounts, EntraId, or Hybrid
  - `AllowRegistration`: Enable/disable user registration
  - `RequireApproval`: Require admin approval for new users
  - `EntraId`: ClientId, TenantId, ClientSecret configuration
- New `WebPortal` section enhancements
  - `HttpPort`: Configurable HTTP port (default: 8080)
  - `HttpsPort`: Configurable HTTPS port (default: 8443)
  - `CertificateThumbprint`: Optional SSL certificate

#### Installer
- Removed IIS prerequisites check
- Added HTTP.sys URL reservation setup
- Added firewall rules for ports 8080/8443
- Web Portal installs as Windows Service
- Updated installation prompts

### Removed

#### Breaking Changes
- **IIS support completely removed** - No longer an option
- **Windows Authentication removed** - Replaced with hybrid auth
- **Active Directory requirement removed** - No longer needed
- IIS configuration scripts removed
- Windows Authentication middleware removed
- Domain dependency eliminated

### Fixed
- Cross-domain authentication issues (DMZ to corporate)
- Air-gapped deployment authentication (now works with local accounts)
- Server Core compatibility (no IIS needed)
- Certificate management complexity (simplified via ConfigTool)

### Migration from 1.0.0

#### For Existing Installations

**If using IIS deployment:**
1. Uninstall ZL File Relay 1.0.0
2. Remove IIS site configuration (if desired)
3. Install ZL File Relay 1.1.0
4. Web Portal will install as Windows Service
5. Configure authentication:
   - For domain environments: Use Entra ID
   - For air-gapped/DMZ: Use Local Accounts
   - For hybrid: Enable both

**Authentication migration:**
- Previous Windows Authentication users: Switch to Entra ID (if cloud-connected)
- Air-gapped users: Use Local Accounts (create users via registration or ConfigTool)
- No automatic user migration (users need to re-register or use Entra ID)

**Configuration changes:**
- Update `appsettings.json` with new `Authentication` section
- Update `WebPortal` section with port configuration
- Run ConfigTool to configure authentication via wizard

### Security

#### Enhancements
- Local account passwords hashed with ASP.NET Core Identity (PBKDF2)
- Entra ID OAuth 2.0 tokens encrypted at rest
- Session cookies are HTTP-only and secure (if HTTPS enabled)
- Role-based authorization prevents privilege escalation
- Admin approval workflow prevents unauthorized access

#### Notes
- **Air-gapped environments**: Local Accounts provide full authentication without internet
- **Cloud-connected environments**: Entra ID provides SSO and centralized user management
- **Hybrid deployments**: Enable both for maximum flexibility

### Technical Details
- ASP.NET Core Identity for local user management
- Microsoft.Identity.Web for Entra ID integration
- SQLite database for local accounts (located in C:\ProgramData\ZLFileRelay)
- HTTP.sys for URL reservations (managed by ConfigTool)
- Kestrel web server (production-ready, high-performance)
- Font Awesome Free (migrated from Pro for open source compatibility)

---

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

### Version 1.1.0 (October 31, 2025)
**Major Architecture Update - Kestrel & Hybrid Authentication**

**Breaking Changes:**
- Web Portal no longer uses IIS - now runs as Windows Service with Kestrel
- Windows Authentication removed - replaced with Local Accounts and/or Entra ID
- Active Directory no longer required

**Key Features:**
- Kestrel-based web hosting (no IIS dependency)
- Hybrid authentication: Local Accounts + Entra ID (Microsoft 365)
- Perfect for air-gapped/DMZ environments (local accounts work offline)
- SQLite user database for local authentication
- User registration with optional admin approval
- Role-based access control
- Simplified deployment (no IIS configuration)
- Works on Windows Server Core without GUI

**Why This Update:**
This release eliminates two major deployment barriers:
1. **IIS Requirement** - Many DMZ/OT environments don't allow IIS or prefer simpler architectures
2. **Domain Dependency** - Cross-domain authentication in DMZ scenarios was complex; local accounts provide offline capability

**Migration Required:** Existing 1.0.0 users must uninstall and reinstall. Users will need to re-authenticate using local accounts or Entra ID.

---

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

