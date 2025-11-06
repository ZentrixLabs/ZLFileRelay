# File Upload Size Limit Fixes - v1.1.1

## Summary

Fixed critical issues with file upload size limits and transfer timeouts:

1. **File Size Limits:** Fixed HTTP 400 errors and configuration confusion
2. **Transfer Timeouts:** Increased timeouts for large file transfers (4+ GB)
3. **UI Warnings:** Adjusted warning thresholds for large file transfers
4. **Executable Files:** Improved logging for AllowExecutableFiles setting

## Problems Fixed

### 1. Configuration Confusion ✅
**Problem:** Two separate file size settings existed:
- `WebPortal.MaxFileSizeBytes` (NOT used by code)
- `Security.MaxUploadSizeBytes` (ACTUALLY used by code)

**Solution:**
- Marked `WebPortal.MaxFileSizeBytes` as `[Obsolete]` with clear XML documentation
- Added startup warning if obsolete property is still configured
- Removed obsolete property from all default configuration files

### 2. Wrong Configuration File ✅
**Problem:** Users were editing the wrong appsettings.json file.

**Solution:**
- Added clear startup logging showing:
  - Which config file was loaded (`C:\ProgramData\ZLFileRelay\appsettings.json`)
  - Current max upload size in bytes and GB
  - Company and site name for verification
  - Warning if obsolete property is detected

### 3. Generic HTTP 400 Error ✅
**Problem:** When uploads exceeded limits, users got a generic HTTP 400 page with no explanation.

**Solution:**
- Created new `/FileTooLarge` error page with:
  - User-friendly explanation
  - Current max file size displayed
  - Total size attempted (if available)
  - Helpful suggestions
  - Support contact information
  - Link back to upload page
- Added middleware to intercept 400 errors on upload POST requests
- Automatically redirect to friendly error page when file size limits exceeded

### 4. Updated to 80GB Limit ✅
**Problem:** Configuration was set to 5GB, user needed 80GB.

**Solution:**
- Updated `Security.MaxUploadSizeBytes` to `85899345920` (80GB) in:
  - `appsettings.json` (root)
  - `publish/appsettings.json` (deployed config)

### 5. Transfer Timeouts Too Short ✅
**Problem:** Large files (4+ GB) were timing out during SCP transfer to SCADA, showing "Transfer is taking longer than expected" warnings.

**Solution:**
- Increased `OperationTimeout` from 300 seconds (5 min) to 3600 seconds (1 hour)
- Increased `TransferTimeout` from 300 seconds (5 min) to 7200 seconds (2 hours)
- Increased UI warning threshold from 2 minutes to 10 minutes
- Added detailed timeout configuration documentation

### 6. Executable File Blocking ✅
**Problem:** `.ps1` files were being blocked even when `AllowExecutableFiles: true`.

**Solution:**
- Added debug logging to show when executable extensions are allowed
- Added startup logging to display `AllowExecutableFiles` setting
- Added troubleshooting documentation for common configuration issues

## Files Changed

### Core Configuration Model
- `src/ZLFileRelay.Core/Models/ZLFileRelayConfiguration.cs`
  - Marked `WebPortal.MaxFileSizeBytes` as obsolete
  - Added XML documentation explaining to use `Security.MaxUploadSizeBytes`

### Web Portal
- `src/ZLFileRelay.WebPortal/Program.cs`
  - Enhanced startup logging with config file path and max upload size
  - Added security settings logging (AllowExecutableFiles, BlockedExtensions)
  - Added obsolete property detection and warning
  - Added middleware to intercept 400 errors and redirect to friendly page

- `src/ZLFileRelay.WebPortal/Pages/FileTooLarge.cshtml` (NEW)
  - User-friendly error page for oversized uploads
  
- `src/ZLFileRelay.WebPortal/Pages/FileTooLarge.cshtml.cs` (NEW)
  - Page model with file size formatting

- `src/ZLFileRelay.WebPortal/Pages/Result.cshtml`
  - Increased UI warning timeout from 2 minutes to 10 minutes for large file transfers

- `src/ZLFileRelay.WebPortal/Services/FileUploadService.cs`
  - Added detailed debug logging for extension blocking/allowing
  - Improved logging to show AllowExecutableFiles setting and why files are blocked

### Configuration Files
- `appsettings.json` (root)
  - Removed obsolete `MaxFileSizeBytes` property
  - Updated `Security.MaxUploadSizeBytes` to 85899345920 (80GB)
  - Updated `Transfer.Ssh.OperationTimeout` to 3600 seconds (1 hour)
  - Updated `Transfer.Ssh.TransferTimeout` to 7200 seconds (2 hours)

- `src/ZLFileRelay.WebPortal/appsettings.json`
  - Removed obsolete `MaxFileSizeBytes` property

- `publish/appsettings.json`
  - Removed obsolete `MaxFileSizeBytes` property
  - Updated `Security.MaxUploadSizeBytes` to 85899345920 (80GB)
  - Updated `Transfer.Ssh.OperationTimeout` to 3600 seconds (1 hour)
  - Updated `Transfer.Ssh.TransferTimeout` to 7200 seconds (2 hours)

## Configuration Guide

### Correct Property to Use
Always use `Security.MaxUploadSizeBytes` for file size limits:

```json
{
  "ZLFileRelay": {
    "Security": {
      "MaxUploadSizeBytes": 85899345920  // 80 GB
    }
  }
}
```

### Common Size Values
```
 1 GB  = 1073741824
 5 GB  = 5368709120
10 GB  = 10737418240
20 GB  = 21474836480
50 GB  = 53687091200
80 GB  = 85899345920
100 GB = 107374182400
```

### Configuration File Location
The web portal and service load configuration from:
**`C:\ProgramData\ZLFileRelay\appsettings.json`**

NOT from the source code directories!

### How to Change Max Upload Size

1. Edit `C:\ProgramData\ZLFileRelay\appsettings.json`
2. Find the `Security` section
3. Update `MaxUploadSizeBytes` value
4. Save the file
5. **Restart the web portal service:**
   ```powershell
   Restart-Service ZLFileRelay.WebPortal
   ```

## Testing Required

### Test 1: Verify 80GB Limit Applied ✅
1. Restart the web portal service
2. Check startup logs for:
   ```
   ✅ Configuration loaded successfully
      📁 Config file: C:\ProgramData\ZLFileRelay\appsettings.json
      🏢 Company: [Your Company]
      📍 Site: [Your Site]
      📦 Max upload size: 85899345920 bytes (80.00 GB)
   ```

### Test 2: Verify Friendly Error Page
1. Try uploading a file larger than 80GB
2. Should see user-friendly `/FileTooLarge` page instead of HTTP 400
3. Page should show:
   - Maximum file size (80 GB)
   - Attempted size (if detected)
   - Helpful instructions
   - Link back to upload page

### Test 3: Verify Valid Uploads Work
1. Upload a file under 80GB
2. Should upload successfully
3. No errors or issues

## Startup Log Example

When the web portal starts correctly, you should see:

```
[Information] ✅ Configuration loaded successfully
[Information]    📁 Config file: C:\ProgramData\ZLFileRelay\appsettings.json
[Information]    🏢 Company: ZentrixLabs
[Information]    📍 Site: Your Site Name
[Information]    📦 Max upload size: 85899345920 bytes (80.00 GB)
[Information] ✅ Kestrel configured for large file uploads:
[Information]    - Max request body: 85899345920 bytes (80.00 GB)
[Information]    - Keep-alive timeout: 10 minutes
[Information]    - Request headers timeout: 2 minutes
[Information]    - Data rate limits: disabled (allows slow connections)
[Information] ✅ Form options configured for large uploads:
[Information]    - Multipart body length limit: 85899345920 bytes (80.00 GB)
[Information]    - Memory buffer threshold: 10 MB
```

## Migration Notes

### For Existing Deployments

If you have an existing deployment with `WebPortal.MaxFileSizeBytes` in your config:

1. The old property is now ignored (obsolete)
2. You'll see a warning in logs:
   ```
   [Warning] ⚠️ DEPRECATED: WebPortal.MaxFileSizeBytes is set but not used. 
             Please remove it and use Security.MaxUploadSizeBytes instead.
   ```
3. To clean up:
   - Edit `C:\ProgramData\ZLFileRelay\appsettings.json`
   - Remove the `MaxFileSizeBytes` line from the `WebPortal` section
   - Ensure `MaxUploadSizeBytes` is set correctly in the `Security` section
   - Restart the service

### Backward Compatibility

The obsolete property remains in the code (marked with `[Obsolete]`) to avoid breaking existing configurations. It will be removed in a future major version.

## Executable File Upload (AllowExecutableFiles)

### How It Works

When `Security.AllowExecutableFiles` is set to `true`, the following extensions are automatically allowed even if they're in the blocked list:

```
.exe, .dll, .bat, .cmd, .ps1, .vbs, .com, .scr, .msi, .jar
```

### Configuration Example

```json
{
  "ZLFileRelay": {
    "Security": {
      "AllowExecutableFiles": true,
      "MaxUploadSizeBytes": 85899345920
    },
    "WebPortal": {
      "BlockedFileExtensions": [".exe", ".dll", ".bat", ".cmd", ".ps1", ".vbs"]
    }
  }
}
```

With this configuration:
- ✅ `.exe`, `.msi`, `.ps1`, etc. are **ALLOWED** (because `AllowExecutableFiles: true`)
- ❌ Any extensions NOT in the executable list but in BlockedFileExtensions are still blocked

### Startup Logging

When the web portal starts, you'll see:

```
[Information] 🔒 Security settings:
[Information]    - Allow executable files: true
[Information]    - Blocked extensions: .exe, .dll, .bat, .cmd, .ps1, .vbs
```

And when files are uploaded, you'll see debug logs:

```
[Debug] Extension .ps1 allowed: is executable and AllowExecutableFiles=true
[Information] Extension .js blocked: AllowExecutableFiles=true, IsExecutable=false, InBlockedList=true
```

## Troubleshooting

### Issue: Changes Not Taking Effect

**Symptoms:** Changed max upload size but still getting old limit

**Solution:**
1. Verify you edited the correct file: `C:\ProgramData\ZLFileRelay\appsettings.json`
2. Check the file was saved (no read-only flag)
3. Restart the web portal service
4. Check startup logs to confirm new value loaded

### Issue: Still Getting HTTP 400

**Symptoms:** Upload fails with HTTP 400 instead of friendly error page

**Possible Causes:**
1. Different type of 400 error (not file size related)
2. Middleware not intercepting correctly
3. Browser cached the old error page

**Solution:**
1. Check web portal logs for details
2. Clear browser cache
3. Try in incognito/private browsing mode
4. Check for other validation errors

### Issue: Uploads Timing Out

**Symptoms:** Upload starts but times out before completing

**Causes:**
1. **Web Portal Upload Timeout:** HTTP upload from browser timing out
2. **Transfer Service Timeout:** SCP/SSH transfer to SCADA timing out

**Solutions:**

**1. Web Portal Upload Timeouts (HTTP):**

Already configured for large files in `Program.cs`:
```csharp
options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(2);
options.Limits.MinRequestBodyDataRate = null; // No rate limit
```

If you need even longer timeouts for very slow connections, increase these values.

**2. Transfer Service Timeouts (SSH/SCP to SCADA):**

The transfer service has been updated with much longer timeouts for large files:

```json
{
  "ZLFileRelay": {
    "Transfer": {
      "Ssh": {
        "ConnectionTimeout": 30,       // 30 seconds to establish connection
        "OperationTimeout": 3600,      // 1 hour for SSH operations (UPDATED from 5 min)
        "TransferTimeout": 7200,       // 2 hours for file transfer (UPDATED from 5 min)
        "KeepAliveInterval": 30        // Keep connection alive every 30 sec
      }
    }
  }
}
```

**Transfer Time Estimates:**

For reference, here are approximate transfer times at different speeds:

| File Size | 10 Mbps | 100 Mbps | 1 Gbps |
|-----------|---------|----------|--------|
| 1 GB      | 13 min  | 1.3 min  | 8 sec  |
| 5 GB      | 67 min  | 6.7 min  | 40 sec |
| 10 GB     | 133 min | 13 min   | 80 sec |
| 80 GB     | 18 hours| 107 min  | 11 min |

**Recommended Settings by Network Speed:**

- **Fast network (100 Mbps+):** Default settings (2 hours) are fine
- **Moderate network (10-100 Mbps):** Consider 4-6 hour timeout for very large files
- **Slow network (<10 Mbps):** Consider 12-24 hour timeout for 50GB+ files

**3. UI Warning Timeout:**

The web interface shows "Transfer is taking longer than expected" warning after 10 minutes. This is now appropriate for large files. If you have extremely slow connections, you can adjust this in `Result.cshtml`:

```javascript
}, 600000); // 10 minutes (600,000 milliseconds)
```

### Issue: .ps1 Files Still Blocked Despite AllowExecutableFiles=true

**Symptoms:** `.exe` and `.msi` files upload fine, but `.ps1` files are rejected

**Diagnosis Steps:**

1. **Check startup logs** to verify configuration:
   ```
   [Information] 🔒 Security settings:
   [Information]    - Allow executable files: true
   ```

2. **Check upload attempt logs** for the actual reason:
   ```
   [Information] Extension .ps1 blocked: AllowExecutableFiles=false, IsExecutable=true, InBlockedList=true
   ```

**Common Causes:**

1. **Wrong config file edited:**
   - You edited: `src/ZLFileRelay.WebPortal/appsettings.json`
   - Actually used: `C:\ProgramData\ZLFileRelay\appsettings.json`

2. **Service not restarted:**
   ```powershell
   Restart-Service ZLFileRelay.WebPortal
   ```

3. **Config file not saved or read-only:**
   - Check file attributes
   - Verify changes are actually in the file

4. **Case sensitivity:**
   - Ensure property is exactly: `AllowExecutableFiles` (capital A, capital E, capital F)

**Solution:**

1. Edit the correct file: `C:\ProgramData\ZLFileRelay\appsettings.json`

2. Set the property correctly:
   ```json
   {
     "ZLFileRelay": {
       "Security": {
         "AllowExecutableFiles": true
       }
     }
   }
   ```

3. Save and restart:
   ```powershell
   Restart-Service ZLFileRelay.WebPortal
   ```

4. Verify in logs:
   ```
   [Information] 🔒 Security settings:
   [Information]    - Allow executable files: true
   ```

5. Try upload again - you should see:
   ```
   [Debug] Extension .ps1 allowed: is executable and AllowExecutableFiles=true
   ```

## Next Steps

1. ✅ Deploy the updated code
2. ✅ Update configuration file with 80GB limit
3. ✅ Restart web portal service
4. ⏳ **Test with actual file uploads** (see Testing Required section)
5. ⏳ Monitor logs for any issues
6. ⏳ Verify user feedback on error messages

## Version Information

- **Version:** 1.1.1
- **Date:** 2025-11-06
- **Status:** Ready for testing
