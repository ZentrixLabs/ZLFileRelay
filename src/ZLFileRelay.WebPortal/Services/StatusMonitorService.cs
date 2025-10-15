using Microsoft.Extensions.Hosting;
using ZLFileRelay.Core.Models;
using System.Text.Json;

namespace ZLFileRelay.WebPortal.Services
{
    /// <summary>
    /// Background service that monitors for file transfer status updates and broadcasts them via SignalR
    /// </summary>
    public class StatusMonitorService : BackgroundService
    {
        private readonly ITransferStatusService _statusService;
        private readonly ILogger<StatusMonitorService> _logger;
        private readonly ZLFileRelayConfiguration _config;
        private FileSystemWatcher? _watcher;
        private Timer? _cleanupTimer;

        public StatusMonitorService(
            ITransferStatusService statusService,
            ILogger<StatusMonitorService> logger,
            ZLFileRelayConfiguration config)
        {
            _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Status Monitor Service starting");

            try
            {
                // Ensure the status directory exists
                var statusDirectory = GetStatusDirectory();
                if (!Directory.Exists(statusDirectory))
                {
                    Directory.CreateDirectory(statusDirectory);
                    _logger.LogInformation("Created status directory: {Directory}", statusDirectory);
                }

                // Set up file system watcher for status files
                _watcher = new FileSystemWatcher(statusDirectory)
                {
                    Filter = "*.status.json",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true
                };

                _watcher.Created += OnStatusFileCreated;
                _watcher.Changed += OnStatusFileChanged;
                _watcher.Error += OnWatcherError;

                _logger.LogInformation("Watching for status updates in: {Directory}", statusDirectory);

                // Set up periodic cleanup timer (every 10 minutes)
                _cleanupTimer = new Timer(
                    async _ => await CleanupAsync(),
                    null,
                    TimeSpan.FromMinutes(10),
                    TimeSpan.FromMinutes(10));

                // Keep the service running
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Status Monitor Service stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Status Monitor Service");
                throw;
            }
        }

        private void OnStatusFileCreated(object sender, FileSystemEventArgs e)
        {
            _logger.LogDebug("Status file created: {FilePath}", e.FullPath);
            _ = ProcessStatusFileAsync(e.FullPath);
        }

        private void OnStatusFileChanged(object sender, FileSystemEventArgs e)
        {
            _logger.LogDebug("Status file changed: {FilePath}", e.FullPath);
            _ = ProcessStatusFileAsync(e.FullPath);
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            _logger.LogError(e.GetException(), "File system watcher error");
        }

        private async Task ProcessStatusFileAsync(string filePath)
        {
            try
            {
                // Wait a bit to ensure file is fully written
                await Task.Delay(100);

                // Read the status file
                var statusJson = await ReadFileWithRetryAsync(filePath);
                if (string.IsNullOrWhiteSpace(statusJson))
                {
                    return;
                }

                // Deserialize the transfer result
                var transferResult = JsonSerializer.Deserialize<TransferResult>(statusJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (transferResult == null)
                {
                    _logger.LogWarning("Failed to deserialize status file: {FilePath}", filePath);
                    return;
                }

                // Extract transfer ID from filename (format: {transferId}.status.json)
                var fileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(filePath));
                
                // Update the transfer status
                var status = transferResult.Success ? TransferStatus.Completed : TransferStatus.Failed;
                await _statusService.UpdateStatusAsync(
                    fileName,
                    status,
                    transferResult.ErrorMessage,
                    transferResult.DestinationPath);

                _logger.LogInformation("Processed status update for transfer {TransferId}: {Status}", 
                    fileName, status);

                // Delete the status file after processing
                try
                {
                    File.Delete(filePath);
                    _logger.LogDebug("Deleted status file: {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete status file: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing status file: {FilePath}", filePath);
            }
        }

        private async Task<string?> ReadFileWithRetryAsync(string filePath, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await File.ReadAllTextAsync(filePath);
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    // File might be locked, wait and retry
                    await Task.Delay(50);
                }
            }
            return null;
        }

        private async Task CleanupAsync()
        {
            try
            {
                // Clean up old transfers from memory
                await _statusService.CleanupOldTransfersAsync();

                // Clean up old status files (older than 1 hour)
                var statusDirectory = GetStatusDirectory();
                var cutoff = DateTime.Now.AddHours(-1);
                var oldFiles = Directory.GetFiles(statusDirectory, "*.status.json")
                    .Where(f => File.GetLastWriteTime(f) < cutoff)
                    .ToList();

                foreach (var file in oldFiles)
                {
                    try
                    {
                        File.Delete(file);
                        _logger.LogDebug("Deleted old status file: {FilePath}", file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old status file: {FilePath}", file);
                    }
                }

                if (oldFiles.Any())
                {
                    _logger.LogInformation("Cleaned up {Count} old status files", oldFiles.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }

        private string GetStatusDirectory()
        {
            // Use a .status subdirectory in the watch directory
            return Path.Combine(_config.Service.WatchDirectory, ".status");
        }

        public override void Dispose()
        {
            _watcher?.Dispose();
            _cleanupTimer?.Dispose();
            base.Dispose();
        }
    }
}

