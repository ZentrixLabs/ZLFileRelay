using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using ZLFileRelay.Core.Models;
using ZLFileRelay.WebPortal.Hubs;

namespace ZLFileRelay.WebPortal.Services
{
    /// <summary>
    /// Service for tracking file transfer status in memory and broadcasting updates via SignalR
    /// </summary>
    public interface ITransferStatusService
    {
        /// <summary>
        /// Register a new file transfer
        /// </summary>
        Task<string> RegisterTransferAsync(string filePath, string fileName, long fileSize, string uploadedBy);
        
        /// <summary>
        /// Update the status of a transfer
        /// </summary>
        Task UpdateStatusAsync(string transferId, TransferStatus status, string? errorMessage = null, string? destinationPath = null);
        
        /// <summary>
        /// Get the status of a specific transfer
        /// </summary>
        FileTransferStatus? GetTransferStatus(string transferId);
        
        /// <summary>
        /// Get all active transfers (not completed or older than retention period)
        /// </summary>
        IEnumerable<FileTransferStatus> GetActiveTransfers();
        
        /// <summary>
        /// Get transfers for a specific user
        /// </summary>
        IEnumerable<FileTransferStatus> GetUserTransfers(string username);
        
        /// <summary>
        /// Clean up old completed/failed transfers
        /// </summary>
        Task CleanupOldTransfersAsync();
    }

    public class TransferStatusService : ITransferStatusService
    {
        private readonly IHubContext<TransferStatusHub> _hubContext;
        private readonly ILogger<TransferStatusService> _logger;
        private readonly ConcurrentDictionary<string, FileTransferStatus> _transfers;
        private readonly TimeSpan _retentionPeriod = TimeSpan.FromHours(1);

        public TransferStatusService(
            IHubContext<TransferStatusHub> hubContext,
            ILogger<TransferStatusService> logger)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transfers = new ConcurrentDictionary<string, FileTransferStatus>();
        }

        public async Task<string> RegisterTransferAsync(string filePath, string fileName, long fileSize, string uploadedBy)
        {
            // Generate a unique transfer ID based on file path and timestamp
            var transferId = GenerateTransferId(filePath);
            
            var status = new FileTransferStatus
            {
                TransferId = transferId,
                FileName = fileName,
                SourcePath = filePath,
                FileSize = fileSize,
                UploadedBy = uploadedBy,
                Status = TransferStatus.Queued,
                QueuedTime = DateTime.Now
            };

            _transfers[transferId] = status;
            
            _logger.LogInformation("Registered transfer {TransferId} for file {FileName} by {User}", 
                transferId, fileName, uploadedBy);

            // Broadcast to all clients
            await BroadcastStatusUpdateAsync(status);

            return transferId;
        }

        public async Task UpdateStatusAsync(string transferId, TransferStatus status, 
            string? errorMessage = null, string? destinationPath = null)
        {
            if (!_transfers.TryGetValue(transferId, out var transfer))
            {
                _logger.LogWarning("Attempted to update non-existent transfer: {TransferId}", transferId);
                return;
            }

            var oldStatus = transfer.Status;
            transfer.Status = status;
            transfer.ErrorMessage = errorMessage;
            transfer.DestinationPath = destinationPath;

            // Update timestamps based on status
            switch (status)
            {
                case TransferStatus.Transferring:
                    transfer.StartTime = DateTime.Now;
                    break;
                case TransferStatus.Completed:
                case TransferStatus.Failed:
                    transfer.CompletedTime = DateTime.Now;
                    break;
            }

            _logger.LogInformation("Transfer {TransferId} status changed: {OldStatus} -> {NewStatus}", 
                transferId, oldStatus, status);

            // Broadcast update
            await BroadcastStatusUpdateAsync(transfer);
        }

        public FileTransferStatus? GetTransferStatus(string transferId)
        {
            return _transfers.TryGetValue(transferId, out var status) ? status : null;
        }

        public IEnumerable<FileTransferStatus> GetActiveTransfers()
        {
            var cutoff = DateTime.Now - _retentionPeriod;
            return _transfers.Values
                .Where(t => t.Status == TransferStatus.Queued || 
                           t.Status == TransferStatus.Transferring ||
                           (t.CompletedTime.HasValue && t.CompletedTime.Value > cutoff))
                .OrderByDescending(t => t.QueuedTime);
        }

        public IEnumerable<FileTransferStatus> GetUserTransfers(string username)
        {
            var cutoff = DateTime.Now - _retentionPeriod;
            return _transfers.Values
                .Where(t => t.UploadedBy.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                           (t.Status == TransferStatus.Queued || 
                            t.Status == TransferStatus.Transferring ||
                            (t.CompletedTime.HasValue && t.CompletedTime.Value > cutoff)))
                .OrderByDescending(t => t.QueuedTime);
        }

        public Task CleanupOldTransfersAsync()
        {
            var cutoff = DateTime.Now - _retentionPeriod;
            var toRemove = _transfers.Values
                .Where(t => (t.Status == TransferStatus.Completed || t.Status == TransferStatus.Failed) &&
                           t.CompletedTime.HasValue && t.CompletedTime.Value < cutoff)
                .Select(t => t.TransferId)
                .ToList();

            foreach (var transferId in toRemove)
            {
                if (_transfers.TryRemove(transferId, out _))
                {
                    _logger.LogDebug("Cleaned up old transfer: {TransferId}", transferId);
                }
            }

            if (toRemove.Any())
            {
                _logger.LogInformation("Cleaned up {Count} old transfers", toRemove.Count);
            }

            return Task.CompletedTask;
        }

        private async Task BroadcastStatusUpdateAsync(FileTransferStatus status)
        {
            try
            {
                // Send to all connected clients
                await _hubContext.Clients.All.SendAsync("TransferStatusUpdate", status);
                
                // Also send to clients subscribed to this specific transfer
                await _hubContext.Clients.Group($"transfer_{status.TransferId}")
                    .SendAsync("TransferStatusUpdate", status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast status update for transfer {TransferId}", status.TransferId);
            }
        }

        private static string GenerateTransferId(string filePath)
        {
            // Create a unique ID based on file path and current timestamp
            // This ensures uniqueness even if same file is uploaded multiple times
            var pathHash = Path.GetFileName(filePath) + "_" + DateTime.Now.Ticks;
            return Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(pathHash)))
                .Replace("/", "_")
                .Replace("+", "-")
                .Substring(0, 16);
        }
    }
}

