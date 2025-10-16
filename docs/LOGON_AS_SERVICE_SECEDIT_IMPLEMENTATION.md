# Grant "Log on as a Service" Right - Safe secedit Implementation

## Overview

This implementation grants the "Log on as a service" right using `secedit.exe` in a **safe manner that preserves existing user rights**. The approach follows Windows Server best practices and works on Windows Server 2016+.

## ✅ Safe Implementation Method

### Step-by-Step Process

1. **Export current security policy**
   ```cmd
   secedit /export /cfg C:\temp\secconfig.cfg
   ```

2. **Read and parse the exported file**
   - Find line starting with: `SeServiceLogonRight = existing_account1,existing_account2`
   - Extract existing accounts

3. **Append new service account (SAFE - preserves existing)**
   ```
   SeServiceLogonRight = existing_account1,existing_account2,*S-1-5-21-xxxxx
   ```

4. **Apply the updated policy**
   ```cmd
   secedit /configure /db secedit.sdb /cfg C:\temp\secconfig.cfg /areas USER_RIGHTS
   ```

5. **Force policy update (optional)**
   ```cmd
   gpupdate /force
   ```

## 🔒 Key Safety Features

✅ **Preserves Existing Rights** - Appends to existing accounts, never overwrites  
✅ **SID-Based** - Uses Security Identifiers for reliability  
✅ **Duplicate Check** - Verifies account doesn't already have the right  
✅ **Admin Elevation** - Supports running with admin credentials when needed  
✅ **Detailed Logging** - Logs all existing accounts and changes made  
✅ **Rollback Safe** - Original policy remains unchanged in case of failure  

## Implementation Details

### Code Location
**File:** `src/ZLFileRelay.ConfigTool/Services/ServiceAccountManager.cs`

**Method:** `GrantLogonAsServiceRightSecEditAsync()`

### Process Flow

```
1. Resolve username → SID (*S-1-5-21-xxxxx)
   ↓
2. Export security policy → C:\Users\[User]\AppData\Local\Temp\ZLFileRelay\secconfig_export.cfg
   ↓
3. Parse exported file → Find SeServiceLogonRight line
   ↓
4. Check if SID already exists
   ├─ YES → Return success (no changes needed)
   └─ NO → Continue
   ↓
5. Append SID to existing accounts
   OLD: existing_account1,existing_account2
   NEW: existing_account1,existing_account2,*S-1-5-21-xxxxx
   ↓
6. Create new config → C:\Users\[User]\AppData\Local\Temp\ZLFileRelay\secconfig_new.cfg
   ↓
7. Apply updated policy
   secedit /configure /db secedit.sdb /cfg [config] /areas USER_RIGHTS
   ↓
8. Optional: Force group policy update
   gpupdate /force
   ↓
9. Clean up temp files
```

### Admin Credential Support

**Scenario 1: Running as Admin**
```csharp
// Direct execution - no elevation needed
await RunSeceditAsync("/export /cfg \"C:\\temp\\config.cfg\"");
```

**Scenario 2: Not Running as Admin**
```csharp
// Prompts for admin credentials
var credDialog = new CredentialDialog();
if (credDialog.ShowDialog() == true)
{
    // Execute with admin credentials
    await RunSeceditElevatedAsync("/export /cfg \"C:\\temp\\config.cfg\"", 
        credDialog.Username, 
        credDialog.Password);
}
```

## 📋 Detailed Logging

The implementation provides step-by-step logging:

```
[INFO] Granting 'Log on as a service' right to DOMAIN\svc_filerelay
[INFO] Resolved DOMAIN\svc_filerelay to SID: S-1-5-21-123456789-123456789-123456789-1001
[DEBUG] Admin elevation not needed
[INFO] Step 1/4: Exporting current security policy...
[INFO] Step 2/4: Reading current SeServiceLogonRight settings...
[INFO] Existing SeServiceLogonRight accounts: *S-1-5-20,*S-1-5-19,*S-1-5-80-0
[INFO] Step 3/4: Appending DOMAIN\svc_filerelay to existing accounts...
[DEBUG] Appending to existing accounts. New setting: *S-1-5-20,*S-1-5-19,*S-1-5-80-0,*S-1-5-21-123456789-123456789-123456789-1001
[DEBUG] Created configuration file at: C:\Users\admin\AppData\Local\Temp\ZLFileRelay\secconfig_new.cfg
[INFO] Step 4/4: Applying updated security policy...
[INFO] ✅ Successfully granted SeServiceLogonRight to DOMAIN\svc_filerelay
[INFO] Existing accounts were preserved and DOMAIN\svc_filerelay was appended to the list
[DEBUG] Forcing group policy update (optional)...
[INFO] Group policy updated successfully
[DEBUG] Cleaned up export file: C:\Users\admin\AppData\Local\Temp\ZLFileRelay\secconfig_export.cfg
[DEBUG] Cleaned up config file: C:\Users\admin\AppData\Local\Temp\ZLFileRelay\secconfig_new.cfg
```

## 📁 Temporary Files

Files are created in a predictable location for troubleshooting:

- **Export File:** `C:\Users\[User]\AppData\Local\Temp\ZLFileRelay\secconfig_export.cfg`
- **Config File:** `C:\Users\[User]\AppData\Local\Temp\ZLFileRelay\secconfig_new.cfg`

Both files are automatically cleaned up after operation (success or failure).

## 🛡️ Security Considerations

### Password Handling
- Passwords converted to `SecureString`
- No passwords written to disk
- Minimal exposure in process arguments

### SID Usage
- Uses Security Identifiers instead of account names
- More reliable across domains and name changes
- Format: `*S-1-5-21-[domain]-[rid]`

### Existing Rights Preservation
```csharp
// SAFE: Appends to existing
if (string.IsNullOrEmpty(currentSetting))
{
    newSetting = sidString;  // First account
}
else
{
    newSetting = $"{currentSetting},{sidString}";  // Append to existing
}
```

## 🧪 Testing

### Test Scenario 1: First Account
```
Existing: (empty)
New:      *S-1-5-21-xxxxx-1001
Result:   SeServiceLogonRight = *S-1-5-21-xxxxx-1001
```

### Test Scenario 2: Append to Existing
```
Existing: *S-1-5-20,*S-1-5-19
New:      *S-1-5-21-xxxxx-1001
Result:   SeServiceLogonRight = *S-1-5-20,*S-1-5-19,*S-1-5-21-xxxxx-1001
```

### Test Scenario 3: Already Has Right
```
Existing: *S-1-5-20,*S-1-5-21-xxxxx-1001
New:      *S-1-5-21-xxxxx-1001
Result:   No changes (already has right)
```

## 📊 Common SIDs

For reference, common built-in account SIDs:

- `*S-1-5-18` - LocalSystem
- `*S-1-5-19` - LocalService (NT AUTHORITY\LocalService)
- `*S-1-5-20` - NetworkService (NT AUTHORITY\NetworkService)
- `*S-1-5-80-0` - NT SERVICE\ALL SERVICES
- `*S-1-5-21-[domain]-[rid]` - Domain/local user accounts

## ⚠️ Important Notes

1. **Never manually edit exported policy files** - Use this automated method
2. **Always verify existing accounts** - They are preserved and logged
3. **Requires admin privileges** - Either run as admin or provide admin credentials
4. **Domain accounts** - Use format: `DOMAIN\Username`
5. **Local accounts** - Use format: `.\Username` or `COMPUTERNAME\Username`

## 🔄 Comparison: Old vs New

### ❌ Old Method (Unsafe)
```csharp
// Would overwrite existing accounts
SeServiceLogonRight = *S-1-5-21-xxxxx-1001
```

### ✅ New Method (Safe)
```csharp
// Preserves existing accounts
SeServiceLogonRight = existing1,existing2,*S-1-5-21-xxxxx-1001
```

## 📚 References

- [Microsoft Docs: secedit](https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/secedit)
- [Security Policy Settings](https://docs.microsoft.com/en-us/windows/security/threat-protection/security-policy-settings/log-on-as-a-service)
- [Well-known SIDs](https://docs.microsoft.com/en-us/windows/security/identity-protection/access-control/security-identifiers)

## 🎯 Usage in Config Tool

1. Open **ZL File Relay Config Tool**
2. Navigate to **Service Account** tab
3. Enter service account credentials
4. Click **Grant Logon Right** button
5. If not admin: Enter admin credentials when prompted
6. View detailed progress in log window
7. Verify success message

The implementation is now **production-ready** and follows Windows Server best practices! ✅

