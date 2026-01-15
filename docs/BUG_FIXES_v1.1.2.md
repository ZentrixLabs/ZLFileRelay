# Bug Fixes - v1.1.2

## Summary

Fixed three critical bugs affecting file uploads and transfers:

1. **Hardcoded DMZ Upload Path** - Files not sent to SCADA were going to generic path instead of configurable DMZ location
2. **Missing Archive Functionality** - Files were deleted after transfer instead of being archived as configured
3. **.status Folder Being Transferred** - Status tracking files were being copied to SCADA along with actual data files

## Bug Details

### Bug 1: Hardcoded DMZ Upload Path ❌

**Problem:**  
When users uploaded files with "Send to SCADA" unchecked, files were saved to the generic `Paths.UploadDirectory` (e.g., `C:\FileRelay\uploads`) instead of a configurable DMZ-specific directory.

**Impact:**
- No separation between DMZ-only files and transfer files
- Hardcoded path made multi-environment deployment difficult
- Files not organized by purpose

**Root Cause:**  
`FileUploadService.cs` line 279 used ternary operator that only checked `requiresTransfer` flag:
```csharp
var dest = destination ?? (requiresTransfer
    ? Config.Service.WatchDirectory    // SCADA transfer folder
    : Config.Paths.UploadDirectory);   // Generic upload - NOT DMZ specific!
```

**Fix:**
- Added new `DmzUploadDirectory` property to `WebPortalSettings`
- Updated upload logic to use DMZ directory for non-transfer files
- Added directory creation at startup
- Falls back to `Paths.UploadDirectory` if not configured

**Configuration:**
```json
{
  "ZLFileRelay": {
    "WebPortal": {
      "DmzUploadDirectory": "C:\\FileRelay\\uploads\\dmz"
    }
  }
}
```

---

### Bug 2: Missing Archive Functionality ❌

**Problem:**  
The configuration had `ArchiveAfterTransfer` and `ArchiveDirectory` settings, but files were only **deleted** after successful transfer - never archived!

**Impact:**
- Lost audit trail of transferred files
- No way to recover accidentally deleted files
- Compliance issues (no retention of transferred data)

**Root Cause:**  
Both `ScpFileTransferService.cs` and `SmbFileTransferService.cs` only implemented delete logic:
```csharp
if (_config.Service.DeleteAfterTransfer && result.Verified)
{
    File.Delete(sourceFile);  // ⚠️ NO ARCHIVE LOGIC!
}
```

**Fix:**
- Implemented archive-before-delete logic in both transfer services
- Preserves folder structure in archive directory
- Handles duplicate files with timestamp suffix
- Only falls back to delete if `ArchiveAfterTransfer` is false

**Archive Logic Flow:**
```
1. Transfer file to SCADA
2. Verify transfer succeeded
3. If ArchiveAfterTransfer=true:
   a. Calculate relative path from watch directory
   b. Create archive path preserving structure
   c. Create archive subdirectories if needed
   d. Handle duplicates with timestamp
   e. Move file to archive
4. Else if DeleteAfterTransfer=true:
   a. Delete file
5. Else:
   a. Leave file in place
```

**Example:**
```
Source:  E:\uploads\transfer\site1\data.csv
Archive: C:\FileRelay\archive\site1\data.csv

If duplicate exists:
Archive: C:\FileRelay\archive\site1\data_20260115_143022.csv
```

---

### Bug 3: .status Folder Being Transferred ❌

**Problem:**  
The `.status` folder (used for real-time transfer status tracking) was being watched by the file watcher, causing status JSON files to be queued for transfer to SCADA!

**Impact:**
- Status tracking files polluting SCADA transfer directory
- Unnecessary network traffic
- Confusing SCADA operators with .status.json files

**Root Cause:**  
`FileWatcher` monitors all subdirectories including `.status`:
```csharp
_watcher = new FileSystemWatcher(path)
{
    IncludeSubdirectories = includeSubdirectories,  // Watches EVERYTHING!
    EnableRaisingEvents = true
};
```

And `TransferWorker.OnFileDetected` didn't filter out `.status` folder:
```csharp
private void OnFileDetected(object sender, FileSystemEventArgs e)
{
    if (File.Exists(e.FullPath))
    {
        FileDetected?.Invoke(this, e);  // .status files got queued!
    }
}
```

**Fix:**
Added exclusion check in `TransferWorker.OnFileDetected`:
```csharp
// EXCLUDE .status folder files from being transferred to SCADA
var statusDirectory = Path.Combine(_config.Service.WatchDirectory, ".status");
if (e.FullPath.StartsWith(statusDirectory, StringComparison.OrdinalIgnoreCase))
{
    _logger.LogDebug("Ignoring status file: {FilePath}", e.FullPath);
    return;
}
```

**Status Directory Structure:**
```
E:\uploads\transfer\
├── site1\
│   └── data.csv              ← TRANSFERRED
├── site2\
│   └── report.xlsx           ← TRANSFERRED
└── .status\
    ├── abc123.status.json    ← EXCLUDED
    └── def456.status.json    ← EXCLUDED
```

---

## Files Changed

### Core Configuration
- **`src/ZLFileRelay.Core/Models/ZLFileRelayConfiguration.cs`**
  - Added `DmzUploadDirectory` property to `WebPortalSettings`

### Web Portal
- **`src/ZLFileRelay.WebPortal/Services/FileUploadService.cs`**
  - Updated `UploadFormFileAsync` to use configurable DMZ directory
  - Added debug logging for destination selection

- **`src/ZLFileRelay.WebPortal/Program.cs`**
  - Added DMZ upload directory creation at startup
  - Logs when DMZ directory is created

### Transfer Service
- **`src/ZLFileRelay.Service/Services/ScpFileTransferService.cs`**
  - Implemented archive functionality before delete
  - Preserves folder structure in archive
  - Handles duplicate files with timestamps

- **`src/ZLFileRelay.Service/Services/SmbFileTransferService.cs`**
  - Implemented archive functionality before delete
  - Same logic as SCP service for consistency

- **`src/ZLFileRelay.Service/Services/TransferWorker.cs`**
  - Added `.status` folder exclusion in `OnFileDetected`
  - Prevents status files from being transferred

### Configuration Files
- **`appsettings.json`** (root)
  - Added `DmzUploadDirectory` setting

- **`publish/appsettings.json`**
  - Added `DmzUploadDirectory` setting

- **`src/ZLFileRelay.WebPortal/appsettings.json`**
  - Added `DmzUploadDirectory` setting

---

## Configuration Guide

### DMZ Upload Directory

Set where files should be saved when NOT transferring to SCADA:

```json
{
  "ZLFileRelay": {
    "WebPortal": {
      "DmzUploadDirectory": "E:\\uploads\\dmz"
    }
  }
}
```

**Behavior:**
- Files with "Send to SCADA" **unchecked** → `DmzUploadDirectory`
- Files with "Send to SCADA" **checked** → `Service.WatchDirectory`

**Default:**  
If not specified, falls back to `Paths.UploadDirectory`

### Archive Configuration

Control what happens to files after successful transfer:

```json
{
  "ZLFileRelay": {
    "Service": {
      "ArchiveAfterTransfer": true,
      "ArchiveDirectory": "C:\\FileRelay\\archive",
      "DeleteAfterTransfer": false
    }
  }
}
```

**Priority:**
1. If `ArchiveAfterTransfer=true` → Move to archive
2. Else if `DeleteAfterTransfer=true` → Delete file
3. Else → Leave file in place

**Best Practice:**  
Set `ArchiveAfterTransfer=true` for audit trails and compliance.

---

## Testing Instructions

### Test 1: DMZ Upload Path

1. Set `DmzUploadDirectory` to `E:\uploads\dmz` in config
2. Restart web portal service
3. Upload file with "Send to SCADA" **unchecked**
4. ✅ Verify file is in `E:\uploads\dmz\{username}\{filename}`
5. Upload file with "Send to SCADA" **checked**
6. ✅ Verify file is in watch directory (e.g., `E:\uploads\transfer\{username}\{filename}`)

### Test 2: Archive Functionality

1. Set `ArchiveAfterTransfer=true` and `DeleteAfterTransfer=false`
2. Restart transfer service
3. Upload file with "Send to SCADA" checked
4. Wait for transfer to complete
5. ✅ Verify file moved to `C:\FileRelay\archive\{username}\{filename}`
6. ✅ Verify file is NOT in watch directory
7. Upload same file again (duplicate test)
8. ✅ Verify second copy archived with timestamp suffix

### Test 3: .status Folder Exclusion

1. Upload file with "Send to SCADA" checked
2. Check `.status` folder in watch directory for status files
3. Wait for transfer to complete
4. ✅ Check SCADA destination - NO .status.json files should exist
5. ✅ Check service logs for "Ignoring status file" messages

---

## Migration Notes

### For Existing Deployments

**DMZ Upload Directory:**
- **Action Required:** Add `DmzUploadDirectory` to your config
- **Default Behavior:** If not set, uses `Paths.UploadDirectory` (current behavior)
- **Recommended:** Set explicit DMZ path for better organization

**Archive Functionality:**
- **No Action Required:** Archive logic respects existing settings
- **Current Behavior:** If `ArchiveAfterTransfer=true`, now actually archives
- **Backward Compatible:** If you have `DeleteAfterTransfer=true` only, behavior unchanged

**.status Exclusion:**
- **No Action Required:** Automatically excludes .status files
- **Benefit:** Cleaner SCADA transfers
- **No Breaking Changes:** Doesn't affect existing file processing

---

## Logging

### New Log Messages

**DMZ Upload Directory:**
```
[Information] ✅ DMZ upload directory created: E:\uploads\dmz
[Debug] Upload destination for DMZ file: E:\uploads\dmz
```

**Archive Operations:**
```
[Information] Archived file after successful transfer: data.csv → C:\FileRelay\archive\site1\data.csv
[Warning] Failed to archive file: data.csv (if error occurs)
```

**.status Exclusion:**
```
[Debug] Ignoring status file: E:\uploads\transfer\.status\abc123.status.json
```

---

## Version Information

- **Version:** 1.1.2
- **Date:** 2026-01-15
- **Status:** Ready for deployment
- **Breaking Changes:** None
- **Configuration Changes:** 1 new optional property (`DmzUploadDirectory`)

---

## Related Documentation

- [Upload Size Fixes v1.1.1](UPLOAD_FIXES_v1.1.1.md) - Previous fixes for file size limits and timeouts
- [Configuration Guide](README.md) - Full configuration reference
- [Deployment Guide](SETUP.md) - How to deploy updates
