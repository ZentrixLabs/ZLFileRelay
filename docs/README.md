# ZL File Relay Documentation

Welcome to the ZL File Relay documentation. This guide will help you install, configure, and use the system.

## Quick Links

- 🚀 **[Quick Start Guide](getting-started/QUICK_START.md)** - Get up and running in 15 minutes
- 📦 **[Installation Guide](getting-started/INSTALLATION.md)** - Detailed installation instructions
- ⚙️ **[Configuration Reference](configuration/CONFIGURATION.md)** - Complete configuration options
- 🌐 **[Deployment Guide](deployment/DEPLOYMENT.md)** - Production deployment best practices

## Documentation Structure

### 📘 Getting Started
Start here if you're new to ZL File Relay:
- **[Quick Start Guide](getting-started/QUICK_START.md)** - Basic setup and first use
- **[Installation Guide](getting-started/INSTALLATION.md)** - Complete installation instructions

### ⚙️ Configuration
Learn how to configure ZL File Relay:
- **[Configuration Reference](configuration/CONFIGURATION.md)** - All configuration options explained
- **[Credential Management](configuration/CREDENTIAL_MANAGEMENT.md)** - Secure credential handling (SSH keys, passwords)
- **[Branding Customization](configuration/BRANDING.md)** - Customize logos, names, and colors
- **[Security Best Practices](configuration/SECURITY.md)** - Security recommendations and hardening
- **[WinRM Setup](configuration/WINRM_SETUP.md)** - Enable remote management with ConfigTool

### 🚀 Deployment
Deploy ZL File Relay in various scenarios:
- **[Deployment Guide](deployment/DEPLOYMENT.md)** - General deployment instructions
- **[DMZ Deployment](deployment/DMZ_DEPLOYMENT.md)** - Deploy in DMZ/air-gapped environments
- **[Side-by-Side Testing](deployment/SIDE_BY_SIDE_TESTING.md)** - Test alongside existing systems
- **[Production Coexistence](deployment/PRODUCTION_COEXISTENCE.md)** - Run with legacy systems
- **[Quick Reference](deployment/QUICK_REFERENCE.md)** - Quick deployment commands

### 👤 User Guides
How to use ZL File Relay components:
- **[Configuration Tool](user-guides/CONFIG_TOOL.md)** - Use the WPF ConfigTool application
- **[Remote Management](user-guides/REMOTE_MANAGEMENT.md)** - Manage remote servers

### 👨‍💻 Development
For developers and contributors:
- **[Icon Reference](development/ICON_REFERENCE.md)** - Font Awesome icons used in the project
- **[Font Awesome Setup](development/FONTAWESOME_SETUP.md)** - Set up Font Awesome Pro for desktop
- **[Font Awesome Quick Start](development/FONTAWESOME_QUICK_START.md)** - Quick Font Awesome reference

### 📚 Reference
Technical references:
- **[ConfigTool Tab Reference](reference/CONFIG_TOOL_TABS.md)** - Detailed tab-by-tab reference

## Common Tasks

### First Time Setup
1. Read the [Quick Start Guide](getting-started/QUICK_START.md)
2. Follow the [Installation Guide](getting-started/INSTALLATION.md)
3. Configure your [SSH credentials](configuration/CREDENTIAL_MANAGEMENT.md)
4. Test with the [Side-by-Side Testing](deployment/SIDE_BY_SIDE_TESTING.md) guide

### Production Deployment
1. Review [Security Best Practices](configuration/SECURITY.md)
2. Follow the [Deployment Guide](deployment/DEPLOYMENT.md)
3. Set up [Remote Management](user-guides/REMOTE_MANAGEMENT.md) if needed
4. See [DMZ Deployment](deployment/DMZ_DEPLOYMENT.md) for air-gapped environments

### Customization
1. Customize [Branding](configuration/BRANDING.md) (logos, colors, names)
2. Configure [allowed user groups](configuration/CONFIGURATION.md#webportal-settings)
3. Set up [credential encryption](configuration/CREDENTIAL_MANAGEMENT.md)

### Testing Alongside Existing Systems
1. Read [Production Coexistence](deployment/PRODUCTION_COEXISTENCE.md)
2. Follow [Side-by-Side Testing](deployment/SIDE_BY_SIDE_TESTING.md)
3. Use different ports and paths to avoid conflicts

## Support

### Getting Help
- Check the documentation sections above
- Review the [troubleshooting sections](deployment/DEPLOYMENT.md#troubleshooting) in deployment guides
- Check Windows Event Viewer (Application log, source: "ZLFileRelay")
- Review log files in configured log directory

### Contributing
Contributions are welcome! See the development section for technical details.

## Architecture Overview

ZL File Relay consists of three main components:

1. **Windows Service** (`ZLFileRelay.Service`)
   - Monitors directories for new files
   - Transfers files via SSH/SCP or SMB
   - Runs as a background Windows Service
   - Configurable retry logic and error handling

2. **Web Portal** (`ZLFileRelay.WebPortal`)
   - ASP.NET Core web application
   - User-friendly file upload interface
   - Windows Authentication
   - Hosted in IIS

3. **Configuration Tool** (`ZLFileRelay.ConfigTool`)
   - WPF desktop application
   - Unified configuration management
   - Service control (start/stop/restart)
   - Remote server management via WinRM
   - Test connections and credentials

All components share the same configuration file (`appsettings.json`) for consistency.

## Version History

See [CHANGELOG.md](../CHANGELOG.md) in the root directory for version history and release notes.

## License

See [LICENSE](../LICENSE) in the root directory.
