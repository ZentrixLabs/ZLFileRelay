# Authentication Flow Changes

## Overview
Updated the web portal authentication flow to require a public landing page that displays domain information, with authentication only required for protected pages (upload, results).

## Changes Made

### 1. Configuration Model Updates
**File:** `src/ZLFileRelay.Core/Models/ZLFileRelayConfiguration.cs`
- Added `AuthenticationDomain` property to `WebPortalSettings` class
- Allows configuration of which domain users should authenticate with

### 2. Configuration Files
**Files:** `appsettings.json`, `src/ZLFileRelay.WebPortal/appsettings.json`
- Added `AuthenticationDomain` field to WebPortal section
- Default value: `"YOURDOMAIN"` (must be configured for production)

### 3. Public Landing Page
**File:** `src/ZLFileRelay.WebPortal/Pages/Index.cshtml`
- Created informative landing page with:
  - Service description
  - Domain authentication requirements
  - File requirements and restrictions
  - Support contact information
  - Link to start file upload

**File:** `src/ZLFileRelay.WebPortal/Pages/Index.cshtml.cs`
- Added `[AllowAnonymous]` attribute to make landing page public
- Displays site name, contact email, and authentication domain
- No redirect to upload page

### 4. Protected Pages
**File:** `src/ZLFileRelay.WebPortal/Pages/Upload.cshtml.cs`
- Added `[Authorize]` attribute to require Windows authentication
- Authorization check still validates against AllowedGroups/AllowedUsers

**File:** `src/ZLFileRelay.WebPortal/Pages/Result.cshtml.cs`
- Added `[Authorize]` attribute to require Windows authentication

### 5. Program Configuration
**File:** `src/ZLFileRelay.WebPortal/Program.cs`
- Uses Kestrel web server with Negotiate authentication handler
- Simple `.AddNegotiate()` configuration (matches proven DMZUploader pattern)
- Automatically uses NTLM when Kerberos is unavailable (cross-domain scenarios)
- Allows anonymous access for landing page
- Authorization policy set to require authentication only on protected routes

## Authentication Flow

### Before
1. User visits `localhost:8080`
2. Immediately redirected to `/Upload`
3. Authentication may not be properly enforced

### After
1. User visits `localhost:8080`
2. **Landing page displays** (public, no authentication required)
   - Shows domain information: "You must authenticate with your YOURDOMAIN credentials"
   - Explains the service
   - Provides support contact
3. User clicks "Start File Upload"
4. **Windows authentication challenge** is presented
5. Upon successful authentication, user accesses upload page
6. Authorization validates user is in AllowedGroups or AllowedUsers
7. If not authorized, user is redirected to `/NotAuthorized` page

## Configuration Required

### In `appsettings.json`:
```json
{
  "ZLFileRelay": {
    "WebPortal": {
      "RequireAuthentication": true,
      "AuthenticationType": "Windows",
      "AuthenticationDomain": "YOURDOMAIN",
      "AllowedGroups": [
        "YOURDOMAIN\\FileUpload_Users",
        "YOURDOMAIN\\Administrators"
      ],
      "AllowedUsers": []
    }
  }
}
```

## Testing

1. Visit `http://localhost:8080`
   - Should see landing page without authentication
   - Domain information should be displayed
   
2. Click "Start File Upload"
   - Should prompt for Windows credentials
   - Should require authentication
   
3. Authenticate with domain credentials
   - Should see upload page
   - Should only work if user is in AllowedGroups

## Benefits

1. **Clearer UX**: Users know which domain to use before authenticating
2. **Security**: Public landing page provides information without security risk
3. **Proper Authentication Flow**: Kestrel with Negotiate handler provides Windows authentication support
4. **Cross-Domain Support**: Negotiate handler automatically uses NTLM when Kerberos unavailable (no configuration needed)
5. **Simplicity**: Matches proven DMZUploader pattern - no complex custom handlers
6. **Flexibility**: Domain is configurable per installation

