# Upload UX Simplification

## Change Summary
**Date**: October 15, 2025

### Problem
The upload interface had confusing logic:
- Hidden dropdown for "destinations" (DMZ vs Transfer directory)
- "Queue for Automatic Transfer" checkbox that didn't actually control the destination
- Logic was backwards: EnableUploadToTransfer config determined ALL file destinations
- Users couldn't easily understand: "Where do my files go?"

### Solution
**Simplified to single, clear choice:**

☐ **Send to SCADA**
- **Unchecked** → Files stay in DMZ upload location
- **Checked** → Files automatically transferred to SCADA system

### Changes Made

#### 1. **Fixed Destination Logic** (`FileUploadService.cs`)
**Before:**
```csharp
var dest = destination ?? (_config.WebPortal.EnableUploadToTransfer 
    ? _config.Service.WatchDirectory 
    : _config.Paths.UploadDirectory);
```
*Problem: Config setting controlled destination, not user checkbox*

**After:**
```csharp
var dest = destination ?? (requiresTransfer
    ? _config.Service.WatchDirectory 
    : _config.Paths.UploadDirectory);
```
*Solution: User's checkbox now controls destination*

#### 2. **Cleaned Up ViewModel** (`FileUploadViewModel.cs`)
**Removed:**
- `SelectedDestination` property
- `AvailableDestinations` dictionary

**Updated:**
- Display name changed from "Queue for Automatic Transfer" to "Send to SCADA"

#### 3. **Updated Upload UI** (`Upload.cshtml`)
**Before:**
```
☐ Queue for Automatic Transfer
When enabled, files will be automatically transferred by the background service.
```

**After:**
```
☐ Send to SCADA
ℹ️ Unchecked: Files stay in DMZ upload location
➡️ Checked: Files automatically transferred to SCADA system
```

#### 4. **Updated Result UI** (`Result.cshtml`)
**Added clarity for non-SCADA uploads:**
```
✓ Saved to DMZ location (no SCADA transfer)
```

**Updated transfer messages:**
- "Queued for SCADA transfer..."
- "Transferring to SCADA..."
- "✓ SCADA transfer completed: {path}"
- "SCADA transfer failed: {error}"

#### 5. **Cleaned Up Code-Behind** (`Upload.cshtml.cs`)
Removed calls to `GetUploadDestinations()` - no longer needed

### User Flow

#### Scenario 1: DMZ Storage Only
1. User uploads file
2. **Unchecks** "Send to SCADA"
3. File saved to: `C:\FileRelay\uploads\{username}\filename.ext`
4. Result page shows: "✓ Saved to DMZ location (no SCADA transfer)"
5. File stays on DMZ server permanently

#### Scenario 2: SCADA Transfer
1. User uploads file
2. **Checks** "Send to SCADA" (default)
3. File saved to: `C:\FileRelay\uploads\transfer\{username}\filename.ext`
4. Result page shows: "⏱ Queued for SCADA transfer..."
5. Service picks up file and transfers to SCADA
6. **Real-time update**: "✓ SCADA transfer completed: root@10.10.4.210:/scada/..."

### Configuration

No configuration changes required! The system uses:
- **DMZ Location**: `ZLFileRelay:Paths:UploadDirectory` (default: `C:\FileRelay\uploads`)
- **Transfer Location**: `ZLFileRelay:Service:WatchDirectory` (default: `C:\FileRelay\uploads\transfer`)
- **Enable Option**: `ZLFileRelay:WebPortal:EnableUploadToTransfer` (true/false)

### Benefits

✅ **Crystal clear UX** - Users immediately understand the choice
✅ **One decision point** - Not two separate, confusing options
✅ **Correct logic** - User's choice actually controls what happens
✅ **Better messaging** - "SCADA" makes it obvious where files go
✅ **Shows both paths** - Users see outcome whether they check or uncheck
✅ **Real-time feedback** - With SignalR, users see SCADA transfer complete

### Files Modified
- `src/ZLFileRelay.WebPortal/Services/FileUploadService.cs` - Fixed destination logic
- `src/ZLFileRelay.WebPortal/ViewModels/FileUploadViewModel.cs` - Removed dropdown props
- `src/ZLFileRelay.WebPortal/Pages/Upload.cshtml` - Updated UI text
- `src/ZLFileRelay.WebPortal/Pages/Upload.cshtml.cs` - Removed destination population
- `src/ZLFileRelay.WebPortal/Pages/Result.cshtml` - Updated status messages

### Testing

1. **Stop services** (if running)
2. **Build**: `dotnet build`
3. **Test DMZ-only upload:**
   - Navigate to Upload page
   - Uncheck "Send to SCADA"
   - Upload file
   - Verify: File in `C:\FileRelay\uploads\{user}\` and stays there
4. **Test SCADA transfer:**
   - Navigate to Upload page
   - Check "Send to SCADA" (default)
   - Upload file
   - Verify: File in `C:\FileRelay\uploads\transfer\{user}\`
   - Verify: Status updates in real-time when transfer completes

---

**Result**: Clean, intuitive UX that accurately reflects what actually happens! 🎯

