namespace ZLFileRelay.Core.Models;

/// <summary>
/// Result of a file transfer operation
/// </summary>
public class TransferResult
{
    public bool Success { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string? DestinationPath { get; set; }
    public long FileSize { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public int RetryCount { get; set; }
    public string TransferMethod { get; set; } = string.Empty;
    public bool Verified { get; set; }
    public string? Checksum { get; set; }
}

/// <summary>
/// Result of a file upload operation
/// </summary>
public class UploadResult
{
    public bool Success { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadTime { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public bool RequiresTransfer { get; set; }
    public string? Notes { get; set; }
}
