# Service Account Configuration - sc.exe Implementation

## Overview

The service account configuration has been refactored to use **only** the `sc.exe` command for maximum compatibility across different Windows Server environments. This is the most reliable method for older and locked-down environments.

## Key Changes

### 1. Simplified ServiceAccountManager

**File:** `src/ZLFileRelay.ConfigTool/Services/ServiceAccountManager.cs`

**Changes:**
- Removed complex native Windows API calls (P/Invoke)
- Removed PowerShell-based fallbacks
- Removed LSA API code for logon rights
- **Primary method:** Uses `sc.exe config` command exclusively

**Method Signature:**
```csharp
public async Task<bool> SetServiceAccountAsync(
    string username, 
    string password, 
    string? adminUsername = null, 
    string? adminPassword = null)
```

**How it works:**
1. Checks if current user is running as administrator
2. If YES: Runs `sc.exe` directly with current credentials
3. If NO: Uses provided admin credentials to run `sc.exe` elevated

### 2. Admin Credential Prompting

**File:** `src/ZLFileRelay.ConfigTool/Dialogs/CredentialDialog.xaml`

A new dialog has been added to prompt for admin credentials when the current user is not an administrator.

**Features:**
- Clean, modern UI
- Pre-fills domain from current user
- Input validation
- Secure password handling (SecureString)

### 3. Updated ViewModel

**File:** `src/ZLFileRelay.ConfigTool/ViewModels/ServiceAccountViewModel.cs`

**Changes:**
- Checks if running as admin before setting service account
- Prompts for admin credentials if needed
- Passes admin credentials to ServiceAccountManager
- Applies to both:
  - `SetServiceAccountAsync` - Setting the service account
  - `GrantLogonRightAsync` - Granting logon as service right

### 4. Logon as Service Right

**Implementation:** Uses `secedit.exe` with optional admin elevation

**Process:**
1. Export current security policy using `secedit.exe /export`
2. Parse the exported file to find current SeServiceLogonRight settings
3. Add the new user's SID if not already present
4. Apply the updated policy using `secedit.exe /configure`
5. Clean up temporary files

## Command Examples

### Setting Service Account
```cmd
sc.exe config "ZLFileRelay" obj= "DOMAIN\User" password= "YourPassword"
```

### For Remote Server
```cmd
sc.exe \\RemoteServer config "ZLFileRelay" obj= "DOMAIN\User" password= "YourPassword"
```

## Security Considerations

### Password Handling
- Passwords are converted to `SecureString` before being passed to Process.Start
- Passwords are never written to disk
- Command line arguments may briefly expose passwords in process list

### Admin Credential Elevation
When not running as admin:
1. User is prompted for admin credentials via dialog
2. Credentials are passed to `Process.Start` using:
   - `Domain` property
   - `UserName` property
   - `Password` property (SecureString)
3. Process runs with elevated privileges

## Compatibility

This implementation is compatible with:
- Windows Server 2012 and later
- Windows Server 2016 (most common in production)
- Windows Server 2019/2022
- Locked-down environments where PowerShell may be restricted
- Environments without .NET Framework 4.8 or PowerShell 5.1+

## Usage Flow

### In Config Tool:

1. **User enters service account credentials**
   - Domain\Username
   - Password

2. **User clicks "Set Service Account"**

3. **System checks admin status:**
   - **If Admin:** Proceeds directly
   - **If Not Admin:** Shows credential dialog

4. **Admin credentials dialog (if needed):**
   - User enters: `DOMAIN\AdminUser`
   - User enters: Admin password
   - Click OK

5. **System executes:**
   ```
   sc.exe config "ZLFileRelay" obj= "DOMAIN\ServiceAccount" password= "ServicePassword"
   ```

6. **Success/Failure message displayed**

## Testing

To test the implementation:

1. **Running as Admin:**
   ```
   Right-click ConfigTool -> Run as Administrator
   Navigate to Service Account tab
   Enter credentials and click "Set Service Account"
   Should succeed without prompting
   ```

2. **Running as Regular User:**
   ```
   Open ConfigTool normally (not as admin)
   Navigate to Service Account tab
   Enter service account credentials
   Click "Set Service Account"
   Should prompt for admin credentials
   Enter admin credentials
   Should succeed
   ```

3. **Remote Server:**
   ```
   Connect to remote server using Remote Management
   Try setting service account
   Should work the same way
   ```

## Troubleshooting

### "Access Denied" Error
- Ensure admin credentials are correct
- Verify admin account has permissions on target server
- Check if service exists: `sc.exe query ZLFileRelay`

### "The specified service does not exist"
- Install the service first using the Service Management tab
- Verify service name: `sc.exe query type= service state= all | findstr ZLFileRelay`

### Password Not Working
- Verify password doesn't contain special characters that need escaping
- Test manually: `sc.exe config ServiceName obj= "DOMAIN\User" password= "Pass"`

### Logon as Service Right Fails
- Requires admin privileges
- Check if secedit.exe is available: `where secedit`
- Verify user SID can be resolved: `whoami /user`

## Files Modified

1. `src/ZLFileRelay.ConfigTool/Services/ServiceAccountManager.cs` - Simplified to sc.exe only
2. `src/ZLFileRelay.ConfigTool/ViewModels/ServiceAccountViewModel.cs` - Added admin prompting
3. `src/ZLFileRelay.ConfigTool/Dialogs/CredentialDialog.xaml` - New credential dialog (created)
4. `src/ZLFileRelay.ConfigTool/Dialogs/CredentialDialog.xaml.cs` - Dialog code-behind (created)

## Benefits

✅ **Maximum Compatibility** - Works on all Windows environments  
✅ **No PowerShell Dependency** - Works even if PowerShell is restricted  
✅ **No Complex P/Invoke** - Simpler, more maintainable code  
✅ **Admin Credential Support** - Handles non-admin users gracefully  
✅ **Remote Server Support** - Works with remote management  
✅ **Battle-Tested** - sc.exe is a proven Windows utility  

## Migration Notes

If upgrading from previous version:
- Old implementations used native Windows API or PowerShell
- New implementation is backwards compatible
- No configuration changes needed
- Admin credential prompting is new feature

