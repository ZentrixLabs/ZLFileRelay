# Upload Fixes - Version 1.1.1

## Issues Fixed

### 1. ❌ 128 MB Upload Limit (CRITICAL)
**Problem**: Files larger than 128 MB were failing with multipart body length limit error, even though config was set to 10 GB.

**Root Cause**: ASP.NET Core's default 128 MB `MultipartBodyLengthLimit` was not being overridden properly. While `FormOptions` were configured globally in `Program.cs`, Razor Pages require explicit attributes at the class level.

**Fix Applied**:
- Added `[RequestSizeLimit(10_737_418_240)]` and `[RequestFormLimits(MultipartBodyLengthLimit = 10_737_418_240)]` attributes to `UploadModel` class
- These attributes MUST be at class level for Razor Pages (not on handler methods)
- Set to 10 GB hardcoded (attributes require compile-time constants)
- Actual limit enforcement still happens via config validation in upload service

**Files Modified**:
- `src/ZLFileRelay.WebPortal/Pages/Upload.cshtml.cs` - Added class-level attributes
- `src/ZLFileRelay.WebPortal/Program.cs` - Added diagnostic logging for configured limits

**Testing**: Upload files up to 10 GB should now work without multipart body length errors.

---

### 2. ⏭️ Status Updates Jumping from Queued → Finished
**Problem**: Transfer status was jumping directly from "Queued" to "Completed/Failed" with no intermediate "Transferring" state shown to users.

**Root Cause**: The Service was only writing ONE status file at the END of the transfer. No status update was sent when the transfer actually started.

**Fix Applied**:
- Added `WriteStartingStatusAsync()` method to send "Transferring" status when transfer begins
- Service now writes TWO status files per transfer:
  1. **Starting status**: Written when transfer begins (special marker `__TRANSFERRING__`)
  2. **Final status**: Written when transfer completes (success or failure)
- WebPortal's `StatusMonitorService` now recognizes the `__TRANSFERRING__` marker and converts it to `TransferStatus.Transferring`

**Files Modified**:
- `src/ZLFileRelay.Service/Services/TransferWorker.cs` - Added `WriteStartingStatusAsync()` and call before transfer
- `src/ZLFileRelay.WebPortal/Services/StatusMonitorService.cs` - Added logic to handle `__TRANSFERRING__` marker

**Status Flow (Now)**:
```
Upload → Queued → Transferring → Completed/Failed
         ^        ^               ^
         |        |               |
    Upload page  Service starts  Service finishes
                 transfer        transfer
```

**Testing**: Users should now see:
1. "Queued" immediately after upload
2. "Transferring" when Service picks up the file and starts transfer
3. "Completed" or "Failed" when transfer finishes

---

## Configuration Cleanup

### Removed Duplicate Config Files
**Problem**: Multiple `appsettings.json` files scattered around causing confusion.

**Architecture Rule**: There should be **ONE master configuration file** at `C:\ProgramData\ZLFileRelay\appsettings.json` that:
- ConfigTool writes to (ONLY component that writes)
- Service reads from
- WebPortal reads from

**Removed**:
- ✅ `appsettings.DMZ.json` - Had outdated/incorrect values (1 GB limit instead of 5 GB)

**Master Config**: `appsettings.json` in repository root → deployed to `C:\ProgramData\ZLFileRelay\appsettings.json`

---

## Deployment Instructions

### To Deploy Version 1.1.1:

**1. Stop Services**
```powershell
Stop-Service ZLFileRelay.WebPortal
Stop-Service ZLFileRelay
```

**2. Deploy Updated Files**
```powershell
# Copy WebPortal (includes upload limit fix)
Copy-Item "publish\WebPortal\*" `
          "C:\FileRelay\WebPortal\" -Recurse -Force

# Copy Service (includes status update fix)
Copy-Item "publish\Service\*" `
          "C:\FileRelay\Service\" -Recurse -Force
```

**3. Verify Configuration**
Ensure `C:\ProgramData\ZLFileRelay\appsettings.json` has:
```json
{
  "ZLFileRelay": {
    "Security": {
      "MaxUploadSizeBytes": 10737418240  // 10 GB
    }
  }
}
```

**4. Start Services**
```powershell
Start-Service ZLFileRelay
Start-Service ZLFileRelay.WebPortal
```

**5. Verify Fixes**

Check WebPortal startup logs for:
```
🔍 Configuring FormOptions with MaxUploadSizeBytes from config: 10737418240 bytes (10.00 GB)
✅ Form options configured for large uploads:
   - Multipart body length limit: 10737418240 bytes (10.00 GB)
```

Upload a file and watch the status progress:
- Should show "Queued" immediately
- Should change to "Transferring" when Service picks it up
- Should change to "Completed" when done

---

## Technical Details

### Why Hardcode 10 GB in Attributes?

C# attributes require **compile-time constants**. You cannot use configuration values in attributes:

```csharp
// ❌ This won't compile - config is runtime value
[RequestSizeLimit(config.Security.MaxUploadSizeBytes)]

// ✅ This works - hardcoded constant
[RequestSizeLimit(10_737_418_240)]
```

The 10 GB limit in attributes is the **framework-level maximum**. The **actual enforced limit** comes from:
1. `appsettings.json` → `Security.MaxUploadSizeBytes` (configurable)
2. Upload service validates against this value
3. Shows user-friendly error if file exceeds configured limit

So you can still configure a lower limit (e.g., 5 GB) without recompiling - users just can't upload larger than 10 GB total.

### Why Special Marker for Transferring Status?

The status notification system uses JSON files written by the Service and read by the WebPortal. The `TransferResult` model doesn't have a "Status" field - only `Success` (bool) and `ErrorMessage` (string).

Rather than change the model (breaking change), we use a special marker:
- `ErrorMessage = "__TRANSFERRING__"` → WebPortal interprets as "Transferring" status
- `ErrorMessage = null` or real error → Normal success/failure handling

This is a pragmatic solution that doesn't break the existing `TransferResult` model used throughout the system.

---

## Version History

- **v1.1.0** - Previous release with upload timeout fixes
- **v1.1.1** - Upload limit fix (128 MB → 10 GB) + Status update improvements

---

## Related Documentation

- [Upload Timeout Fix](UPLOAD_TIMEOUT_FIX.md) - Previous fix for timeout issues
- [Deployment Guide](deployment/DEPLOYMENT_NO_IIS.md) - Full deployment instructions
- [Configuration Reference](../appsettings.json) - Master configuration file


