# Upload Timeout Fix

**Date**: October 31, 2025  
**Issue**: Long file uploads timing out on the web portal  
**Status**: ✅ Fixed

## Problem

Users were experiencing timeouts when uploading large files (multi-GB files) through the web portal. The issue was caused by default Kestrel timeout settings that were too restrictive for long-running file uploads.

## Root Cause

ASP.NET Core's Kestrel web server has several timeout and rate-limiting settings designed to protect against slow/malicious connections:

1. **MinRequestBodyDataRate**: Default 240 bytes/second minimum - causes timeout on slow connections
2. **MinResponseDataRate**: Default 240 bytes/second minimum - restricts server response rate
3. **KeepAliveTimeout**: Default 130 seconds - insufficient for multi-GB uploads on slower networks
4. **RequestHeadersTimeout**: Default 30 seconds - can be too short for large multipart form uploads

For a 5GB file on a 10 Mbps connection:
- Upload time: ~67 minutes
- Default keep-alive: 130 seconds (2.2 minutes) ❌ **Timeout!**

## Solution

Modified `src/ZLFileRelay.WebPortal/Program.cs` to configure Kestrel with settings optimized for large file uploads:

### 1. Kestrel Timeout Settings

```csharp
// Configure timeouts for long file uploads
options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10); // 10 minutes keep-alive
options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(2); // 2 minutes for headers

// Disable minimum data rate requirement for file uploads
// This allows slow connections to upload large files without timing out
options.Limits.MinRequestBodyDataRate = null;
options.Limits.MinResponseDataRate = null;
```

**Benefits:**
- ✅ No minimum data rate requirement - supports slow connections
- ✅ 10-minute keep-alive - handles network interruptions gracefully
- ✅ Increased header timeout - handles large multipart forms

### 2. Form Options Configuration

```csharp
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    // Set generous limits for large file uploads
    options.MultipartBodyLengthLimit = initialConfig.Security.MaxUploadSizeBytes;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
    
    // Increase buffer threshold for better performance
    options.MemoryBufferThreshold = 1024 * 1024 * 10; // 10 MB buffer
});
```

**Benefits:**
- ✅ Aligns multipart form limits with configured max file size
- ✅ 10 MB memory buffer before spilling to disk - better performance
- ✅ Removes limits on form field sizes

## Configuration

The maximum upload size is controlled by the configuration file:

**Location**: `C:\ProgramData\ZLFileRelay\appsettings.json`

```json
{
  "ZLFileRelay": {
    "Security": {
      "MaxUploadSizeBytes": 5368709120  // 5 GB default
    }
  }
}
```

This can be adjusted using the **ZL File Relay Configuration Tool**:
1. Open ConfigTool
2. Navigate to **Security** tab
3. Adjust **Maximum Upload Size**
4. Save configuration
5. Restart web portal service

## Upload Time Estimates

With the new configuration, here are realistic upload times for various file sizes and connection speeds:

| File Size | 1 Mbps | 10 Mbps | 100 Mbps | 1 Gbps |
|-----------|--------|---------|----------|--------|
| 100 MB    | 13 min | 1.3 min | 8 sec    | 0.8 sec |
| 1 GB      | 2.2 hrs| 13 min  | 1.3 min  | 8 sec |
| 5 GB      | 11 hrs | 67 min  | 6.7 min  | 40 sec |

**Note**: The 10-minute keep-alive timeout means:
- Files must maintain some data transfer within each 10-minute window
- Total upload time is unlimited as long as data continues flowing
- Completely stalled uploads will timeout after 10 minutes

## Testing

To verify the fix is working:

1. **Check logs** during web portal startup:
```
[INF] ✅ Kestrel configured for large file uploads:
[INF]    - Max request body: 5368709120 bytes (5.00 GB)
[INF]    - Keep-alive timeout: 10 minutes
[INF]    - Request headers timeout: 2 minutes
[INF]    - Data rate limits: disabled (allows slow connections)
[INF] ✅ Form options configured for large uploads:
[INF]    - Multipart body length limit: 5368709120 bytes
[INF]    - Memory buffer threshold: 10 MB
```

2. **Test upload** with a large file (1GB+)
3. **Monitor progress** - should show steady upload without disconnections

## Additional Considerations

### Network Interruptions

The web portal's file upload is a single HTTP POST request. If the network disconnects during upload:
- The upload will fail and need to be restarted from the beginning
- Browsers do NOT automatically resume uploads
- Consider using the **Transfer** destination for critical files (enables retry logic)

### Reverse Proxies

If using a reverse proxy (IIS, nginx, Apache) in front of Kestrel:
- Configure **proxy timeout** settings to match or exceed Kestrel's 10-minute keep-alive
- Configure **proxy buffer** settings for large request bodies

#### IIS Example (web.config):
```xml
<system.webServer>
  <aspNetCore requestTimeout="00:30:00" />  <!-- 30 minutes -->
</system.webServer>
```

#### nginx Example:
```nginx
proxy_read_timeout 600s;  # 10 minutes
proxy_send_timeout 600s;  # 10 minutes
client_max_body_size 5G;  # Match max file size
```

### Browser Timeouts

Modern browsers typically don't have upload timeouts, but some corporate environments may:
- Check browser console for errors
- Verify no browser extensions are interfering
- Test with a different browser

## Related Files

- `src/ZLFileRelay.WebPortal/Program.cs` - Kestrel and form configuration
- `src/ZLFileRelay.WebPortal/Services/FileUploadService.cs` - Upload handling logic
- `src/ZLFileRelay.WebPortal/Pages/Upload.cshtml` - Upload UI with progress indicator
- `appsettings.json` - Configuration file with MaxUploadSizeBytes setting

## Monitoring

Monitor upload success/failure in logs:

**Location**: `C:\FileRelay\logs\zlfilerelay-web-YYYYMMDD.log`

**Successful upload**:
```
[INF] Processing file: largefile.zip (5368709120 bytes) for user: john.doe
[INF] Creating user directory: C:\FileRelay\uploads\transfer\john_doe
[INF] File saved successfully: C:\FileRelay\uploads\transfer\john_doe\largefile.zip
```

**Failed upload** (timeout or other error):
```
[ERR] Error uploading file: largefile.zip for user: john.doe
      Exception: The request was aborted
```

## Future Enhancements

Potential improvements for even better upload experience:

1. **Chunked Upload** - Split large files into chunks for resumable uploads
2. **Upload Progress API** - Real-time progress tracking via SignalR
3. **Upload Queue** - Background processing for very large files
4. **Direct-to-Transfer** - Skip DMZ storage, upload directly to watch directory

## Support

If upload timeouts persist after this fix:

1. Check network bandwidth and stability
2. Review reverse proxy configuration (if applicable)
3. Examine web portal logs for specific error messages
4. Verify disk I/O performance on server
5. Contact system administrator for network diagnostics

---

**Change Log**:
- 2025-10-31: Initial fix implemented - added Kestrel timeout configuration and form options

