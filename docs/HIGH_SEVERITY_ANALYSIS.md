# HIGH Severity Issues - Detailed Analysis

**Before we fix anything, we need to understand what the code is trying to accomplish**

---

## HIGH-1: DPAPI Encryption Scope Mismatch

### Location
`src/ZLFileRelay.Core/Services/CredentialProvider.cs`, lines 155-173

### What It's Trying To Do

**Business Requirement:**  
Store sensitive credentials (SMB passwords, SSH passphrases) securely on disk so that:
1. **ConfigTool** can save them (runs as Administrator)
2. **Service** can read them (runs as service account)
3. Credentials are encrypted at rest
4. No plaintext passwords in configuration files

**Technical Process:**
1. Accept credential key-value pairs
2. Encrypt using Windows DPAPI (Data Protection API)
3. Store encrypted bytes as JSON
4. Later, decrypt and return the credential

### Current Implementation

```csharp
private static byte[] ProtectData(string data)
{
    byte[] dataBytes = Encoding.UTF8.GetBytes(data);
    return ProtectedData.Protect(dataBytes, null, DataProtectionScope.CurrentUser);  // Line 158
}

private static string UnprotectData(byte[] encryptedData)
{
    try
    {
        byte[] dataBytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);  // Line 165
        return Encoding.UTF8.GetString(dataBytes);
    }
    catch (CryptographicException ex)
    {
        throw new CryptographicException(
            "Failed to decrypt credentials. Ensure the service is running as the correct user.", ex);
    }
}
```

### The Problem Explained

**DataProtectionScope.CurrentUser:**
- Encrypts data using keys tied to the **current Windows user profile**
- Keys stored in: `C:\Users\{username}\AppData\Roaming\Microsoft\Protect`
- Only that specific user can decrypt the data

**The Scenario:**
1. **Administrator** runs ConfigTool and saves SMB credentials
   - Encrypted with Administrator's user profile keys
   - Stored in `credentials.json`
2. **Service Account** (e.g., `DOMAIN\svc_filerelay`) runs the Windows Service
   - Tries to read `credentials.json`
   - Attempts to decrypt with service account's profile keys
   - ❌ **FAILS** - different user, different keys!

**Error Message:**
```
Failed to decrypt credentials. Ensure the service is running as the correct user.
```

This error message is actually describing the problem! But the fix isn't to run as the same user - it's to use LocalMachine scope.

### Available Options

**Option 1: DataProtectionScope.LocalMachine** (RECOMMENDED)
- Encrypts data using machine-wide keys
- Any user on the machine can decrypt (if they have file access)
- Keys stored in: `C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys`
- **Requires:** File ACLs to restrict access

**Pros:**
- ConfigTool and Service can both access credentials
- Standard Windows security model
- No user dependency

**Cons:**
- Any administrator could decrypt if they get file access
- Must secure the file with proper ACLs

**Option 2: Keep CurrentUser + Run service as Administrator**
- Service runs under same account that configured it
- No code changes needed

**Cons:**
- ❌ Services should NOT run as Administrator (principle of least privilege)
- ❌ Defeats the purpose of service accounts
- ❌ Security best practice violation

**Option 3: Azure Key Vault / Hardware Security Module**
- Enterprise-grade credential storage
- Centralized management

**Cons:**
- ❌ Overkill for this use case
- ❌ Requires cloud connectivity
- ❌ Additional complexity and cost

### The Fix Strategy

**Use DataProtectionScope.LocalMachine + File ACLs**

1. Change `ProtectData()` to use `LocalMachine` scope
2. Change `UnprotectData()` to use `LocalMachine` scope  
3. Add `SetFilePermissions()` method to secure the credentials file
4. Call it after `SaveCredentials()`

**File Permission Requirements:**
- **SYSTEM:** Full Control (service runs as SYSTEM or specific account)
- **Administrators:** Full Control (ConfigTool needs to write)
- **Remove:** All other users and inherited permissions

This ensures only administrators and the service can access the encrypted file.

---

## HIGH-2: Insufficient Authorization Check

### Location
`src/ZLFileRelay.WebPortal/Services/AuthorizationService.cs`, lines 90-106

### What It's Trying To Do

**Business Requirement:**  
Control who can upload files via the web portal using Windows Authentication:
- Option A: List specific allowed usernames
- Option B: List specific allowed AD groups
- Option C: Both (union - user in EITHER list grants access)

**Technical Process:**
1. User authenticates via Windows Authentication
2. Check if username is in `AllowedUsers` list → grant access
3. If not, check if user is member of any `AllowedGroups` → grant access
4. Otherwise, deny access

### Current Implementation

```csharp
public bool IsUserAllowed(ClaimsPrincipal user)
{
    // Check if user is in allowed users list
    if (_config.WebPortal.AllowedUsers != null && _config.WebPortal.AllowedUsers.Any())
    {
        var userName = user.Identity?.Name?.ToLowerInvariant();
        if (userName != null && _config.WebPortal.AllowedUsers
            .Any(u => u.ToLowerInvariant() == userName))
        {
            _logger.LogInformation("User {User} authorized via user list", user.Identity?.Name);
            return true;
        }
    }

    // Check group membership
    return IsUserInAllowedGroups(user);
}

public bool IsUserInAllowedGroups(ClaimsPrincipal user)
{
    var allowedGroups = _config.WebPortal.AllowedGroups;

    if (allowedGroups == null || !allowedGroups.Any())
    {
        _logger.LogWarning("No allowed groups configured - denying access");
        return false;
    }
    
    // ... check group membership ...
}
```

### The Problem Explained

**Scenario 1: Both Lists Empty (CURRENT BEHAVIOR)**
```json
{
  "AllowedUsers": [],
  "AllowedGroups": []
}
```

**What happens:**
1. Check AllowedUsers list → empty, skip to next check
2. Call `IsUserInAllowedGroups()` → returns `false` (no groups configured)
3. User denied

**Result:** ✅ Secure (denies all), but **not obvious** to administrator

**Scenario 2: Only AllowedUsers Configured**
```json
{
  "AllowedUsers": ["DOMAIN\\john", "DOMAIN\\jane"],
  "AllowedGroups": []
}
```

**What happens:**
1. Check if user in list → Yes: grant access | No: check groups
2. If not in user list, call `IsUserInAllowedGroups()` → returns `false`
3. User denied (unless in user list)

**Result:** ✅ Works as expected

**Scenario 3: Only AllowedGroups Configured**
```json
{
  "AllowedUsers": [],
  "AllowedGroups": ["DOMAIN\\FileUpload_Users"]
}
```

**What happens:**
1. AllowedUsers is empty → skip to groups
2. Call `IsUserInAllowedGroups()` → checks membership
3. Grant access if in group

**Result:** ✅ Works as expected

### So What's The Issue?

The code **works correctly**, but has **poor user experience:**

1. **No Clear Error:** If both lists are empty, logs show "No allowed groups configured" but not "SECURITY ERROR: No authorization configured!"

2. **Silent Failure:** Administrator might not realize they've locked everyone out

3. **No Validation:** Configuration is accepted even if it denies all users

4. **Misleading Log:** "No allowed groups configured - denying access" appears even when groups aren't being used

### The Fix Strategy

**Add Explicit Configuration Validation**

1. Check if **at least one** authorization method is configured
2. If neither list has entries, log **SECURITY ERROR** and deny all
3. Provide clear feedback to administrators
4. Consider: Add configuration validation in `ConfigurationService`

**Benefits:**
- Makes misconfiguration obvious
- Helps administrators during setup
- Clearer security intent in logs
- Prevents accidental lockouts

---

## HIGH-3: Path Traversal in UNC Path

### Location
`src/ZLFileRelay.ConfigTool/Services/ConfigurationService.cs`, line 48

### What It's Trying To Do

**Business Requirement:**  
ConfigTool can manage ZLFileRelay installations on remote servers:
1. User enters remote server name
2. ConfigTool accesses configuration via UNC path
3. Read/write `appsettings.json` on remote server

**Expected Paths:**
- Local: `C:\Program Files\ZLFileRelay\appsettings.json`
- Remote: `\\SERVER01\c$\Program Files\ZLFileRelay\appsettings.json`

### Current Implementation

```csharp
private void UpdateConfigPath()
{
    if (!_remoteServerProvider.IsRemote || string.IsNullOrWhiteSpace(_remoteServerProvider.ServerName))
    {
        // Local mode
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
    }
    else
    {
        // Remote mode - try standard installation path
        var serverName = _remoteServerProvider.ServerName;
        _configPath = $@"\\{serverName}\c$\Program Files\ZLFileRelay\appsettings.json";  // Line 48
        
        _logger.LogInformation("Remote mode enabled, using UNC path: {Path}", _configPath);
    }
}
```

### The Problem Explained

**UNC Path Injection Attack:**

If `serverName` is not validated, an attacker could enter:
```
ServerName: "evil.com\share\..\..\..\Windows\System32\config"
```

**Resulting Path:**
```
\\evil.com\share\..\..\..\Windows\System32\config\c$\Program Files\ZLFileRelay\appsettings.json
```

After path normalization, this could potentially access:
```
\\evil.com\share\..\..\..\ = \\evil.com\
Then navigate to sensitive files
```

**Real Attack Scenarios:**

1. **Path Traversal:**
   ```
   ServerName: "legitimate-server\..\..\..\c$\Windows\System32"
   Result: Access system files instead of application config
   ```

2. **Alternative Share Access:**
   ```
   ServerName: "legitimate-server\admin$\..\"
   Result: Access admin share instead of c$
   ```

3. **External Attacker Server:**
   ```
   ServerName: "attacker-controlled-server.com"
   Result: ConfigTool sends credentials to attacker's server
   ```

### How Realistic Is This?

**Attack Requirements:**
1. Attacker needs access to ConfigTool (requires Administrator privileges)
2. Attacker needs to modify the server name field

**But:**
- If attacker has admin rights, they have bigger problems
- However, this could be exploited via:
  - Social engineering (trick admin into entering malicious server)
  - Supply chain attack (compromised configuration file)
  - Insider threat

**Risk Level:** **Medium-High**
- Requires admin access (reduces likelihood)
- But could lead to credential theft (increases impact)

### The Fix Strategy

**Validate Server Name Format**

Add validation to ensure server name is a valid hostname or IP address:

```csharp
private static bool IsValidServerName(string serverName)
{
    // Check for null or whitespace
    if (string.IsNullOrWhiteSpace(serverName))
        return false;
    
    // Check for path traversal characters
    if (serverName.Contains("..") || serverName.Contains("/") || serverName.Contains("\\"))
        return false;
    
    // Check for alternative data streams or special characters
    if (serverName.Contains(":") || serverName.Contains("$"))
        return false;
    
    // Validate as hostname (DNS name)
    var hostnamePattern = @"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$";
    if (System.Text.RegularExpressions.Regex.IsMatch(serverName, hostnamePattern))
        return true;
    
    // Validate as IP address (IPv4 or IPv6)
    if (System.Net.IPAddress.TryParse(serverName, out var ipAddress))
    {
        return ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ||
               ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
    }
    
    return false;
}
```

**Apply Validation:**
```csharp
private void UpdateConfigPath()
{
    if (!_remoteServerProvider.IsRemote || string.IsNullOrWhiteSpace(_remoteServerProvider.ServerName))
    {
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
    }
    else
    {
        var serverName = _remoteServerProvider.ServerName;
        
        // SECURITY: Validate server name format
        if (!IsValidServerName(serverName))
        {
            _logger.LogError("Invalid server name format: {ServerName}", serverName);
            throw new ArgumentException($"Invalid server name format: {serverName}");
        }
        
        _configPath = $@"\\{serverName}\c$\Program Files\ZLFileRelay\appsettings.json";
        _logger.LogInformation("Remote mode enabled, using UNC path: {Path}", _configPath);
    }
}
```

---

## Summary of Intended Behaviors

### HIGH-1: DPAPI Encryption
- **Input:** Credential key and value
- **Output:** Encrypted bytes stored in JSON file
- **Requirement:** Both ConfigTool and Service must be able to decrypt
- **Current Issue:** Only works if both run as same user (they don't)
- **Fix:** Use LocalMachine scope + file ACLs

### HIGH-2: Authorization Check
- **Input:** Authenticated Windows user
- **Output:** Boolean - allow or deny access
- **Requirement:** At least one authorization method must be configured
- **Current Issue:** Silent failure if misconfigured (both lists empty)
- **Fix:** Explicit validation and clear error messaging

### HIGH-3: UNC Path Construction
- **Input:** Remote server name
- **Output:** UNC path to config file
- **Requirement:** Only valid hostnames/IPs should be accepted
- **Current Issue:** No validation allows path traversal
- **Fix:** Regex validation for hostname format

---

## Questions Before Implementation

### HIGH-1 (DPAPI):
1. **Where is the credentials file stored?**
   - Need to check where `credentialFilePath` points to
   - Ensure directory has proper permissions

2. **What happens to existing credentials?**
   - Encrypted with CurrentUser can't be decrypted with LocalMachine
   - May need migration script or "re-enter credentials" message

3. **Should we add entropy parameter?**
   - DPAPI supports optional entropy (additional secret)
   - Currently using `null` for entropy parameter

### HIGH-2 (Authorization):
1. **Should we fail startup if misconfigured?**
   - Deny all access (current behavior)
   - vs Throw exception and refuse to start
   - vs Allow all access (dangerous!)

2. **Should we add config validation to ConfigTool?**
   - Warn user when both lists are empty
   - Require at least one entry before saving

### HIGH-3 (UNC Path):
1. **Should we also validate the path after construction?**
   - Use `Path.GetFullPath()` and check for traversal
   - Additional safety layer

2. **What about NetBIOS names?**
   - Short names like "SERVER01" are valid
   - But don't match FQDN regex pattern

---

## Implementation Priority

**Recommended Order:**

1. **HIGH-2 (Authorization)** - Easiest, high impact for usability
   - Small code change
   - Prevents common misconfiguration
   - Clear security improvement

2. **HIGH-3 (UNC Path)** - Medium difficulty, prevents injection
   - Add validation method
   - Apply in one location
   - Improves security posture

3. **HIGH-1 (DPAPI)** - Most complex, requires careful handling
   - Change encryption scope
   - Add file ACL management
   - Handle migration of existing credentials
   - Test thoroughly (could break existing deployments)

---

## Ready to Proceed?

Once you approve the analysis, I'll implement the fixes in the recommended order, with proper error handling, logging, and testing guidance for each.
