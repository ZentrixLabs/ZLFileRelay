# Entra ID (Azure AD) Setup Guide

This guide walks you through configuring Entra ID (formerly Azure Active Directory) authentication for ZL File Relay.

## Prerequisites

- Azure subscription with Entra ID (Azure AD)
- Global Administrator or Application Administrator role in Azure AD
- ZL File Relay installed and running

## Overview

Entra ID authentication provides:
- Single Sign-On (SSO) for users with Microsoft 365/Azure AD accounts
- Seamless integration with organizational identity
- Multi-factor authentication (MFA) support
- Conditional Access policies

## Step 1: Register Application in Azure Portal

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Go to **Azure Active Directory** > **App registrations**
3. Click **New registration**
4. Configure the application:
   - **Name**: `ZL File Relay` (or your preferred name)
   - **Supported account types**: `Accounts in this organizational directory only (Single tenant)`
   - **Redirect URI**: Leave blank for now (we'll add multiple after registration)
   - Click **Register**

## Step 1b: Add Multiple Redirect URIs

Servers with multiple network interfaces or DNS suffixes need multiple redirect URIs configured.

**Using the Configuration Tool:**
1. Click "📋 Open Azure Portal - Setup Guide" button
2. The wizard will detect and display all hostname variations automatically
3. **Add custom hostnames** in the "Additional custom hostnames" field (one per line):
   ```
   dfwupload.root.mpmaterials.com
   another-custom-hostname.example.com
   ```
4. The "Complete Redirect URI List" will auto-update with all detected + custom URIs
5. Click "📋 Copy Complete List to Clipboard" for easy copying

**Manually:**
1. Go to your app registration > **Authentication**
2. Click **Add a platform** > **Web**
3. Add EACH of these Redirect URIs (one at a time, clicking "Configure" after each):
   ```
   https://MACHINENAME:8443/signin-oidc
   https://MACHINENAME.FQDN1:8443/signin-oidc
   https://MACHINENAME.FQDN2:8443/signin-oidc
   ```
   *(Replace MACHINENAME and FQDN with your actual server names)*

**Example** for multi-NIC server:
```
https://dfwdiis01:8443/signin-oidc
https://dfwdiis01.dfw.scada.dmz:8443/signin-oidc
https://dfwupload.root.mpmaterials.com:8443/signin-oidc
```

4. **IMPORTANT:** Scroll down and add **Front-channel logout URL**:
   ```
   https://MACHINENAME:8443
   ```
   *(Use your primary hostname - the one users typically access)*
   
**Example:**
```
https://dfwupload.root.mpmaterials.com:8443
```

**Why multiple URIs?** Users may access the server using different DNS names from different networks. Azure AD will reject authentication attempts if the URI doesn't match exactly what was configured.

4. **IMPORTANT:** Configure authentication settings:
   - Scroll down to **Implicit grant and hybrid flows** (if visible in older Azure Portal)
   - **For new apps:** Under **Supported account types**, ensure **Web** platform is selected
   - **Enable Access tokens** (not usually needed)
   - **DO NOT enable ID tokens** - we use authorization code flow (more secure)
   
**Note:** New Azure App registrations use authorization code flow by default. If you see "unsupported_response_type" error, your app registration may be configured for an older flow.

## Step 2: Note Application IDs

After registration, you'll see the **Overview** page. **Copy and save** these values:

- **Application (client) ID**: A GUID like `12345678-1234-1234-1234-123456789012`
- **Directory (tenant) ID**: A GUID like `87654321-4321-4321-4321-210987654321`

You'll need these for configuration.

## Step 3: Create Client Secret

1. In your app registration, go to **Certificates & secrets**
2. Click **New client secret**
3. Add description: `ZL File Relay Secret`
4. Select expiration: `24 months` (recommended)
5. Click **Add**
6. **IMPORTANT**: Copy the **Value** immediately (it won't be shown again)
7. Store it securely - this is your **Client Secret**

## Step 4: Configure API Permissions

1. Go to **API permissions**
2. Click **Add a permission**
3. Select **Microsoft Graph**
4. Choose **Delegated permissions**
5. Add these permissions:
   - `User.Read` (Read user profile)
   - `email` (Read user email)
   - `openid` (Sign in)
   - `profile` (Read user profile)
6. Click **Add permissions**
7. Click **Grant admin consent for [Your Organization]**
8. Confirm the consent

## Step 5: Configure ZL File Relay

### Option A: Using the Configuration Tool (Recommended)

1. Open **ZL File Relay Configuration Tool**
2. Navigate to **Web Portal** tab
3. Click **"📋 Open Azure Portal - Setup Guide"** button
4. Follow the wizard:
   - Azure Portal opens automatically
   - Wizard displays all detected Redirect URIs
   - Copy credentials from Azure Portal using 💾 Copy buttons
   - Enter all credentials in wizard
5. Click **"Finish Setup"** - wizard auto-populates configuration
6. Under **Authorization (Role-Based)**:
   - Add admin email addresses (one per line)
   - Configure roles as needed
7. Click **Save Configuration**
8. Restart the ZL File Relay Web Portal service

### Option B: Manual Configuration (appsettings.json)

Edit `C:\ProgramData\ZLFileRelay\appsettings.json`:

```json
{
  "ZLFileRelay": {
    "WebPortal": {
      "Authentication": {
        "EnableEntraId": true,
        "EnableLocalAccounts": true,
        "EntraIdTenantId": "YOUR-TENANT-ID",
        "EntraIdClientId": "YOUR-CLIENT-ID",
        "EntraIdClientSecret": "YOUR-CLIENT-SECRET",
        "RequireApproval": true,
        "RequireEmailConfirmation": false
      },
      "AdminEmails": [
        "admin@example.com",
        "manager@example.com"
      ],
      "AdminRoles": ["Admin"],
      "UploaderRoles": ["Uploader", "Admin"]
    }
  }
}
```

Restart the service:
```powershell
Restart-Service ZLFileRelay
```

## Step 6: Test the Configuration

1. Open a web browser
2. Navigate to your ZL File Relay portal: `https://your-server:8443`
3. Click **Sign In to Upload**
4. You should see a **"Sign in with Microsoft"** button
5. Click it to test Microsoft authentication
6. Sign in with your Azure AD credentials
7. You should be redirected back to the portal and authenticated

## Troubleshooting

### Error: "AADSTS50011: The reply URL specified in the request does not match"

**Solution**: This means the redirect URI being used doesn't match any configured URI in Azure Portal. 
- **Common cause**: Server has multiple hostnames (e.g., `dfwdiis01`, `dfwdiis01.dfw.scada.dmz`, `dfwupload.root.mpmaterials.com`)
- **Fix**: Add ALL possible hostname variations as separate redirect URIs in Azure Portal
- **Use the wizard**: Configuration Tool's "Open Azure Portal - Setup Guide" button will detect and display all hostnames automatically

### Error: "AADSTS700016: Application not found in the directory"

**Solution**: Verify that the Tenant ID and Client ID are correct.

### Error: "AADSTS7000215: Invalid client secret provided"

**Solution**: The Client Secret is incorrect or expired. Generate a new one in Azure Portal.

### Users can sign in but get "Not Authorized" page

**Solution**: 
- Check that the user's email is in the `AdminEmails` list, OR
- Ensure the user has been assigned the appropriate role (Admin or Uploader)

### Entra ID button doesn't appear

**Solution**:
- Verify `EnableEntraId` is `true` in configuration
- Verify Tenant ID is not empty
- Check application logs for errors

### Error: "AADSTS700054: response_type 'id_token' is not enabled"

**Solution**: This means your Azure App registration is configured for implicit grant flow but ID tokens aren't enabled.
1. Go to **Authentication** in your app registration
2. Under **Implicit grant and hybrid flows**, check **ID tokens** (even though we use code flow now)
3. Alternatively, create a new app registration which uses authorization code flow by default
4. This is a legacy Azure AD configuration issue

### Sign-in works but redirects to wrong page

**Symptom**: After successful sign-in, user is redirected to the Index page instead of the Upload page.

**Solution**: This should now be automatically handled. Authenticated users visiting the Index page (`/`) are automatically redirected to `/Upload`. If you need different behavior, edit `src/ZLFileRelay.WebPortal/Pages/Index.cshtml.cs`.

**Note**: The `returnUrl` parameter is passed through the authentication flow and defaults to `/` if not specified. The Index page now checks authentication status and redirects to Upload accordingly.

## Security Best Practices

1. **Client Secret Rotation**: Rotate client secrets before they expire (set calendar reminder)
2. **Least Privilege**: Only grant users the minimum required roles
3. **Conditional Access**: Consider using Azure AD Conditional Access policies for additional security
4. **MFA**: Enable Multi-Factor Authentication for all users
5. **Audit Logs**: Regularly review Azure AD sign-in logs

## User Management

### Assigning Roles to Entra ID Users

1. Users automatically get the "Uploader" role on first sign-in
2. To grant Admin access:
   - Add their email to `AdminEmails` in configuration, OR
   - Manually assign "Admin" role via user management (future feature)

### Approval Workflow (if enabled)

If `RequireApproval` is enabled:
- New Entra ID users can sign in but cannot upload until approved
- Admins must approve users through the web portal admin interface (future feature)
- For now, set `RequireApproval: false` to auto-approve Entra ID users

## Advanced Configuration

### Multi-Tenant Support (External Users)

To allow users from external Azure AD tenants:
1. In Azure Portal, edit app registration
2. Change **Supported account types** to:
   - `Accounts in any organizational directory (Any Azure AD directory - Multitenant)`
3. Update `appsettings.json` to allow external emails in `AdminEmails`

### Custom Branding

The sign-in experience uses Microsoft's default branding. To customize:
1. In Azure Portal, go to **Company branding**
2. Add your logo, colors, and custom text
3. This will appear on the Microsoft sign-in page

## Related Documentation

- [User Management Guide](USER_MANAGEMENT.md)
- [Configuration Reference](../configuration/CONFIGURATION.md)
- [Deployment Guide](DEPLOYMENT.md)

## Support

For issues with Entra ID authentication:
1. Check application logs: `C:\ProgramData\ZLFileRelay\logs\`
2. Review Azure AD sign-in logs in Azure Portal
3. Contact your IT administrator or Azure AD admin

