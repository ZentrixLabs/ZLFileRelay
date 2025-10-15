using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZLFileRelay.Core.Models;
using ZLFileRelay.Core.Services;

namespace ZLFileRelay.Service.Services
{
    /// <summary>
    /// Background worker service for file transfer operations
    /// </summary>
    public class TransferWorker : BackgroundService
    {
        private readonly ILogger<TransferWorker> _logger;
        private readonly ZLFileRelayConfiguration _config;
        private readonly IFileWatcher _fileWatcher;
        private readonly IFileQueue _fileQueue;
        private readonly IFileTransferServiceFactory _fileTransferServiceFactory;
        
        private Timer? _processTimer;
        private Timer? _cleanupTimer;
        private readonly SemaphoreSlim _processingSemaphore = new SemaphoreSlim(1, 1);
        private readonly object _disposeLock = new object();
        private bool _disposed = false;
        private CancellationToken _serviceCancellationToken;
        private Task? _currentProcessingTask;

        public TransferWorker(
            ILogger<TransferWorker> logger,
            ZLFileRelayConfiguration config,
            IFileWatcher fileWatcher,
            IFileQueue fileQueue,
            IFileTransferServiceFactory fileTransferServiceFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
            _fileQueue = fileQueue ?? throw new ArgumentNullException(nameof(fileQueue));
            _fileTransferServiceFactory = fileTransferServiceFactory ?? throw new ArgumentNullException(nameof(fileTransferServiceFactory));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _serviceCancellationToken = stoppingToken;
            _logger.LogInformation("ZL File Relay Service starting...");
            _logger.LogInformation("Transfer Method: {TransferMethod}", _config.Service.TransferMethod);

            try
            {
                // Validate watch directory exists
                PathValidator.ValidateDirectory(_config.Service.WatchDirectory, mustExist: true);

                // Setup file watcher
                _fileWatcher.FileDetected += OnFileDetected;
                _fileWatcher.FileChanged += OnFileChanged;
                _fileWatcher.StartWatching(_config.Service.WatchDirectory, _config.Service.IncludeSubdirectories);

                _logger.LogInformation("Monitoring watch directory: {WatchDirectory}", _config.Service.WatchDirectory);

                // Setup processing timer
                _processTimer = new Timer(
                    ProcessPendingFiles,
                    null,
                    TimeSpan.FromSeconds(_config.Service.ProcessingIntervalSeconds),
                    TimeSpan.FromSeconds(_config.Service.ProcessingIntervalSeconds));

                // Setup memory cleanup timer (run every 5 minutes)
                _cleanupTimer = new Timer(
                    CleanupMemory, 
                    null, 
                    TimeSpan.FromMinutes(5), 
                    TimeSpan.FromMinutes(5));

                _logger.LogInformation("ZL File Relay Service started successfully");

                // Keep the service running
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start service");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ZL File Relay Service stopping...");

            DisposeResources();

            _logger.LogInformation("ZL File Relay Service stopped");

            await base.StopAsync(stoppingToken);
        }

        private void OnFileDetected(object? sender, FileSystemEventArgs e)
        {
            try
            {
                // Validate the file path for security
                PathValidator.ValidatePath(e.FullPath);
                
                // Ensure the file is within the allowed source directory
                if (!PathValidator.IsPathWithinBase(_config.Service.WatchDirectory, e.FullPath))
                {
                    _logger.LogWarning("File outside watch directory detected and ignored: {FilePath}", e.FullPath);
                    return;
                }

                // Check queue size limit before adding
                if (_fileQueue.Count >= _config.Service.MaxQueueSize)
                {
                    _logger.LogWarning("Queue size limit reached ({MaxQueueSize}). Cannot add file: {FilePath}", 
                        _config.Service.MaxQueueSize, e.FullPath);
                    return;
                }

                _logger.LogInformation("File detected: {FilePath}", e.FullPath);
                _fileQueue.TryEnqueue(e.FullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file detection event for {FilePath}", e.FullPath);
            }
        }

        private void OnFileChanged(object? sender, FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath))
            {
                _fileQueue.UpdateFileActivity(e.FullPath);
            }
        }

        private void ProcessPendingFiles(object? state)
        {
            // Use semaphore to ensure only one processing task runs at a time
            if (_processingSemaphore.Wait(0))
            {
                _currentProcessingTask = ProcessPendingFilesAsync();
                _currentProcessingTask.ContinueWith(_ => _processingSemaphore.Release(), 
                    TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        private async Task ProcessPendingFilesAsync()
        {
            try
            {
                var processedCount = 0;
                const int maxFilesPerCycle = 10;
                
                while (_fileQueue.TryPeek(out string? filePath) && processedCount < maxFilesPerCycle)
                {
                    if (_serviceCancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Service stopping - cancelling file processing");
                        break;
                    }

                    if (string.IsNullOrEmpty(filePath))
                    {
                        _fileQueue.Remove(filePath ?? string.Empty);
                        processedCount++;
                        continue;
                    }

                    try
                    {
                        // Check if file still exists
                        if (!File.Exists(filePath))
                        {
                            _fileQueue.Remove(filePath);
                            processedCount++;
                            continue;
                        }

                        // Check if file is stable (no writes for specified seconds)
                        if (!_fileQueue.IsFileStable(filePath, _config.Service.FileStabilitySeconds))
                        {
                            break;
                        }

                        // Transfer the file
                        try
                        {
                            var transferService = _fileTransferServiceFactory.CreateTransferService();
                            var result = await transferService.TransferFileAsync(filePath, null, _serviceCancellationToken);
                            
                            if (result.Success)
                            {
                                _logger.LogInformation("Successfully transferred file: {FileName} ({Duration}ms)", 
                                    result.FileName, result.Duration?.TotalMilliseconds);
                            }
                            else
                            {
                                _logger.LogError("Failed to transfer file: {FileName}. Error: {Error}", 
                                    result.FileName, result.ErrorMessage);
                            }

                            // Write status notification for WebPortal
                            await WriteStatusNotificationAsync(filePath, result);

                            _fileQueue.Remove(filePath);
                            processedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to transfer file after retries: {FilePath}", filePath);
                            
                            // Write failure notification
                            var failedResult = new TransferResult
                            {
                                Success = false,
                                FileName = Path.GetFileName(filePath),
                                SourcePath = filePath,
                                ErrorMessage = ex.Message,
                                EndTime = DateTime.Now
                            };
                            await WriteStatusNotificationAsync(filePath, failedResult);

                            _fileQueue.Remove(filePath);
                            processedCount++;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing file {FilePath}", filePath);
                        _fileQueue.Remove(filePath);
                        processedCount++;
                    }
                }
                
                if (processedCount > 0)
                {
                    _logger.LogDebug("Processed {ProcessedCount} files in this cycle", processedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in ProcessPendingFilesAsync - service will continue");
            }
        }

        private void CleanupMemory(object? state)
        {
            try
            {
                _fileQueue.CleanupStaleEntries(TimeSpan.FromHours(1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during memory cleanup");
            }
        }

        private async Task WriteStatusNotificationAsync(string sourceFilePath, TransferResult result)
        {
            try
            {
                // Extract transfer ID from source file path
                // This should match the ID generated when the file was uploaded
                var transferId = GenerateTransferId(sourceFilePath);

                // Create status directory if it doesn't exist
                var statusDirectory = Path.Combine(_config.Service.WatchDirectory, ".status");
                if (!Directory.Exists(statusDirectory))
                {
                    Directory.CreateDirectory(statusDirectory);
                }

                // Write status file
                var statusFilePath = Path.Combine(statusDirectory, $"{transferId}.status.json");
                var statusJson = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(statusFilePath, statusJson);
                _logger.LogDebug("Wrote status notification: {StatusFile}", statusFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write status notification for {SourcePath}", sourceFilePath);
            }
        }

        private static string GenerateTransferId(string filePath)
        {
            // Create a unique ID based on file path
            // This should match the logic in TransferStatusService
            var fileName = Path.GetFileName(filePath);
            var pathHash = fileName + "_" + File.GetLastWriteTime(filePath).Ticks;
            return Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(pathHash)))
                .Replace("/", "_")
                .Replace("+", "-")
                .Substring(0, 16);
        }

        private void DisposeResources()
        {
            lock (_disposeLock)
            {
                if (_disposed) return;

                try
                {
                    _fileWatcher?.StopWatching();
                    _fileWatcher?.Dispose();

                    _processTimer?.Dispose();
                    _processTimer = null;

                    _cleanupTimer?.Dispose();
                    _cleanupTimer = null;

                    if (_currentProcessingTask != null)
                    {
                        try
                        {
                            _currentProcessingTask.Wait(TimeSpan.FromSeconds(5));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error waiting for processing task to complete during disposal");
                        }
                    }

                    _processingSemaphore?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing resources");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        public override void Dispose()
        {
            DisposeResources();
            base.Dispose();
        }
    }
}

