using ZLFileRelay.Core.Models;

namespace ZLFileRelay.Core.Interfaces;

/// <summary>
/// Interface for file transfer services (SSH/SCP, SMB, etc.)
/// </summary>
public interface IFileTransferService
{
    /// <summary>
    /// Transfer a single file to the destination
    /// </summary>
    Task<TransferResult> TransferFileAsync(string sourceFilePath, string? destinationPath = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Test connection to the destination
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the transfer method name
    /// </summary>
    string GetTransferMethod();
    
    /// <summary>
    /// Verify a transferred file
    /// </summary>
    Task<bool> VerifyTransferAsync(string sourceFilePath, string destinationPath, CancellationToken cancellationToken = default);
}
