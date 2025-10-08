using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ZLFileRelay.Service.Services
{
    /// <summary>
    /// Provides retry logic with exponential backoff for operations that may fail transiently
    /// </summary>
    public class RetryPolicy
    {
        private readonly ILogger _logger;
        private readonly int _maxRetries;
        private readonly TimeSpan _initialDelay;
        private readonly double _backoffMultiplier;
        private readonly TimeSpan _maxDelay;

        public RetryPolicy(ILogger logger, int maxRetries = 3, TimeSpan? initialDelay = null, 
            double backoffMultiplier = 2.0, TimeSpan? maxDelay = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxRetries = maxRetries;
            _initialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
            _backoffMultiplier = backoffMultiplier;
            _maxDelay = maxDelay ?? TimeSpan.FromMinutes(5);
        }

        /// <summary>
        /// Executes an operation with retry logic
        /// </summary>
        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, 
            CancellationToken cancellationToken = default)
        {
            var exceptions = new List<Exception>();
            var currentDelay = _initialDelay;

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger.LogInformation("Retrying {OperationName} (attempt {Attempt}/{MaxRetries})", 
                            operationName, attempt + 1, _maxRetries + 1);
                    }

                    var result = await operation();
                    
                    if (attempt > 0)
                    {
                        _logger.LogInformation("Operation {OperationName} succeeded on attempt {Attempt}", 
                            operationName, attempt + 1);
                    }

                    return result;
                }
                catch (Exception ex) when (IsRetryableException(ex) && attempt < _maxRetries)
                {
                    exceptions.Add(ex);
                    _logger.LogWarning(ex, "Operation {OperationName} failed on attempt {Attempt}/{MaxRetries}. " +
                        "Retrying in {Delay}ms", operationName, attempt + 1, _maxRetries + 1, currentDelay.TotalMilliseconds);

                    await Task.Delay(currentDelay, cancellationToken);
                    currentDelay = CalculateNextDelay(currentDelay);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    break;
                }
            }

            _logger.LogError("Operation {OperationName} failed after {MaxRetries} retries", 
                operationName, _maxRetries + 1);

            throw new AggregateException($"Operation '{operationName}' failed after {_maxRetries + 1} attempts", exceptions);
        }

        private static bool IsRetryableException(Exception ex)
        {
            return ex switch
            {
                DirectoryNotFoundException => false,
                FileNotFoundException => false,
                IOException => true,
                UnauthorizedAccessException => true,
                System.ComponentModel.Win32Exception win32Ex => win32Ex.NativeErrorCode switch
                {
                    53 => true,   // Network path not found
                    67 => true,   // Network name not found
                    121 => true,  // Semaphore timeout
                    1203 => true, // Session limit exceeded
                    1225 => true, // Remote access denied
                    1231 => true, // Network location unavailable
                    64 => true,   // Network name in use
                    1311 => true, // No logon servers available
                    86 => false,   // Invalid password
                    1326 => false, // Invalid username or password
                    1396 => false, // Logon failure
                    1909 => false, // Account locked
                    5 => false,    // Access denied
                    1331 => false, // Account disabled
                    1907 => false, // Password expired
                    1219 => false, // Multiple connections
                    _ => false
                },
                _ => false
            };
        }

        private TimeSpan CalculateNextDelay(TimeSpan currentDelay)
        {
            var nextDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * _backoffMultiplier);
            
            if (nextDelay > _maxDelay)
                nextDelay = _maxDelay;

            // Add jitter (±25% random variation)
            var random = new Random();
            var jitter = TimeSpan.FromMilliseconds(
                (random.NextDouble() - 0.5) * nextDelay.TotalMilliseconds * 0.5);

            return nextDelay + jitter;
        }
    }
}

