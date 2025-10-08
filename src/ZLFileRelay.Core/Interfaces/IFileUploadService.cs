using ZLFileRelay.Core.Models;

namespace ZLFileRelay.Core.Interfaces;

/// <summary>
/// Interface for file upload operations in the web portal
/// </summary>
public interface IFileUploadService
{
    /// <summary>
    /// Upload a single file
    /// </summary>
    Task<UploadResult> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string destination,
        string uploadedBy,
        bool requiresTransfer = false,
        string? notes = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Upload multiple files
    /// </summary>
    Task<List<UploadResult>> UploadFilesAsync(
        IEnumerable<(Stream stream, string fileName)> files,
        string destination,
        string uploadedBy,
        bool requiresTransfer = false,
        string? notes = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get available upload destinations
    /// </summary>
    Dictionary<string, string> GetUploadDestinations();
    
    /// <summary>
    /// Validate file before upload
    /// </summary>
    (bool isValid, string? errorMessage) ValidateFile(string fileName, long fileSize);
}
