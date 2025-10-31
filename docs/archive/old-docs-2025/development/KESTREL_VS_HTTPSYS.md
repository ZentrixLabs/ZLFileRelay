# Kestrel vs HttpSys: Windows Authentication Deep Dive

## Quick Answer

**Switching from Kestrel to HttpSys is a MODERATE change, not massive.** It's primarily:
- ✅ **Security/Authentication handling change** (main benefit)
- ✅ **Configuration adjustments** (~50-100 lines of code)
- ⚠️ **Deployment requirement change** (URL reservations needed)

The rest of your application code (controllers, services, middleware) **stays exactly the same**.

---

## Key Differences

### Kestrel (Current)
- **Cross-platform** (.NET managed server)
- **User-mode** HTTP handling
- **Negotiate authentication** supported via middleware (managed code)
- **Works** for same-domain Windows Auth
- **Limited** for cross-domain scenarios (corp → DMZ)
  - Relies on managed Negotiate handler
  - Browser may not auto-prompt for credentials
  - NTLM fallback can be inconsistent

### HttpSys (Alternative)
- **Windows-only** (uses HTTP.sys kernel driver)
- **Kernel-mode** HTTP handling (part of Windows networking stack)
- **Native Windows Authentication** (integrated at OS level)
- **Excellent** for cross-domain scenarios
  - Uses Windows' native SSPI (Security Support Provider Interface)
  - Proper NTLM credential prompting
  - Better Kerberos/Negotiate handling
- **Requires URL reservations** (admin privilege for setup)

---

## What Changes?

### 1. Code Changes (~50-100 lines)

**Current (Kestrel):**
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = initialConfig.Security.MaxUploadSizeBytes;
    options.ListenAnyIP(kestrel.HttpsPort, listenOptions =>
    {
        listenOptions.UseHttps(cert);
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});
```

**With HttpSys:**
```csharp
builder.WebHost.UseHttpSys(options =>
{
    options.MaxRequestBodySize = initialConfig.Security.MaxUploadSizeBytes;
    options.UrlPrefixes.Add($"https://*:{kestrel.HttpsPort}/");
    options.UrlPrefixes.Add($"http://*:{kestrel.HttpPort}/");
    
    // Enable Windows Authentication at kernel level
    options.Authentication.Schemes = AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM;
    options.Authentication.AllowAnonymous = false;
});

// Certificate binding happens at OS level (via netsh/httpcfg)
// OR can be configured in code for HttpSys
```

### 2. Certificate Handling

**Kestrel:** Certificate loaded in application code (what you have now)

**HttpSys:** Two options:
- **Option A:** Use OS-level certificate binding (via `netsh` commands)
  ```powershell
  netsh http add sslcert ipport=0.0.0.0:8443 certhash=<thumbprint> appid={<guid>}
  ```
- **Option B:** Still configure in code (HttpSys supports this)

### 3. Deployment Requirements

**Kestrel:**
- ✅ Works out of the box
- ✅ No admin privileges needed (for non-privileged ports)
- ✅ Just start the service

**HttpSys:**
- ⚠️ **Requires URL reservations** (one-time setup, needs admin):
  ```powershell
  netsh http add urlacl url=https://*:8443/ user="NT AUTHORITY\SYSTEM"
  netsh http add urlacl url=http://*:8080/ user="NT AUTHORITY\SYSTEM"
  ```
- ⚠️ Or can be done programmatically (but still requires elevated privileges)
- ✅ After setup, runs as regular service

### 4. What DOESN'T Change

✅ All your middleware (authentication, authorization, routing)  
✅ All your Razor Pages  
✅ All your services and business logic  
✅ Your configuration structure  
✅ Your logging setup  

**Only the HTTP server layer changes.**

---

## Benefits for Your Cross-Domain Scenario

### Current Problem (Kestrel)
```
Corporate Domain → DMZ Domain
├── Kerberos: ❌ Fails (no domain trust)
├── NTLM fallback: ⚠️ Sometimes works, browser may not prompt
└── Result: "Hanging" / incomplete handshake
```

### With HttpSys
```
Corporate Domain → DMZ Domain
├── Kerberos: ❌ Still fails (expected)
├── NTLM fallback: ✅ Works reliably, proper OS-level prompting
├── Native SSPI: ✅ Windows handles authentication at kernel level
└── Result: ✅ Browser properly prompts for credentials
```

---

## Migration Complexity Assessment

| Aspect | Complexity | Notes |
|--------|-----------|-------|
| **Code Changes** | 🟡 Medium | ~50-100 lines, mostly configuration |
| **Testing Required** | 🟡 Medium | Need to test auth flows, but app logic unchanged |
| **Deployment** | 🟠 Higher | URL reservations needed (admin setup) |
| **Certificate Setup** | 🟡 Medium | May need to adjust binding method |
| **Risk** | 🟢 Low | HttpSys is mature, well-tested |

**Overall: Moderate change** - Not a rewrite, but not trivial either.

---

## Should You Switch?

### Switch to HttpSys if:
- ✅ Cross-domain authentication is critical for your use case
- ✅ You're willing to add URL reservation setup to deployment
- ✅ You're Windows-only (no cross-platform requirement)
- ✅ Better Windows Authentication is worth the migration

### Stay with Kestrel if:
- ✅ You want simpler deployment (no URL reservations)
- ✅ You might need cross-platform support later
- ✅ Current auth works for same-domain scenarios
- ✅ You can work around cross-domain issues (manual credential entry)

---

## Recommendation

For your **corp domain → DMZ domain** scenario, **HttpSys is probably worth it** if:
1. Users are experiencing authentication issues frequently
2. You can handle the URL reservation setup (one-time per server)
3. Cross-domain authentication is a core requirement

The change is **moderate** - mostly configuration and deployment, not a massive rewrite. Your application code stays the same.

---

## Implementation Estimate

If we were to implement this:
- **Code changes:** 2-3 hours
- **Testing:** 2-3 hours
- **Documentation updates:** 1 hour
- **Total:** ~6-7 hours of focused work

**Not a massive change, but not a 5-minute fix either.**

