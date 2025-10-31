# User Management Guide

This guide covers managing users in ZL File Relay's hybrid authentication system (Entra ID + Local Accounts).

## User Types

ZL File Relay supports two types of users:

### 1. Entra ID (Azure AD) Users
- Authenticate via Microsoft 365 / Azure AD
- Single Sign-On (SSO) experience
- Managed through Azure AD
- Automatically created on first sign-in

### 2. Local Account Users
- Authenticate with email/password
- Managed directly in ZL File Relay
- Self-registration available (if enabled)
- Stored in local SQLite database

## Roles and Permissions

### Built-in Roles

| Role | Permissions |
|------|-------------|
| **Admin** | Full access - upload files, manage users (future), view all logs |
| **Uploader** | Upload files only |

### Role Assignment Priority

Users are authorized in this order:
1. **Admin Emails**: If user's email is in the `AdminEmails` list → Full admin access
2. **Roles**: If user has `Admin` or `Uploader` role → Corresponding access
3. **Approval Status**: If `RequireApproval` is enabled and user is not approved → No access

## Managing Users via Configuration

### Add Admin by Email

**Method 1: Configuration Tool**
1. Open ZL File Relay Configuration Tool
2. Go to **Web Portal** tab
3. Under **Authorization (Role-Based)**
4. In **Admin Emails**, add one email per line:
   ```
   admin@example.com
   manager@example.com
   ```
5. Click **Save Configuration**
6. Restart service

**Method 2: Edit appsettings.json**
```json
{
  "ZLFileRelay": {
    "WebPortal": {
      "AdminEmails": [
        "admin@example.com",
        "manager@example.com"
      ]
    }
  }
}
```

### Configure Approval Workflow

To require admin approval for new local account registrations:

**Configuration Tool:**
1. Web Portal tab → Authentication
2. ✅ Check "Require Admin Approval for New Accounts"
3. Save and restart

**appsettings.json:**
```json
{
  "ZLFileRelay": {
    "WebPortal": {
      "Authentication": {
        "RequireApproval": true
      }
    }
  }
}
```

## Local Account Management

### Self-Registration Process

If local accounts are enabled:
1. Users visit the portal
2. Click "Sign in" → "Register here"
3. Provide email and password
4. If `RequireApproval` is enabled:
   - Account created but cannot upload
   - Admin must approve (see below)
5. If `RequireApproval` is disabled:
   - Account created and can upload immediately

### Approving Local Accounts (Manual Database Method)

**Until the admin UI is implemented**, approve users via database:

1. Locate the database:
   ```powershell
   cd C:\ProgramData\ZLFileRelay
   ```

2. Install SQLite tools (if not already installed):
   ```powershell
   choco install sqlite
   ```

3. Open the database:
   ```powershell
   sqlite3 zlfilerelay.db
   ```

4. List pending users:
   ```sql
   SELECT Id, UserName, Email, RegistrationDate, IsApproved 
   FROM AspNetUsers 
   WHERE IsApproved = 0;
   ```

5. Approve a user:
   ```sql
   UPDATE AspNetUsers 
   SET IsApproved = 1, 
       ApprovedDate = datetime('now'), 
       ApprovedBy = 'admin@example.com' 
   WHERE Email = 'user@example.com';
   ```

6. Exit SQLite:
   ```
   .quit
   ```

### Deleting Local Accounts

**Via Database:**
```sql
DELETE FROM AspNetUsers WHERE Email = 'user@example.com';
```

**Important**: This does NOT delete associated uploads or logs, only the user account.

### Resetting Local Account Password

**Until password reset is fully implemented**, reset via database:

1. User must go to login page → "Forgot your password?"
2. (Currently not functional - requires email configuration)

**Manual reset (requires C# knowledge)**:
```csharp
var user = await _userManager.FindByEmailAsync("user@example.com");
var token = await _userManager.GeneratePasswordResetTokenAsync(user);
await _userManager.ResetPasswordAsync(user, token, "NewPassword123!");
```

## Entra ID User Management

Entra ID users are managed through Azure Active Directory:

### Grant Access to Entra ID User

**Method 1: Add to Admin Emails (Recommended)**
- Add their email to `AdminEmails` in configuration
- Restart service
- User will have admin access on next sign-in

**Method 2: Assign Role (Future Feature)**
- Will be available in admin UI
- Currently, all new Entra ID users get "Uploader" role automatically

### Revoke Access for Entra ID User

**Temporary:**
- Remove from `AdminEmails` if present
- (Currently no way to revoke Uploader role without code changes)

**Permanent:**
- Remove their Azure AD account or block sign-ins in Azure Portal
- Remove from `AdminEmails` in ZL File Relay configuration

## User Database Schema

Local accounts are stored in SQLite with ASP.NET Core Identity schema:

### Key Tables

- `AspNetUsers`: User accounts and profile data
- `AspNetRoles`: Roles (Admin, Uploader)
- `AspNetUserRoles`: User-role mappings
- `AspNetUserLogins`: External logins (Entra ID)

### Custom Fields in AspNetUsers

| Field | Type | Description |
|-------|------|-------------|
| `IsApproved` | bit | Whether user is approved to upload |
| `ApprovedDate` | datetime | When user was approved |
| `ApprovedBy` | nvarchar | Email of admin who approved |
| `RegistrationDate` | datetime | When user registered |

## Monitoring and Auditing

### View Recent Sign-Ins

Check application logs:
```powershell
Get-Content C:\ProgramData\ZLFileRelay\logs\zlfilerelay-web-*.log -Tail 50 | Select-String "authenticated"
```

### View Recent Registrations

Via database:
```sql
SELECT Email, RegistrationDate, IsApproved 
FROM AspNetUsers 
ORDER BY RegistrationDate DESC 
LIMIT 10;
```

### View Failed Login Attempts

Check logs:
```powershell
Get-Content C:\ProgramData\ZLFileRelay\logs\zlfilerelay-web-*.log -Tail 50 | Select-String "Invalid login attempt"
```

## Best Practices

1. **Principle of Least Privilege**: Only grant "Admin" role to users who need it
2. **Regular Audits**: Review user list and access logs quarterly
3. **Approval Workflow**: Enable `RequireApproval` for external-facing portals
4. **Entra ID for Employees**: Use Entra ID authentication for internal employees
5. **Local Accounts for Contractors**: Use local accounts for external contractors
6. **Admin Email List**: Keep `AdminEmails` list up to date when employees change roles

## Future Enhancements

Planned features for user management:

- [ ] Admin web UI for user approval
- [ ] Admin web UI for role assignment
- [ ] Email confirmation workflow
- [ ] Password reset email workflow
- [ ] User activity reports
- [ ] Bulk user import/export
- [ ] Integration with AD groups via Entra ID app roles

## Troubleshooting

### User Registers but Can't Upload

**Symptoms**: User can sign in but sees "Not Authorized" on Upload page

**Solutions**:
1. Check if `RequireApproval` is enabled and user needs approval
2. Verify user has "Uploader" or "Admin" role
3. Check application logs for authorization errors

### User Can't Register (Local Accounts)

**Symptoms**: Register button missing or registration fails

**Solutions**:
1. Verify `EnableLocalAccounts` is `true` in configuration
2. Check password meets requirements (8+ chars, uppercase, lowercase, number, special char)
3. Check for duplicate email address

### Entra ID User Not Authorized After Sign-In

**Symptoms**: User signs in successfully but gets "Not Authorized"

**Solutions**:
1. Add user email to `AdminEmails` or `UploaderRoles` in configuration
2. Check if `RequireApproval` is enabled (set to `false` for Entra ID auto-approval)
3. Verify role assignment in database

## Related Documentation

- [Entra ID Setup Guide](ENTRA_ID_SETUP.md)
- [Configuration Reference](../configuration/CONFIGURATION.md)
- [Security Guide](../configuration/SECURITY.md)

## Support

For user management issues:
1. Check application logs: `C:\ProgramData\ZLFileRelay\logs\`
2. Review database state via SQLite
3. Contact system administrator

