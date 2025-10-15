namespace ZLFileRelay.Core.Models;

/// <summary>
/// Status of a file transfer operation
/// </summary>
public enum TransferStatus
{
    /// <summary>
    /// File is queued and waiting for transfer
    /// </summary>
    Queued,
    
    /// <summary>
    /// File is currently being transferred
    /// </summary>
    Transferring,
    
    /// <summary>
    /// Transfer completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Transfer failed
    /// </summary>
    Failed
}

/// <summary>
/// Tracks the status of an individual file transfer
/// </summary>
public class FileTransferStatus
{
    /// <summary>
    /// Unique identifier for this transfer (typically file path hash or guid)
    /// </summary>
    public string TransferId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the file being transferred
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Full path to the source file
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Destination path (if known)
    /// </summary>
    public string? DestinationPath { get; set; }
    
    /// <summary>
    /// Current status of the transfer
    /// </summary>
    public TransferStatus Status { get; set; }
    
    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// User who uploaded the file
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// When the file was queued for transfer
    /// </summary>
    public DateTime QueuedTime { get; set; }
    
    /// <summary>
    /// When the transfer started (if applicable)
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// When the transfer completed or failed
    /// </summary>
    public DateTime? CompletedTime { get; set; }
    
    /// <summary>
    /// Error message if transfer failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Transfer method (SSH/SCP, SMB, etc.)
    /// </summary>
    public string? TransferMethod { get; set; }
    
    /// <summary>
    /// Duration of the transfer (if completed)
    /// </summary>
    public TimeSpan? Duration => CompletedTime.HasValue && StartTime.HasValue 
        ? CompletedTime.Value - StartTime.Value 
        : null;
}

