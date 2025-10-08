using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ZLFileRelay.Service.Services
{
    /// <summary>
    /// Interface for file queue management
    /// </summary>
    public interface IFileQueue
    {
        bool TryEnqueue(string filePath);
        bool TryPeek(out string? filePath);
        void Remove(string filePath);
        int Count { get; }
        void UpdateFileActivity(string filePath);
        bool IsFileStable(string filePath, int stabilitySeconds);
        void CleanupStaleEntries(TimeSpan maxAge);
    }

    /// <summary>
    /// Thread-safe queue for managing file transfers
    /// </summary>
    public class FileQueue : IFileQueue
    {
        private readonly ILogger<FileQueue> _logger;
        private readonly ConcurrentDictionary<string, FileQueueItem> _files;
        private readonly object _peekLock = new object();

        public FileQueue(ILogger<FileQueue> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _files = new ConcurrentDictionary<string, FileQueueItem>();
        }

        public int Count => _files.Count;

        public bool TryEnqueue(string filePath)
        {
            var item = new FileQueueItem
            {
                FilePath = filePath,
                AddedTime = DateTime.Now,
                LastActivity = DateTime.Now
            };

            if (_files.TryAdd(filePath, item))
            {
                _logger.LogDebug("File added to queue. Current queue size: {QueueSize}", _files.Count);
                return true;
            }
            return false;
        }

        public bool TryPeek(out string? filePath)
        {
            lock (_peekLock)
            {
                var oldestFile = _files.Values
                    .OrderBy(f => f.AddedTime)
                    .FirstOrDefault();

                if (oldestFile != null)
                {
                    filePath = oldestFile.FilePath;
                    return true;
                }

                filePath = null;
                return false;
            }
        }

        public void Remove(string filePath)
        {
            if (_files.TryRemove(filePath, out _))
            {
                _logger.LogDebug("File removed from queue: {FilePath}. Current queue size: {QueueSize}", 
                    filePath, _files.Count);
            }
        }

        public void UpdateFileActivity(string filePath)
        {
            if (_files.TryGetValue(filePath, out var item))
            {
                item.LastActivity = DateTime.Now;
            }
        }

        public bool IsFileStable(string filePath, int stabilitySeconds)
        {
            if (_files.TryGetValue(filePath, out var item))
            {
                return (DateTime.Now - item.LastActivity).TotalSeconds >= stabilitySeconds;
            }
            return true;
        }

        public void CleanupStaleEntries(TimeSpan maxAge)
        {
            var cutoffTime = DateTime.Now.Subtract(maxAge);
            var removedCount = 0;

            var keysToRemove = _files
                .Where(kvp => kvp.Value.LastActivity < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                if (_files.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                _logger.LogDebug("Memory cleanup: removed {RemovedCount} stale file tracking entries", removedCount);
            }

            _logger.LogDebug("Memory status - Pending files: {PendingCount}", _files.Count);
        }

        private class FileQueueItem
        {
            public string FilePath { get; set; } = string.Empty;
            public DateTime AddedTime { get; set; }
            public DateTime LastActivity { get; set; }
        }
    }
}

