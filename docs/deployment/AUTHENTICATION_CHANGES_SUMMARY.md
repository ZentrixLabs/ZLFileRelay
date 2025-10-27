# Authentication Flow Changes - Summary

## Issue
When accessing `localhost:8080`, users were immediately redirected to the upload page without being prompted for Windows authentication. This bypassed the security model and didn't inform users which domain credentials to use.

## Solution
Implemented a public landing page flow similar to DMZUploader, where users first see information about the service and domain requirements, then authenticate when accessing protected pages.

## Changes Summary

### Files Modified

1. **`src/ZLFileRelay.Core/Models/ZLFileRelayConfiguration.cs`**
   - Added `AuthenticationDomain` property to `WebPortalSettings`

2. **`appsettings.json`**
   - Added `AuthenticationDomain` field with default value "YOURDOMAIN"

3. **`src/ZLFileRelay.WebPortal/appsettings.json`**
   - Added `AuthenticationDomain` field
   - Set `RequireAuthentication` to `true`

4. **`src/ZLFileRelay.WebPortal/Pages/Index.cshtml`**
   - Completely redesigned as informative landing page
   - Shows domain authentication requirements
   - Displays service information and file requirements

5. **`src/ZLFileRelay.WebPortal/Pages/Index.cshtml.cs`**
   - Added `[AllowAnonymous]` attribute
   - Displays authentication domain from configuration
   - No longer redirects to upload page

6. **`src/ZLFileRelay.WebPortal/Pages/Upload.cshtml.cs`**
   - Added `[Authorize]` attribute to require authentication
   - Maintains existing group-based authorization checks

7. **`src/ZLFileRelay.WebPortal/Pages/Result.cshtml.cs`**
   - Added `[Authorize]` attribute

8. **`src/ZLFileRelay.WebPortal/Program.cs`**
   - Added HttpSys configuration for Windows Authentication
   - Configured Negotiate and NTLM schemes
   - Allows anonymous for landing page
   - Requires authentication only on protected routes

## New Authentication Flow

### User Experience

1. **Visit Homepage** (`http://localhost:8080`)
   - See landing page with information about the service
   - Displayed domain: "You must authenticate with your YOURDOMAIN domain credentials"
   - No authentication required at this stage

2. **Click "Start File Upload"**
   - Browser prompts for Windows credentials
   - User enters domain credentials

3. **Upload Page**
   - Authentication successful
   - System validates user is in AllowedGroups/AllowedUsers
   - If authorized: Access granted
   - If not authorized: Redirect to NotAuthorized page

4. **Results Page**
   - Requires authentication (protected)
   - Shows upload results

## Configuration Example

```json
{
  "ZLFileRelay": {
    "WebPortal": {
      "RequireAuthentication": true,
      "AuthenticationType": "Windows",
      "AuthenticationDomain": "YOURDOMAIN",
      "AllowedGroups": [
        "YOURDOMAIN\\FileUpload_Users"
      ]
    }
  }
}
```

## Technical Details

### HttpSys Configuration
- Uses Windows HTTP Server API (HttpSys) for Windows Authentication
- Supports Negotiate and NTLM authentication schemes
- Allows anonymous access on landing page
- Enforces authentication on routes with `[Authorize]` attribute

### Page Protection
- **Public Pages**: Index (landing page), Error, NotAuthorized
- **Protected Pages**: Upload, Result
- Group-based authorization checked after authentication

## Testing

1. Start the web portal: `dotnet run --project src/ZLFileRelay.WebPortal`
2. Open browser: `http://localhost:8080`
3. Verify landing page displays without authentication
4. Click "Start File Upload"
5. Verify Windows authentication prompt appears
6. Authenticate with domain credentials
7. Verify upload page accessible if in allowed groups

## Benefits

- Hospitals clearer user experience - users know which domain to use
- Proper security flow - authentication required before access
- Informative landing page - explains service and requirements
- Configurable per installation - domain can be customized
- Production-ready - follows enterprise authentication patterns

