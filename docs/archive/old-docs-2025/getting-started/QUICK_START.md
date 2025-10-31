# Getting Started with ZL File Relay Development

## Quick Start for Developers

### Prerequisites
- Visual Studio 2022 or VS Code with C# extension
- .NET 8.0 SDK
- Windows 10/11 or Windows Server 2019+

### Clone and Build

```powershell
# Clone repository
git clone https://github.com/your-org/ZLFileRelay.git
cd ZLFileRelay

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test
```

### Solution Structure

```
ZLFileRelay/
├── src/
│   ├── ZLFileRelay.Core/           # ✅ CREATED - Shared models, interfaces
│   ├── ZLFileRelay.Service/        # 🔄 TODO - Windows Service implementation
│   ├── ZLFileRelay.WebPortal/      # 🔄 TODO - ASP.NET Core web app
│   └── ZLFileRelay.ConfigTool/     # 🔄 TODO - WPF configuration tool
├── tests/
│   └── ZLFileRelay.Core.Tests/     # ✅ CREATED - Unit tests
├── docs/                            # ✅ CREATED - Documentation
└── installer/                       # 🔄 TODO - Inno Setup installer
```

### Core Library (ZLFileRelay.Core)

The Core library contains shared models, interfaces, and constants used by all components.

**Key Files:**
- `Models/ZLFileRelayConfiguration.cs` - Complete configuration model
- `Models/TransferResult.cs` - Transfer and upload result models
- `Interfaces/IFileTransferService.cs` - File transfer service interface
- `Interfaces/IFileUploadService.cs` - File upload service interface
- `Interfaces/ICredentialProvider.cs` - Secure credential storage interface
- `Constants/ApplicationConstants.cs` - Application-wide constants
- `Constants/ErrorMessages.cs` - Centralized error messages

### Configuration

The application uses a unified `appsettings.json` file. See the root `appsettings.json` for a complete example.

**Key Sections:**
- `Branding` - Customize company name, logo, theme
- `Paths` - Configure all directory paths
- `Service` - File transfer service settings
- `WebPortal` - Web upload portal settings
- `Transfer` - SSH and SMB destination configuration
- `Security` - Security and encryption settings

### Development Workflow

#### Phase 1: Core Library ✅ COMPLETE
- [x] Create solution structure
- [x] Define configuration models
- [x] Define core interfaces
- [x] Create constants and error messages

#### Phase 2: Service Implementation 🔄 NEXT
- [ ] Migrate file watcher logic from ZLBridge
- [ ] Implement IFileTransferService for SSH
- [ ] Implement IFileTransferService for SMB
- [ ] Add retry logic and error handling
- [ ] Add file verification
- [ ] Implement configuration loading
- [ ] Add comprehensive logging

#### Phase 3: Web Portal 🔄 TODO
- [ ] Migrate upload logic from DMZUploader
- [ ] Implement IFileUploadService
- [ ] Update UI with new branding
- [ ] Add multi-file upload support
- [ ] Implement Windows Authentication
- [ ] Add configuration-based theming

#### Phase 4: Configuration Tool 🔄 TODO
- [ ] Create MVVM structure
- [ ] Implement configuration editor
- [ ] Add SSH key generation
- [ ] Add service management
- [ ] Add IIS management
- [ ] Add connection testing

#### Phase 5: Installer 🔄 TODO
- [ ] Create Inno Setup script
- [ ] Add component selection
- [ ] Implement IIS configuration
- [ ] Implement service registration
- [ ] Add pre-flight checks

### Running Individual Components

**Service (once implemented):**
```powershell
cd src/ZLFileRelay.Service
dotnet run
```

**Web Portal (once implemented):**
```powershell
cd src/ZLFileRelay.WebPortal
dotnet run
# Navigate to https://localhost:5001
```

**Configuration Tool (once implemented):**
```powershell
cd src/ZLFileRelay.ConfigTool
dotnet run
```

### Testing

```powershell
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test project
dotnet test tests/ZLFileRelay.Core.Tests
```

### Migrating Code from Legacy Projects

#### From ZLBridge (DMZFileTransferService)
Files to migrate:
- `ScpFileTransferService.cs` → Service implementation
- `SshKeyGenerator.cs` → ConfigTool
- `FileWatcher.cs` → Service implementation
- `Worker.cs` → Service implementation
- `CredentialProvider.cs` → Core/Services
- `PathValidator.cs` → Core/Services
- `FileQueue.cs` → Service implementation
- `RetryPolicy.cs` → Service implementation

#### From DMZUploader
Files to migrate:
- `Services/FileUploadService.cs` → WebPortal
- `Services/ADAuthorizationService.cs` → WebPortal
- `Pages/*.cshtml` → WebPortal/Pages
- `wwwroot/` → WebPortal/wwwroot

### Code Style

- Use C# 12 features where appropriate
- Enable nullable reference types
- Use `async/await` for all I/O operations
- Follow SOLID principles
- XML documentation for public APIs
- Comprehensive error handling
- Structured logging with Serilog

### Architecture Decisions

1. **Shared Configuration**: All components use the same `appsettings.json`
2. **Dependency Injection**: Used throughout for testability
3. **Interface-Based Design**: Allows easy swapping of implementations
4. **Separation of Concerns**: Clear boundaries between components
5. **Security First**: Encryption, validation, audit logging built-in

### Next Steps

1. Review the Core models and interfaces
2. Start implementing the Service component
3. Migrate SSH/SCP logic from ZLBridge
4. Test file transfer functionality
5. Move on to WebPortal migration

### Questions?

- Check the docs/ folder for detailed documentation
- Review the .cursorrules file for development guidelines
- Look at existing ZLBridge and DMZUploader code for reference

### Useful Commands

```powershell
# Clean all build artifacts
dotnet clean

# Rebuild everything
dotnet build --no-incremental

# Format code
dotnet format

# Update NuGet packages
dotnet list package --outdated
dotnet add package <PackageName> --version <Version>

# Publish for deployment
dotnet publish -c Release -r win-x64 --self-contained
```

---

**Ready to start building!** 🚀

Begin with Phase 2: Service Implementation by migrating the core transfer logic from ZLBridge.
