using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLFileRelay.Core.Interfaces;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.Service.Services
{
    /// <summary>
    /// Factory for creating the appropriate file transfer service based on configuration
    /// </summary>
    public interface IFileTransferServiceFactory
    {
        IFileTransferService CreateTransferService();
    }

    /// <summary>
    /// Creates instances of file transfer services based on configuration
    /// </summary>
    public class FileTransferServiceFactory : IFileTransferServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FileTransferServiceFactory> _logger;
        private readonly ZLFileRelayConfiguration _config;

        public FileTransferServiceFactory(
            IServiceProvider serviceProvider,
            ILogger<FileTransferServiceFactory> logger,
            ZLFileRelayConfiguration config)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public IFileTransferService CreateTransferService()
        {
            try
            {
                var transferMethod = _config.Service.TransferMethod?.ToUpperInvariant() ?? "SSH";

                switch (transferMethod)
                {
                    case "SSH":
                    case "SCP":
                    case "SSH_SCP":
                        _logger.LogInformation("Creating SSH/SCP file transfer service");
                        return _serviceProvider.GetRequiredService<ScpFileTransferService>();

                    case "SMB":
                    case "CIFS":
                        _logger.LogInformation("Creating SMB file transfer service");
                        return _serviceProvider.GetRequiredService<SmbFileTransferService>();

                    default:
                        _logger.LogWarning("Unknown transfer method '{TransferMethod}', defaulting to SSH/SCP", 
                            _config.Service.TransferMethod);
                        return _serviceProvider.GetRequiredService<ScpFileTransferService>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create file transfer service for method: {TransferMethod}", 
                    _config.Service.TransferMethod);
                throw;
            }
        }
    }
}

