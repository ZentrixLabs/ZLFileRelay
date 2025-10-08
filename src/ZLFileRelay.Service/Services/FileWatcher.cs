using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ZLFileRelay.Service.Services
{
    /// <summary>
    /// Interface for file system watching
    /// </summary>
    public interface IFileWatcher : IDisposable
    {
        event EventHandler<FileSystemEventArgs>? FileDetected;
        event EventHandler<FileSystemEventArgs>? FileChanged;
        void StartWatching(string path, bool includeSubdirectories);
        void StopWatching();
    }

    /// <summary>
    /// Monitors file system for new files
    /// </summary>
    public class FileWatcher : IFileWatcher
    {
        private readonly ILogger<FileWatcher> _logger;
        private FileSystemWatcher? _watcher;
        private readonly object _disposeLock = new object();
        private bool _disposed = false;

        public event EventHandler<FileSystemEventArgs>? FileDetected;
        public event EventHandler<FileSystemEventArgs>? FileChanged;

        public FileWatcher(ILogger<FileWatcher> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void StartWatching(string path, bool includeSubdirectories)
        {
            if (_watcher != null)
            {
                throw new InvalidOperationException("Watcher is already started");
            }

            _watcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                IncludeSubdirectories = includeSubdirectories,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnFileCreated;
            _watcher.Changed += OnFileChanged;
            _watcher.Error += OnWatcherError;

            _logger.LogInformation("File watcher started for path: {Path} (IncludeSubdirectories: {IncludeSub})", 
                path, includeSubdirectories);
        }

        public void StopWatching()
        {
            lock (_disposeLock)
            {
                if (_watcher != null && !_disposed)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Created -= OnFileCreated;
                    _watcher.Changed -= OnFileChanged;
                    _watcher.Error -= OnWatcherError;
                    _logger.LogInformation("File watcher stopped");
                }
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (File.Exists(e.FullPath))
                {
                    FileDetected?.Invoke(this, e);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file creation event for {FilePath}", e.FullPath);
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (File.Exists(e.FullPath))
                {
                    FileChanged?.Invoke(this, e);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file change event for {FilePath}", e.FullPath);
            }
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            _logger.LogError(e.GetException(), "FileSystemWatcher error occurred");
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed) return;

                try
                {
                    if (_watcher != null)
                    {
                        _watcher.EnableRaisingEvents = false;
                        _watcher.Dispose();
                        _watcher = null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing file watcher");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}

