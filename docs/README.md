# ZL File Relay Documentation

## 📚 Essential Guides

Start here for common tasks:

| Guide | Description |
|-------|-------------|
| **[Installation](INSTALLATION.md)** | Install ZL File Relay (5-10 minutes) |
| **[Quick Start](CONFIGTOOL_QUICK_START.md)** | Configure and start using the system |
| **[Configuration Reference](CONFIGURATION.md)** | Complete settings reference |
| **[Deployment Guide](DEPLOYMENT.md)** | Deployment scenarios and architecture |

---

## 🚀 Quick Start Path

**New to ZL File Relay?** Follow this path:

```
1. README (../README.md)           → Understand what it does
2. INSTALLATION.md                 → Install the software  
3. CONFIGTOOL_QUICK_START.md      → Configure SSH and settings
4. Test upload and transfer        → Done!
```

**Time to production:** ~30 minutes

---

## 📖 All Documentation

### Core Documentation

**[Installation Guide](INSTALLATION.md)**
- System requirements
- Installation types (Full, Server Core, ConfigTool Only)
- Step-by-step installation
- Verification

**[Configuration Reference](CONFIGURATION.md)**
- Complete settings reference
- Branding customization
- SSH/SCP setup
- SMB setup
- Security settings
- Web portal settings
- Logging configuration

**[ConfigTool Quick Start](CONFIGTOOL_QUICK_START.md)**
- Configuration tool overview
- Common tasks
- SSH key generation
- Service management
- Testing connections
- Troubleshooting

**[Deployment Guide](DEPLOYMENT.md)** ⭐ **Updated!**
- Architecture overview (Kestrel standalone - no IIS!)
- Network scenarios (DMZ, OT, Multi-site)
- Installation types
- Port configuration
- HTTPS setup
- Firewall rules
- Security considerations
- Post-deployment verification

---

### Advanced Features

**[Remote Management](REMOTE_MANAGEMENT.md)**
- Managing Server Core installations
- Remote connection setup
- Multi-server management
- Requirements and permissions
- Troubleshooting remote connections
- Deployment patterns

**[Credential Management](CREDENTIAL_MANAGEMENT.md)** ⚠️ **Important!**
- Understanding credential types
- Remote management credentials
- Service account credentials
- Security best practices
- Troubleshooting credential issues

**[Deployment Quick Reference](DEPLOYMENT_QUICK_REFERENCE.md)**
- Command cheatsheet
- Quick configuration snippets
- Firewall rules
- Common tasks

---

### Reference

**[Documentation Structure](DOCUMENTATION_STRUCTURE.md)**
- How documentation is organized
- Finding information quickly
- Document maintenance

---

## 🎯 By Task

### Installation & Setup
- **Install software** → [INSTALLATION.md](INSTALLATION.md)
- **First-time setup** → [CONFIGTOOL_QUICK_START.md](CONFIGTOOL_QUICK_START.md)
- **Configure settings** → [CONFIGURATION.md](CONFIGURATION.md)

### Deployment
- **Deploy standard installation** → [DEPLOYMENT.md](DEPLOYMENT.md) → Full Installation
- **Deploy to Server Core** → [DEPLOYMENT.md](DEPLOYMENT.md) → Server Core + [REMOTE_MANAGEMENT.md](REMOTE_MANAGEMENT.md)
- **Deploy to DMZ/OT** → [DEPLOYMENT.md](DEPLOYMENT.md) → Network Scenarios
- **Deploy multi-site** → [REMOTE_MANAGEMENT.md](REMOTE_MANAGEMENT.md) → Multi-Server

### Configuration
- **Generate SSH keys** → [CONFIGTOOL_QUICK_START.md](CONFIGTOOL_QUICK_START.md) → SSH Keys
- **Configure transfer** → [CONFIGURATION.md](CONFIGURATION.md) → SSH/SMB Settings
- **Change web portal port** → [DEPLOYMENT.md](DEPLOYMENT.md) → Port Configuration
- **Enable HTTPS** → [DEPLOYMENT.md](DEPLOYMENT.md) → HTTPS Configuration

### Management
- **Manage services** → [CONFIGTOOL_QUICK_START.md](CONFIGTOOL_QUICK_START.md) → Service Management
- **Manage remote server** → [REMOTE_MANAGEMENT.md](REMOTE_MANAGEMENT.md)
- **Understand credentials** → [CREDENTIAL_MANAGEMENT.md](CREDENTIAL_MANAGEMENT.md)
- **View logs** → Check `C:\FileRelay\logs\`
- **Troubleshoot** → Check relevant guide's troubleshooting section

---

## 🏗️ By Role

### End User
```
1. Access web portal: http://server:8080
2. Upload files
3. Done!
```

### Administrator
```
Start Here:
├── INSTALLATION.md (install)
├── CONFIGTOOL_QUICK_START.md (configure)
└── DEPLOYMENT.md (understand architecture)
```

### Developer
```
Start Here:
├── ../README.md (project overview)
├── ../GETTING_STARTED.md (development setup)
└── CONFIGURATION.md (settings reference)
```

### Security Team
```
Review:
├── DEPLOYMENT.md (security considerations)
├── CONFIGURATION.md (security settings)
├── CREDENTIAL_MANAGEMENT.md (credential security) ⚠️
└── REMOTE_MANAGEMENT.md (access requirements)
```

---

## 🌐 By Environment

### Windows Server (GUI)
**Deployment:** Full Installation  
**Guides:**
1. [INSTALLATION.md](INSTALLATION.md)
2. [CONFIGTOOL_QUICK_START.md](CONFIGTOOL_QUICK_START.md)

### Windows Server Core
**Deployment:** Server Core + ConfigTool on workstation  
**Guides:**
1. [DEPLOYMENT.md](DEPLOYMENT.md) → Server Core Installation
2. [REMOTE_MANAGEMENT.md](REMOTE_MANAGEMENT.md)

### DMZ/OT Network
**Deployment:** Server Core (air-gapped)  
**Guides:**
1. [DEPLOYMENT.md](DEPLOYMENT.md) → Network Scenarios → DMZ
2. [REMOTE_MANAGEMENT.md](REMOTE_MANAGEMENT.md)

### Multi-Site
**Deployment:** Server Core at each site + ConfigTool centrally  
**Guides:**
1. [DEPLOYMENT.md](DEPLOYMENT.md) → Multi-Site
2. [REMOTE_MANAGEMENT.md](REMOTE_MANAGEMENT.md) → Multi-Server

---

## 🛠️ Architecture

### Default Architecture (No IIS!)

```
Windows Server
├── Service 1: File Transfer (SSH/SCP or SMB)
├── Service 2: Web Portal (Kestrel on port 8080)
└── Optional: ConfigTool (GUI management)
```

**Key Points:**
- ✅ No IIS required
- ✅ No external dependencies
- ✅ Self-contained (.NET 8 included)
- ✅ Works on Server Core
- ✅ Perfect for air-gapped environments

**Details:** [DEPLOYMENT.md](DEPLOYMENT.md) → Architecture

---

## 📊 Features Overview

### File Transfer Service
- Real-time file watching
- SSH/SCP transfer (primary)
- SMB/CIFS transfer (fallback)
- Automatic retry with backoff
- File verification
- Archive after transfer
- Comprehensive logging

### Web Portal
- Modern upload interface
- Drag-and-drop support
- Multiple files
- Windows Authentication
- Real-time progress
- Configurable file size limits

### ConfigTool
- Service management
- SSH key generation
- Connection testing
- Complete configuration editor
- **Remote management** (new!)
- Import/Export settings

---

## 🆘 Troubleshooting

### Quick Fixes

**Service won't start:**
→ [CONFIGTOOL_QUICK_START.md](CONFIGTOOL_QUICK_START.md) → Troubleshooting

**SSH connection fails:**
→ [CONFIGTOOL_QUICK_START.md](CONFIGTOOL_QUICK_START.md) → SSH Settings → Test Connection

**Web portal not accessible:**
→ [DEPLOYMENT.md](DEPLOYMENT.md) → Troubleshooting

**Remote management issues:**
→ [REMOTE_MANAGEMENT.md](REMOTE_MANAGEMENT.md) → Troubleshooting

### Logs Location
```
Service Logs:     C:\FileRelay\logs\
Web Portal Logs:  C:\FileRelay\logs\
Windows Event Log: Application → Source: ZLFileRelay
```

---

## 📚 Additional Resources

### In Repository
- **[Main README](../README.md)** - Project overview
- **[Changelog](../CHANGELOG.md)** - Version history
- **[Getting Started](../GETTING_STARTED.md)** - Developer guide

### Archive
Historical and technical design documents are in `archive/`:
- Development phase completions
- Technical designs
- Historical IIS deployment guides
- Air-gapped deployment details

**Note:** Archive documents are preserved for reference but not actively maintained.

---

## 📞 Getting Help

1. **Check relevant guide** (see above)
2. **Review logs** (`C:\FileRelay\logs\`)
3. **Check Windows Event Log** (Source: ZLFileRelay)
4. **Review configuration** (`C:\Program Files\ZLFileRelay\appsettings.json`)

---

## 📈 Documentation Stats

**Active Documentation:** 8 essential guides  
**Total Pages:** ~80 pages of documentation  
**Coverage:** Installation, Configuration, Deployment, Management, Troubleshooting  
**Last Updated:** October 8, 2025

---

## ✨ What's New

**Recent Updates:**
- ✅ Consolidated deployment guides
- ✅ Simplified documentation structure
- ✅ Clarified Kestrel-first architecture (no IIS confusion!)
- ✅ Added remote management documentation
- ✅ Moved technical/historical docs to archive

**Key Simplification:**
- Removed redundant IIS vs. No-IIS documentation
- Single, clear deployment guide
- Kestrel standalone is the default (no IIS!)
- IIS is optional/legacy (archived)

---

**Version:** 1.0  
**Last Updated:** October 8, 2025  
**Status:** Clean, organized, production-ready documentation

**Happy deploying! 🚀**
