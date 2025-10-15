# 📚 Documentation Structure

This document describes the organization of ZL File Relay documentation.

## 📁 Directory Structure

```
ZLFileRelay/
├── README.md                          # Project overview and quick start
├── CHANGELOG.md                       # Version history and release notes
├── GETTING_STARTED.md                 # Developer quick start guide
├── LICENSE                            # Software license
│
├── docs/                              # 📖 Main documentation
│   ├── README.md                      # Documentation index
│   │
│   ├── INSTALLATION.md                # Installation guide
│   ├── CONFIGURATION.md               # Configuration reference
│   ├── CONFIGTOOL_QUICK_START.md     # ConfigTool usage guide
│   │
│   ├── DEPLOYMENT.md                  # General deployment scenarios
│   ├── DEPLOYMENT_SELF_CONTAINED.md  # Air-gapped deployments
│   ├── DEPLOYMENT_QUICK_REFERENCE.md # Quick deployment cheatsheet
│   ├── IIS_DEPLOYMENT_DMZ_OT.md      # DMZ/OT network deployments
│   ├── NO_IIS_ARCHITECTURE.md        # Kestrel standalone deployment
│   │
│   ├── REMOTE_MANAGEMENT.md          # Remote management user guide
│   ├── REMOTE_MANAGEMENT_PLAN.md     # Remote management technical design
│   ├── WEB_PORTAL_CONFIGURATION.md   # Web portal setup
│   │
│   ├── PROJECT_ROADMAP.md            # Project development roadmap
│   │
│   └── archive/                       # 📦 Development archives
│       ├── BOTH_SERVICES_RUNNING.md
│       ├── CONFIG_TOOL_COMPLETED.md
│       ├── CONFIG_TOOL_FEATURES.md
│       ├── IMPLEMENTATION_COMPLETE.md
│       ├── LIVE_UPLOAD_TEST_SUCCESS.md
│       ├── MISSION_ACCOMPLISHED.md
│       ├── PHASE1_REMOTE_MANAGEMENT_COMPLETE.md
│       ├── PHASE2_COMPLETE.md
│       ├── PHASE2_COMPLETION_SUMMARY.md
│       ├── PHASE3_COMPLETE.md
│       ├── PHASE4_CONFIG_TOOL_PLAN.md
│       ├── PHASE4_WINUI3_IMPLEMENTATION.md
│       ├── PHASE4_WINUI3_MIGRATION_PLAN.md
│       ├── PROJECT_STATUS_UPDATE.md
│       ├── PROJECT_STATUS.md
│       ├── SERVICE_IS_ALIVE.md
│       ├── TESTING_COMPLETE.md
│       ├── TODAY_ACCOMPLISHMENTS.md
│       ├── VIEWMODEL_IMPLEMENTATION_SUMMARY.md
│       ├── VIEWMODELS_COMPLETE.md
│       └── ZERO_DEPENDENCIES_ACHIEVED.md
│
├── installer/                         # 📦 Installation files
│   ├── README.md                      # Installer documentation
│   ├── ZLFileRelay.iss               # Inno Setup script
│   └── scripts/                       # Installation scripts
│       ├── Configure-IIS.ps1
│       └── Remove-IIS.ps1
│
├── build/                             # 🔨 Build scripts
│   └── publish-selfcontained.ps1     # Self-contained build script
│
└── .github/                           # ⚙️ GitHub configuration
    └── README.md                      # GitHub workflows info
```

## 📖 Document Categories

### 🎯 Getting Started (Root Level)
Essential documents for all users:

| File | Purpose | Audience |
|------|---------|----------|
| `README.md` | Project overview and features | Everyone |
| `CHANGELOG.md` | Version history | Everyone |
| `GETTING_STARTED.md` | Quick start for developers | Developers |
| `LICENSE` | Software license | Everyone |

### 📚 User Documentation (`docs/`)
Comprehensive guides for users and administrators:

#### Installation & Setup
- `INSTALLATION.md` - Step-by-step installation
- `CONFIGURATION.md` - Complete configuration reference
- `CONFIGTOOL_QUICK_START.md` - Configuration tool guide

#### Deployment
- `DEPLOYMENT.md` - General deployment scenarios
- `DEPLOYMENT_SELF_CONTAINED.md` - Air-gapped/offline deployments
- `DEPLOYMENT_QUICK_REFERENCE.md` - Quick reference card
- `IIS_DEPLOYMENT_DMZ_OT.md` - DMZ and OT network deployments
- `NO_IIS_ARCHITECTURE.md` - Kestrel standalone mode

#### Advanced Features
- `REMOTE_MANAGEMENT.md` - Managing remote servers
- `REMOTE_MANAGEMENT_PLAN.md` - Remote management design
- `CREDENTIAL_MANAGEMENT.md` - Understanding credential types and security
- `WEB_PORTAL_CONFIGURATION.md` - Web portal configuration

#### Project Information
- `PROJECT_ROADMAP.md` - Development roadmap and history
- `README.md` - Documentation index

### 📦 Development Archives (`docs/archive/`)
Historical development documents and status reports:

- Phase completion reports (PHASE1-4)
- Feature completion summaries
- Testing reports
- Implementation notes
- Status updates
- Technical design documents

**Note:** Archive documents are kept for reference but are not actively maintained.

### 🔧 Technical Documentation
Component-specific documentation:

- `installer/README.md` - Installer documentation
- `.github/README.md` - GitHub workflows info

## 🗺️ Documentation Roadmap

### Quick Navigation Paths

**New User Path:**
```
1. README.md (overview)
2. docs/INSTALLATION.md (install)
3. docs/CONFIGTOOL_QUICK_START.md (configure)
4. Done!
```

**Server Core Deployment:**
```
1. README.md (overview)
2. docs/DEPLOYMENT_SELF_CONTAINED.md (prepare)
3. docs/REMOTE_MANAGEMENT.md (setup remote management)
4. docs/CONFIGTOOL_QUICK_START.md (configure remotely)
```

**DMZ/OT Network:**
```
1. README.md (overview)
2. docs/IIS_DEPLOYMENT_DMZ_OT.md (deployment guide)
3. docs/CONFIGURATION.md (configure)
```

**Configuration Reference:**
```
1. docs/CONFIGURATION.md (settings reference)
2. docs/CONFIGTOOL_QUICK_START.md (how to change settings)
3. docs/REMOTE_MANAGEMENT.md (if managing remotely)
```

**Troubleshooting:**
```
1. docs/README.md (find relevant guide)
2. Check specific guide (Installation, Configuration, etc.)
3. Review logs in C:\FileRelay\logs\
4. Check docs/archive/ for detailed technical notes
```

## 📝 Document Maintenance

### Active Documents
Documents in root and `docs/` (excluding `archive/`) are actively maintained:
- Updated with new features
- Reviewed for accuracy
- Version-controlled

### Archive Documents
Documents in `docs/archive/` are historical:
- Preserved for reference
- Not updated with new features
- Represent point-in-time status

### Adding New Documents
New documentation should be placed in:
- **Root** - Only essential, universal documents
- **docs/** - User guides, references, how-tos
- **docs/archive/** - Historical/development notes
- **Component folders** - Component-specific docs

## 🔍 Finding Information

### By Task
| I want to... | Read this |
|--------------|-----------|
| Install ZL File Relay | `docs/INSTALLATION.md` |
| Configure settings | `docs/CONFIGURATION.md` |
| Use ConfigTool | `docs/CONFIGTOOL_QUICK_START.md` |
| Deploy to Server Core | `docs/REMOTE_MANAGEMENT.md` |
| Deploy to DMZ | `docs/IIS_DEPLOYMENT_DMZ_OT.md` |
| Build from source | `GETTING_STARTED.md` |
| See version history | `CHANGELOG.md` |
| Understand architecture | `README.md` |

### By Role
| Role | Start Here |
|------|------------|
| **End User** | `README.md` → `docs/INSTALLATION.md` |
| **Administrator** | `docs/CONFIGTOOL_QUICK_START.md` |
| **Developer** | `GETTING_STARTED.md` |
| **DevOps** | `docs/DEPLOYMENT.md` |
| **Security** | `docs/CONFIGURATION.md` (Security section) |

### By Environment
| Environment | Guides |
|-------------|--------|
| **Windows Server (GUI)** | Standard installation |
| **Windows Server Core** | `docs/REMOTE_MANAGEMENT.md` |
| **DMZ Network** | `docs/IIS_DEPLOYMENT_DMZ_OT.md` |
| **Air-Gapped** | `docs/DEPLOYMENT_SELF_CONTAINED.md` |
| **Multi-Site** | `docs/DEPLOYMENT.md` + `docs/REMOTE_MANAGEMENT.md` |

## 📊 Documentation Statistics

**Total Documents:** ~40 files  
**Active Documentation:** ~15 files  
**Archive Documents:** ~20 files  
**README Files:** 4 files  

**Documentation Coverage:**
- ✅ Installation
- ✅ Configuration
- ✅ Deployment (multiple scenarios)
- ✅ Remote Management
- ✅ Troubleshooting
- ✅ API Reference
- ✅ Developer Guide
- ✅ Change History

## 🎯 Quick Links

### Most Important Documents
1. **[README.md](../README.md)** - Start here
2. **[docs/INSTALLATION.md](docs/INSTALLATION.md)** - How to install
3. **[docs/CONFIGTOOL_QUICK_START.md](docs/CONFIGTOOL_QUICK_START.md)** - How to configure
4. **[docs/REMOTE_MANAGEMENT.md](docs/REMOTE_MANAGEMENT.md)** - Server Core support
5. **[CHANGELOG.md](../CHANGELOG.md)** - What's new

### Common Tasks
- **Configure SSH:** `docs/CONFIGTOOL_QUICK_START.md` → SSH Settings
- **Deploy to Server Core:** `docs/REMOTE_MANAGEMENT.md` → Setup
- **Change Settings:** `docs/CONFIGURATION.md` → Settings Reference
- **Troubleshoot:** `docs/README.md` → Troubleshooting section

## 📞 Contributing to Documentation

### Reporting Issues
- Documentation errors → GitHub Issues
- Missing information → GitHub Issues
- Suggestions → GitHub Issues

### Improving Documentation
1. Fork repository
2. Edit documentation
3. Submit pull request
4. See `docs/CONTRIBUTING.md` (when created)

---

**Documentation Version:** 1.0  
**Last Updated:** October 8, 2025  
**Project:** ZL File Relay v1.0.0

