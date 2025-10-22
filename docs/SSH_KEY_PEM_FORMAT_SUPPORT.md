# SSH Key Format Support Enhancement - .key and .pem Extensions

## Summary
Added explicit support and documentation for `.key` and `.pem` file extensions for SSH private keys, addressing the common web server convention where:
- Private key: `server.key` or `private.pem`
- Public key/certificate: `server.pem` or `public.pem`

## Background

### Common Naming Conventions
Different systems use different naming conventions for SSH keys:

**OpenSSH (Linux/Unix):**
- Private: `id_ed25519` (no extension)
- Public: `id_ed25519.pub`

**Web Servers (Apache/Nginx):**
- Private: `server.key` or `domain.key` (PEM format)
- Certificate/Public: `server.pem`, `domain.pem`, `server.crt`

**Generic PEM:**
- Private: `private_key.pem` or `mykey.pem`
- Public: `public_key.pem` or `mykey.pub`

All these formats are **PEM-encoded** under the hood - the difference is just the file extension naming convention.

## Changes Made

### 1. Browse Dialog Filter Enhancement

**Before:**
```csharp
Filter = "SSH Private Keys (id_*;*_key;*.pem)|id_*;*_key;*.pem|All Files (*.*)|*.*"
```

**After:**
```csharp
Filter = "SSH Private Keys (*.key;*.pem;id_*;*_key)|*.key;*.pem;id_*;*_key|" +
         "Key Files (*.key)|*.key|" +
         "PEM Files (*.pem)|*.pem|" +
         "All Files (*.*)|*.*"
```

**Benefits:**
- ✅ `.key` files appear first (most common web server format)
- ✅ `.pem` files explicitly supported
- ✅ Separate filter categories for each extension
- ✅ OpenSSH style (no extension) still supported

### 2. Additional File Type Warnings

Added detection for certificate files that users might mistakenly select:

```csharp
// Warn if they selected a .crt or .cer file (certificate, not private key)
else if (dialog.FileName.EndsWith(".crt", StringComparison.OrdinalIgnoreCase) ||
         dialog.FileName.EndsWith(".cer", StringComparison.OrdinalIgnoreCase))
{
    SshTestResult = "⚠️ Warning: You selected a certificate file (.crt/.cer). " +
                  "Please select the PRIVATE key file (usually .key or .pem) instead.";
}
```

This helps prevent confusion when users have both keys and certificates in the same folder.

### 3. UI Examples Updated

**Field Tooltip:**
```
Path to SSH private key. Formats: OpenSSH (no extension), PEM (.pem), or Key (.key)
```

**Browse Button Tooltip:**
```
Select the PRIVATE key file (not .pub, .crt, or .cer files)
```

**Example Paths:**
```
Examples: id_ed25519, server.key, zlrelay_key.pem, private_key
```

### 4. Visual Guide Enhancement

Updated the SSH Key Setup Guide box to show all common formats:

**Private Key Formats:**
- `id_ed25519` (OpenSSH)
- `server.key` (Web server)
- `private.pem` (Generic PEM)

**Public Key Formats:**
- `id_ed25519.pub` (OpenSSH)
- `server.pub` (Web server)
- `public.pem` (Generic PEM)

### 5. Documentation Updates

#### SSH_KEY_SETUP_GUIDE.md

**Added Naming Convention Guide:**
```markdown
> **Common Naming Conventions:**
> 
> **OpenSSH Style** (Linux/Unix):
> - Private: `id_ed25519` (no extension)
> - Public: `id_ed25519.pub`
> 
> **Web Server Style** (Apache/Nginx):
> - Private: `server.key` or `domain.key`
> - Certificate: `server.pem` or `domain.pem` or `server.crt`
> 
> **Generic Style**:
> - Private: `private_key.pem` or `mykey.key`
> - Public: `public_key.pem` or `mykey.pub`
>
> All these work with ZL File Relay! Just browse to the **private** key file.
```

**Added Practical Examples:**

Three detailed examples showing how to use existing keys:
1. OpenSSH Keys (Linux/Unix style)
2. Web Server Keys (.key/.pem style)
3. Generic PEM Keys

Each example includes:
- Source file names
- Where to copy them
- Which file to browse to
- How permissions work

## Supported File Extensions

### Private Keys (Select in Browse Dialog)
| Extension | Format | Common Use |
|-----------|--------|------------|
| (none) | OpenSSH | `id_ed25519`, `id_rsa` |
| `.key` | PEM | Web servers, `server.key` |
| `.pem` | PEM | Generic, `private.pem` |
| `_key` | OpenSSH | Custom naming, `zlrelay_key` |

### Public Keys (Auto-detected, don't select)
| Extension | Format | Common Use |
|-----------|--------|------------|
| `.pub` | OpenSSH | `id_ed25519.pub` |
| `.pem` | PEM | Certificates, `server.pem` |
| `.crt` | X.509 | Certificates |
| `.cer` | X.509 | Certificates |

### Warned Against (Not Private Keys)
- `.pub` - Public key
- `.crt` - Certificate
- `.cer` - Certificate

## How Public Key Detection Works

When you select a private key, the system looks for the public key in this order:

1. **Same name + .pub**: `server.key` → `server.key.pub`
2. **Base name + .pub**: `server.key` → `server.pub`
3. **Base name + .pem**: `server.key` → `server.pem`

If the public key isn't found automatically, you can still use "View Public Key" to see it if it exists.

## Use Cases

### Use Case 1: Migrating from Apache/Nginx
```
Current setup:
- /etc/ssl/private/server.key (private key)
- /etc/ssl/certs/server.pem (certificate)

Migration:
1. Copy server.key to C:\ProgramData\ZLFileRelay\ssh\
2. In Config Tool, browse to server.key
3. Done!
```

### Use Case 2: Existing PEM Keys
```
Current setup:
- C:\Keys\mykey.pem (private)
- C:\Keys\mykey.pub (public)

Setup:
1. Browse to C:\Keys\mykey.pem
2. System auto-detects mykey.pub
3. Test connection
```

### Use Case 3: Mixed Environment
```
Multiple key pairs:
- C:\ProgramData\ZLFileRelay\ssh\dmz_server.key (DMZ)
- C:\ProgramData\ZLFileRelay\ssh\ot_server.key (OT)
- C:\ProgramData\ZLFileRelay\ssh\dev.pem (Dev)

All work seamlessly - just browse to the appropriate private key.
```

## Backward Compatibility

### Existing Deployments
✅ No changes needed for existing installations
✅ OpenSSH format (no extension) still works
✅ All previously supported formats still supported
✅ No breaking changes to configuration

### Configuration Files
No changes needed to `appsettings.json`:
```json
{
  "Transfer": {
    "Ssh": {
      "PrivateKeyPath": "C:\\ProgramData\\ZLFileRelay\\ssh\\server.key"
    }
  }
}
```

Works with any of:
- `id_ed25519`
- `server.key`
- `mykey.pem`
- `custom_key`

## Testing

### Test Scenarios Verified
- ✅ Browse for `.key` file
- ✅ Browse for `.pem` file  
- ✅ Browse for file with no extension
- ✅ Warning when selecting `.pub` file
- ✅ Warning when selecting `.crt` file
- ✅ Warning when selecting `.cer` file
- ✅ Public key auto-detection for all formats
- ✅ Connection test with `.key` private key
- ✅ Connection test with `.pem` private key

### Edge Cases Handled
- Multiple extensions: `server.key.old` (works, treats as `.old` extension)
- No extension: `mykey` (works, OpenSSH style)
- Double extensions: `key.pem.bak` (works, treats as `.bak`)

## Benefits

1. **Web Server Migration Support**
   - Easy to migrate existing `.key` files
   - No need to rename files
   - Works with existing certificate infrastructure

2. **Clearer User Guidance**
   - Examples show all common formats
   - Filter shows expected file types first
   - Warnings prevent common mistakes

3. **Professional Appearance**
   - Supports industry-standard naming conventions
   - Matches what users expect from other tools
   - Reduces confusion about file formats

4. **Better Error Prevention**
   - Warns against selecting certificates
   - Validates file extensions
   - Clear feedback on selection

## Implementation Details

### Code Changes
**Files Modified:**
- `src/ZLFileRelay.ConfigTool/ViewModels/FileTransferViewModel.cs`
  - Enhanced browse dialog filter
  - Added certificate file warnings
  - Improved feedback messages

- `src/ZLFileRelay.ConfigTool/Views/FileTransferView.xaml`
  - Updated tooltips to mention `.key` and `.pem`
  - Added format examples
  - Enhanced visual guide

- `docs/configuration/SSH_KEY_SETUP_GUIDE.md`
  - Added naming convention guide
  - Added practical examples
  - Clarified format support

### No Breaking Changes
- ✅ Existing configurations work as-is
- ✅ OpenSSH format still default
- ✅ No API changes
- ✅ No database/config schema changes

## Security Considerations

### File Permissions
Regardless of extension, always:
1. Store private keys in secure location
2. Grant read access only to service account
3. Never share private keys
4. Use NTFS permissions to restrict access

### Format Security
- `.key`, `.pem`, and no-extension all use same PEM encoding
- Extension doesn't affect security
- All formats support strong encryption (ED25519, RSA 4096)

## Future Enhancements

Potential future improvements:
- [ ] Auto-convert between formats (OpenSSH ↔ PEM)
- [ ] Support PKCS#8 format explicitly
- [ ] Support passphrase-protected `.key` files
- [ ] Import from Windows Certificate Store
- [ ] Integration with Azure Key Vault

## Related Documentation

- `docs/configuration/SSH_KEY_SETUP_GUIDE.md` - Complete setup guide
- `docs/SSH_KEY_CLARIFICATION_UPDATE.md` - Private vs public key clarification
- `docs/SSH_ADVANCED_SETTINGS_ENHANCEMENT.md` - Advanced SSH settings

---

**Date:** January 2025  
**Version:** ZL File Relay 1.0  
**Issue:** Support for .key/.pem file naming conventions

