# MEDIUM Severity Issues - Quick Summary

**Status:** 📋 **NOT YET ADDRESSED**  
**Priority:** Optional (can be done in next sprint)  
**Risk Level:** 🟡 **LOW-MEDIUM** (Good security hygiene, not urgent)

---

## Overview

There are **5 MEDIUM severity issues** remaining. These are **nice-to-have security improvements** but not production blockers. You can safely deploy after testing the CRITICAL and HIGH fixes we just completed.

---

## MEDIUM-1: Sensitive Data in Logs ℹ️

**Files:** Multiple (`ScpFileTransferService.cs`, `AuthorizationService.cs`, others)  
**Risk Level:** 🟡 Information Disclosure

### The Issue

Several places log potentially sensitive information:

1. **SSH Commands:**
   ```csharp
   _logger.LogDebug("Executing SCP command: scp.exe {Arguments}", scpArgs);
   // Logs: scp.exe -i "C:\ProgramData\ZLFileRelay\ssh\id_ed25519" ...
   // Exposes: Private key path
   ```

2. **User Groups:**
   ```csharp
   _logger.LogDebug("User {User} groups: {Groups}", user.Identity?.Name, userGroups);
   // Exposes: Internal AD group structure
   ```

3. **File Paths:**
   - Full paths may contain sensitive directory names
   - Could reveal internal network structure

### Impact
- **Low:** Only in DEBUG logs (not production default)
- Information could help an attacker map the environment
- Useful for insider threat reconnaissance

### Fix Complexity
**Easy** - Add log sanitization helper (20 lines)

### Recommendation
Create a `LoggingHelper` class:
```csharp
public static class LoggingHelper
{
    public static string SanitizePath(string path)
    {
        return Path.GetFileName(path); // Show only filename
    }
    
    public static string SanitizeCommandLine(string command)
    {
        // Redact private key paths
        return Regex.Replace(command, @"-i\s+""[^""]+""", "-i \"[REDACTED]\"");
    }
}
```

---

## MEDIUM-2: No Anti-CSRF Protection ⚠️

**File:** `src/ZLFileRelay.WebPortal/Pages/Upload.cshtml`  
**Risk Level:** 🟡 Cross-Site Request Forgery

### The Issue

File upload forms don't include anti-forgery tokens:

```html
<form method="post" enctype="multipart/form-data" id="uploadForm">
    <!-- Missing: @Html.AntiForgeryToken() -->
    <div asp-validation-summary="All" class="text-danger"></div>
```

### Attack Scenario

1. Attacker creates malicious website
2. Tricks authenticated user into visiting
3. Hidden form auto-submits to your upload endpoint
4. Uploads malicious file under victim's identity

### Impact
- **Medium:** Requires user to be authenticated AND visit malicious site
- Could upload unwanted files
- Could abuse user's upload quota
- Logs would show legitimate user uploaded file

### Fix Complexity
**Easy** - Add token to forms (2 lines per form)

### Recommendation

**In Upload.cshtml:**
```html
<form method="post" enctype="multipart/form-data" id="uploadForm">
    @Html.AntiForgeryToken()  <!-- ADD THIS -->
    <div asp-validation-summary="All" class="text-danger"></div>
```

**In Upload.cshtml.cs:**
```csharp
[ValidateAntiForgeryToken]  // ADD THIS
public async Task<IActionResult> OnPostAsync()
{
    // ... existing code
}
```

---

## MEDIUM-3: Weak SSH Host Validation 🔐

**File:** `src/ZLFileRelay.Service/Services/ScpFileTransferService.cs` (Line 467)  
**Risk Level:** 🟡 Potential Misconfiguration

### The Issue

SSH hostname validation doesn't handle IP addresses:

```csharp
// Current regex only validates hostnames
if (!Regex.IsMatch(host, @"^[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?..."))
    throw new ArgumentException($"Invalid SSH hostname format: {host}");
```

**Problems:**
- Single-character hostnames allowed (technically valid but unusual)
- IPv4 addresses fail validation (e.g., `192.168.1.100`)
- IPv6 addresses fail validation (e.g., `2001:db8::1`)

### Impact
- **Low:** Might reject valid IP addresses
- Forces use of hostnames only
- User experience issue more than security

### Fix Complexity
**Easy** - Expand validation (10 lines)

### Recommendation

```csharp
private static string ValidateSshHost(string host)
{
    if (string.IsNullOrWhiteSpace(host))
        throw new ArgumentException("SSH host cannot be null or empty");

    host = host.Trim();

    // Check if it's an IP address
    if (System.Net.IPAddress.TryParse(host, out var ipAddress))
    {
        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ||
            ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return host; // Valid IP
        }
    }

    // Check hostname (minimum 2 characters for real-world usage)
    if (host.Length < 2)
        throw new ArgumentException($"SSH hostname too short: {host}");

    // Existing hostname validation...
    return host;
}
```

---

## MEDIUM-4: Missing File Upload Rate Limiting 🚦

**File:** `src/ZLFileRelay.WebPortal/Services/FileUploadService.cs`  
**Risk Level:** 🟡 Denial of Service

### The Issue

No rate limiting on file uploads:

```json
// Config has setting but not enforced:
"MaxConcurrentUploads": 10  // Not actually used!
```

### Attack Scenario

1. Attacker (or malicious insider) spams upload endpoint
2. Uploads thousands of files rapidly
3. Exhausts disk space, memory, or CPU
4. Legitimate users can't upload

### Impact
- **Low-Medium:** Requires authenticated access
- Could cause service degradation
- Disk space exhaustion possible
- No automatic mitigation

### Fix Complexity
**Medium** - Add ASP.NET Core rate limiter (30 lines)

### Recommendation

**In Program.cs:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("upload", limiterOptions =>
    {
        limiterOptions.PermitLimit = appConfig.WebPortal.MaxConcurrentUploads;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0; // No queuing
    });
});
```

**In Upload.cshtml.cs:**
```csharp
[EnableRateLimiting("upload")]  // ADD THIS
public async Task<IActionResult> OnPostAsync()
{
    // ... existing code
}
```

---

## MEDIUM-5: Insufficient File Extension Validation 📁

**File:** `src/ZLFileRelay.WebPortal/Services/FileUploadService.cs` (Lines 51-55)  
**Risk Level:** 🟡 Potential Bypass

### The Issue

File extension validation can be bypassed:

```csharp
var extension = Path.GetExtension(fileName).ToLowerInvariant();
if (_config.WebPortal.BlockedFileExtensions.Contains(extension))
    // Block file
```

**Bypass Techniques:**
- Double extensions: `malicious.txt.exe`
- Alternate data streams: `file.txt::$DATA`
- Null bytes: `malicious.exe%00.txt` (though .NET handles this)
- Unicode normalization attacks

### Impact
- **Low:** Additional validation already exists (file size, path validation)
- Could bypass extension blocklist
- Other defenses would still catch most issues

### Fix Complexity
**Easy** - Enhanced validation (20 lines)

### Recommendation

```csharp
private bool IsFileExtensionAllowed(string fileName)
{
    var extension = Path.GetExtension(fileName).ToLowerInvariant();
    
    // Check for multiple extensions (double extension attack)
    var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
    if (nameWithoutExtension.Contains('.'))
    {
        var allExtensions = fileName.Split('.')
            .Skip(1)
            .Select(e => "." + e.ToLowerInvariant());
        
        if (allExtensions.Any(e => _config.WebPortal.BlockedFileExtensions.Contains(e)))
        {
            _logger.LogWarning("Blocked file with multiple extensions: {FileName}", fileName);
            return false;
        }
    }
    
    // Check for NTFS alternate data streams
    if (fileName.Contains("::") || fileName.Contains(":$"))
    {
        _logger.LogWarning("Blocked file with alternate data stream: {FileName}", fileName);
        return false;
    }
    
    // Check for null bytes
    if (fileName.Contains('\0'))
    {
        _logger.LogWarning("Blocked file with null byte: {FileName}", fileName);
        return false;
    }
    
    // Normal extension check
    if (_config.WebPortal.BlockedFileExtensions.Contains(extension))
        return false;
    
    return true;
}
```

---

## Priority Recommendation

### Deploy Now ✅
You can safely deploy to production after testing the CRITICAL and HIGH fixes. The MEDIUM issues are **not blockers**.

### Address in Next Sprint 📅

**Priority Order:**

1. **MEDIUM-2 (CSRF)** - Easy fix, meaningful security improvement
   - **Effort:** 30 minutes
   - **Benefit:** Protects against common web attack

2. **MEDIUM-5 (File Validation)** - Easy fix, defense in depth
   - **Effort:** 30 minutes
   - **Benefit:** Hardens upload validation

3. **MEDIUM-1 (Log Sanitization)** - Good security hygiene
   - **Effort:** 1 hour
   - **Benefit:** Reduces information disclosure

4. **MEDIUM-4 (Rate Limiting)** - Medium effort, good protection
   - **Effort:** 1-2 hours
   - **Benefit:** Prevents upload DoS

5. **MEDIUM-3 (SSH Validation)** - Low priority UX fix
   - **Effort:** 30 minutes
   - **Benefit:** Accepts IP addresses

**Total Time:** ~4-5 hours for all five

---

## Risk Assessment

### Current State (with CRITICAL + HIGH fixes):
```
Critical Issues:  ✅ 0
High Issues:      ✅ 0
Medium Issues:    📋 5 (acceptable for production)
Overall Risk:     🟢 LOW
```

### With MEDIUM Fixes:
```
Critical Issues:  ✅ 0
High Issues:      ✅ 0
Medium Issues:    ✅ 0
Overall Risk:     🟢 VERY LOW (excellent security posture)
```

---

## Comparison: What Matters Most

| Issue | Risk | Impact | Effort | Priority |
|-------|------|--------|--------|----------|
| ~~CRITICAL-1~~ | ~~🔴 RCE~~ | ~~Complete compromise~~ | ~~Done~~ | ✅ Fixed |
| ~~CRITICAL-2~~ | ~~🔴 Creds~~ | ~~Credential theft~~ | ~~Done~~ | ✅ Fixed |
| ~~HIGH-1~~ | ~~🔴 Broken~~ | ~~Production blocker~~ | ~~Done~~ | ✅ Fixed |
| ~~HIGH-2~~ | ~~🟡 UX~~ | ~~Misconfiguration~~ | ~~Done~~ | ✅ Fixed |
| ~~HIGH-3~~ | ~~🟡 Injection~~ | ~~Path traversal~~ | ~~Done~~ | ✅ Fixed |
| MEDIUM-1 | 🟢 Info | Logs reveal info | Easy | Optional |
| MEDIUM-2 | 🟡 CSRF | File upload abuse | Easy | Recommended |
| MEDIUM-3 | 🟢 UX | IP addresses fail | Easy | Optional |
| MEDIUM-4 | 🟡 DoS | Upload spam | Medium | Recommended |
| MEDIUM-5 | 🟡 Bypass | Extension tricks | Easy | Recommended |

---

## Bottom Line

**You've already fixed the important stuff!** 🎉

- ✅ Remote code execution: ELIMINATED
- ✅ Credential theft: ELIMINATED
- ✅ Production blockers: ELIMINATED
- ✅ Path traversal: ELIMINATED
- ✅ Misconfiguration: ELIMINATED

The MEDIUM issues are **security best practices** and **nice-to-haves**, not urgent problems.

### Recommendation:
1. **Test and deploy the CRITICAL + HIGH fixes** (you're ready!)
2. **Address MEDIUM issues in next sprint** (security polish)
3. **Focus on MEDIUM-2 and MEDIUM-5 first** (easy wins)

---

**Want me to implement any of these MEDIUM fixes?** Let me know which ones interest you!
