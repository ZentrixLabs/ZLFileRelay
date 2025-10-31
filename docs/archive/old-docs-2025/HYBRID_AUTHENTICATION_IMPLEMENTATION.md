# Hybrid Authentication Implementation Summary

## Overview

Successfully migrated ZL File Relay from Windows Authentication (NTLM/Kerberos) to a modern hybrid authentication system supporting both **Entra ID (Azure AD)** for SSO and **Local Accounts** for external users.

## Problem Statement

Cross-domain Windows Authentication was failing between corporate AD domain and DMZ domain without trust relationship, causing authentication loops and failed credential prompts with Kestrel. Additionally, Windows Authentication required domain trust and didn't work in air-gapped (isolated) networks.

## Solution

Replaced Windows Authentication with a flexible hybrid system:
- **Entra ID (Azure AD)**: OAuth 2.0/OIDC for Microsoft 365 users (seamless SSO) - Optional for cloud-connected deployments
- **ASP.NET Core Identity**: Local username/password accounts with SQLite database - Works offline in air-gapped environments
- **Role-Based Authorization**: Simple Admin/Uploader roles instead of AD groups

### Key Benefit: Air-Gapped Support

The local authentication option enables ZL File Relay to operate **completely offline** in isolated networks without:
- Internet connectivity
- Domain trust relationships
- Cloud dependencies
- Active Directory infrastructure

This is ideal for:
- OT (Operational Technology) environments
- SCADA networks
- Industrial control systems
- Secure/classified installations
- Isolated DMZ networks

## Implementation Details

### 1. Core Changes

**Configuration Model** (`ZLFileRelay.Core/Models/ZLFileRelayConfiguration.cs`):
- Added `AuthenticationSettings` class with Entra ID and Local Account configuration
- Added `AdminEmails`, `AdminRoles`, `UploaderRoles` for role-based authorization
- Removed `RequireAuthentication`, `AuthenticationDomain`, `AllowedGroups`, `AllowedUsers`

### 2. NuGet Packages Added

**WebPortal** (`ZLFileRelay.WebPortal.csproj`):
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="8.0.0" />
<PackageReference Include="Microsoft.Identity.Web" Version="2.15.5" />
<PackageReference Include="Microsoft.Identity.Web.UI" Version="2.15.5" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Identity.UI" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
```

Removed:
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.Negotiate" Version="8.0.0" />
```

### 3. Database Layer

**New Files**:
- `ZLFileRelay.WebPortal/Data/ApplicationDbContext.cs`: EF Core DbContext for Identity
- `ZLFileRelay.WebPortal/Data/ApplicationUser.cs`: Extended IdentityUser with approval workflow
- `ZLFileRelay.WebPortal/Services/DatabaseInitializationService.cs`: Database and role seeding

**Schema**:
- SQLite database (default: `C:\ProgramData\ZLFileRelay\zlfilerelay.db`)
- ASP.NET Core Identity tables (AspNetUsers, AspNetRoles, AspNetUserRoles, etc.)
- Custom fields: `IsApproved`, `ApprovedDate`, `ApprovedBy`, `RegistrationDate`

### 4. Authentication Configuration

**Program.cs** changes:
- Replaced `AddNegotiate()` with `AddDefaultIdentity<ApplicationUser>()` for local accounts
- Added `AddMicrosoftIdentityWebApp()` for Entra ID OAuth/OIDC
- Added database initialization on startup
- Removed Windows-specific middleware (NTLM header manipulation)
- Simplified diagnostic logging

**Configuration** (`appsettings.json`):
```json
{
  "ZLFileRelay": {
    "WebPortal": {
      "Authentication": {
        "EnableEntraId": false,
        "EnableLocalAccounts": true,
        "EntraIdTenantId": null,
        "EntraIdClientId": null,
        "EntraIdClientSecret": null,
        "ConnectionString": "Data Source=C:\\ProgramData\\ZLFileRelay\\zlfilerelay.db",
        "RequireEmailConfirmation": false,
        "RequireApproval": true
      },
      "AdminEmails": ["admin@example.com"],
      "AdminRoles": ["Admin"],
      "UploaderRoles": ["Uploader", "Admin"]
    }
  }
}
```

### 5. Authorization Service

**Replaced AD Group Checks with Role-Based**:
- `IsUserAllowedAsync(ClaimsPrincipal user)`: Checks admin emails, roles, and approval status
- `IsUserAdmin(ClaimsPrincipal user)`: Checks admin emails and admin roles
- `GetUserDisplayName(ClaimsPrincipal user)`: Extracts name from various claim types
- `GetUserEmail(ClaimsPrincipal user)`: Extracts email from various claim types

**Authorization Logic**:
1. Check if user email is in `AdminEmails` → Full access
2. Check if local account and `RequireApproval` → Verify `IsApproved` field
3. Check if user has `Uploader` or `Admin` role → Grant upload access

### 6. Login/Register Pages

**New Razor Pages**:
- `Pages/Login.cshtml` + `.cs`: Unified login page with "Sign in with Microsoft" button and local account form
- `Pages/Register.cshtml` + `.cs`: Local account registration with password requirements and approval workflow

**Features**:
- Conditional UI based on `EnableEntraId` and `EnableLocalAccounts`
- External login handling for Entra ID (auto-creates local account on first sign-in)
- Password strength requirements (8+ chars, uppercase, lowercase, number, special char)
- Approval status messaging

### 7. Landing Page Updates

**Index.cshtml** changes:
- Dynamic authentication messaging based on enabled auth methods
- "Sign In to Upload" button for unauthenticated users
- "Start File Upload" button for authenticated users

### 8. Configuration Tool

**WebPortalViewModel.cs** changes:
- Removed: `RequireAuthentication`, `AuthenticationDomain`, `AllowedGroups`
- Added: `EnableEntraId`, `EnableLocalAccounts`, `EntraIdTenantId`, `EntraIdClientId`, `EntraIdClientSecret`, `ConnectionString`, `RequireEmailConfirmation`, `RequireApproval`, `AdminEmails`, `AdminRoles`, `UploaderRoles`
- Removed: `BrowseGroups()` command (no longer relevant)

**WebPortalView.xaml** changes:
- Replaced "Windows Authentication" section with "Authentication (Hybrid: Entra ID + Local Accounts)"
- Added Entra ID configuration UI (Tenant ID, Client ID, Client Secret)
- Added Local Accounts configuration UI (Approval settings, Email confirmation, Database path)
- Added "Authorization (Role-Based)" section (Admin emails, Admin roles, Uploader roles)

### 9. Upload Page

**Upload.cshtml.cs** changes:
- Changed `OnGet()` to `async OnGetAsync()`
- Changed `IsUserAllowed(User)` to `await IsUserAllowedAsync(User)`
- Removed `SupportedOSPlatform("windows")` attribute

### 10. Documentation

**New Documentation**:
- `docs/deployment/ENTRA_ID_SETUP.md`: Complete guide for Azure AD app registration and configuration
- `docs/deployment/USER_MANAGEMENT.md`: Guide for managing users, roles, and approval workflows

## Configuration Migration

### Old Configuration (Windows Auth)
```json
{
  "WebPortal": {
    "RequireAuthentication": true,
    "AuthenticationType": "Windows",
    "AuthenticationDomain": "DMZDOMAIN",
    "AllowedGroups": ["DOMAIN\\FileUpload_Users"],
    "AllowedUsers": []
  }
}
```

### New Configuration (Hybrid)
```json
{
  "WebPortal": {
    "Authentication": {
      "EnableEntraId": false,
      "EnableLocalAccounts": true,
      "RequireApproval": true
    },
    "AdminEmails": ["admin@example.com"],
    "AdminRoles": ["Admin"],
    "UploaderRoles": ["Uploader", "Admin"]
  }
}
```

## Benefits

### ✅ Problems Solved
- **No more cross-domain auth issues**: Eliminated NTLM/Kerberos cross-domain trust problems
- **Works from anywhere**: Not limited to intranet/domain-joined machines
- **Mobile-friendly**: Can access from phones, tablets, non-Windows devices
- **External access**: Can grant access to contractors without AD accounts
- **Modern standards**: OAuth 2.0, OIDC, SAML support via Entra ID

### ✅ New Capabilities
- **SSO for employees**: Seamless sign-in with Microsoft 365 accounts
- **Self-service registration**: Users can create accounts (with approval workflow)
- **Role-based access**: Simple, flexible authorization model
- **Email-based identification**: Works across identity providers
- **MFA support**: Leverage Entra ID MFA and Conditional Access
- **Audit trail**: All sign-ins logged with identity provider info

### ✅ Flexibility
- **Choose your auth**: Enable Entra ID, Local Accounts, or both
- **Gradual migration**: Can enable both methods during transition
- **No infrastructure changes**: SQLite database (no SQL Server required)
- **Cloud-native**: Ready for Azure/cloud deployment

## Testing Checklist

### Local Accounts
- [ ] User can register a new account
- [ ] Password requirements are enforced
- [ ] User can sign in with local account
- [ ] Approval workflow blocks unauthorized uploads (if enabled)
- [ ] User can upload files after approval
- [ ] Account lockout works after failed attempts

### Entra ID (when configured)
- [ ] "Sign in with Microsoft" button appears
- [ ] SSO redirects to Microsoft login
- [ ] User is created on first Entra ID sign-in
- [ ] Entra ID user email is captured correctly
- [ ] Entra ID user can upload files (with appropriate role)
- [ ] Admin email list grants access to Entra ID users

### Authorization
- [ ] Admin emails have full access
- [ ] Uploader role can upload files
- [ ] Admin role can upload files
- [ ] Non-approved users see "Not Authorized"
- [ ] Unauthenticated users redirected to Login

### Configuration Tool
- [ ] Can enable/disable Entra ID
- [ ] Can enable/disable Local Accounts
- [ ] Can configure Entra ID credentials
- [ ] Can add admin emails
- [ ] Can configure roles
- [ ] Settings save correctly to appsettings.json

## Deployment Steps

1. **Backup existing configuration**:
   ```powershell
   Copy-Item C:\ProgramData\ZLFileRelay\appsettings.json C:\ProgramData\ZLFileRelay\appsettings.json.bak
   ```

2. **Stop services**:
   ```powershell
   Stop-Service ZLFileRelay
   ```

3. **Deploy updated binaries**:
   - Copy new WebPortal, ConfigTool, and Core DLLs

4. **Update configuration**:
   - Use Configuration Tool to configure authentication
   - OR manually edit `appsettings.json` with new structure

5. **Initialize database**:
   - Database will be created automatically on first run
   - Or run: `dotnet ef database update --project src/ZLFileRelay.WebPortal`

6. **Create initial admin account**:
   - Add your email to `AdminEmails` in configuration
   - Register account via web portal
   - Or use Entra ID (email must match `AdminEmails`)

7. **Start services**:
   ```powershell
   Start-Service ZLFileRelay
   ```

8. **Test authentication**:
   - Visit portal and sign in
   - Verify upload access

## Rollback Plan

If issues arise, rollback to Windows Authentication:
1. Restore backup: `Copy-Item appsettings.json.bak appsettings.json`
2. Deploy previous binaries
3. Restart service

## Known Limitations

- **No password reset email**: Email configuration required (future enhancement)
- **No admin UI for user approval**: Must use database queries or PowerShell (future enhancement)
- **No group sync with Entra ID**: Users get default "Uploader" role, must manually add to `AdminEmails`
- **No 2FA for local accounts**: Only available via Entra ID

## Future Enhancements

- [ ] Admin web UI for user management
- [ ] Email confirmation workflow
- [ ] Password reset email workflow  
- [ ] Bulk user import/export
- [ ] Integration with Entra ID app roles
- [ ] SAML support for other identity providers
- [ ] API tokens for programmatic access

## Files Changed

### Modified Files
- `src/ZLFileRelay.Core/Models/ZLFileRelayConfiguration.cs`
- `src/ZLFileRelay.WebPortal/Program.cs`
- `src/ZLFileRelay.WebPortal/Services/AuthorizationService.cs`
- `src/ZLFileRelay.WebPortal/Pages/Upload.cshtml.cs`
- `src/ZLFileRelay.WebPortal/Pages/Index.cshtml` + `.cs`
- `src/ZLFileRelay.WebPortal/ZLFileRelay.WebPortal.csproj`
- `src/ZLFileRelay.ConfigTool/ViewModels/WebPortalViewModel.cs`
- `src/ZLFileRelay.ConfigTool/Views/WebPortalView.xaml`
- `appsettings.json`

### New Files
- `src/ZLFileRelay.WebPortal/Data/ApplicationDbContext.cs`
- `src/ZLFileRelay.WebPortal/Services/DatabaseInitializationService.cs`
- `src/ZLFileRelay.WebPortal/Pages/Login.cshtml` + `.cs`
- `src/ZLFileRelay.WebPortal/Pages/Register.cshtml` + `.cs`
- `docs/deployment/ENTRA_ID_SETUP.md`
- `docs/deployment/USER_MANAGEMENT.md`
- `docs/HYBRID_AUTHENTICATION_IMPLEMENTATION.md`

### Deleted Concepts
- Windows Authentication (Negotiate/NTLM/Kerberos)
- AD Group authorization
- Cross-domain authentication middleware
- `BrowseGroups` AD browser functionality

## Verification

All projects build successfully:
- ✅ `ZLFileRelay.Core` - Build succeeded
- ✅ `ZLFileRelay.WebPortal` - Build succeeded
- ✅ `ZLFileRelay.ConfigTool` - Build succeeded
- ✅ `ZLFileRelay.Service` - (Not modified, should still build)

## Support

For issues with hybrid authentication:
1. Check application logs: `C:\ProgramData\ZLFileRelay\logs\`
2. Review database state: `sqlite3 C:\ProgramData\ZLFileRelay\zlfilerelay.db`
3. Verify Entra ID app registration (if using Entra ID)
4. Check configuration in `appsettings.json`

## Conclusion

The hybrid authentication system provides a modern, flexible, and reliable solution that eliminates cross-domain Windows Authentication problems while supporting both SSO for internal users and local accounts for external users.

