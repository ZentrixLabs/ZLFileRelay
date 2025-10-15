using Microsoft.AspNetCore.SignalR;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.WebPortal.Hubs
{
    /// <summary>
    /// SignalR Hub for broadcasting file transfer status updates to connected clients
    /// </summary>
    public class TransferStatusHub : Hub
    {
        private readonly ILogger<TransferStatusHub> _logger;

        public TransferStatusHub(ILogger<TransferStatusHub> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogDebug("Client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogWarning(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
            }
            else
            {
                _logger.LogDebug("Client disconnected: {ConnectionId}", Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Client can call this to subscribe to updates for a specific file
        /// </summary>
        /// <param name="transferId">The transfer ID to monitor</param>
        public async Task SubscribeToTransfer(string transferId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"transfer_{transferId}");
            _logger.LogDebug("Client {ConnectionId} subscribed to transfer {TransferId}", 
                Context.ConnectionId, transferId);
        }

        /// <summary>
        /// Client can call this to unsubscribe from updates for a specific file
        /// </summary>
        /// <param name="transferId">The transfer ID to stop monitoring</param>
        public async Task UnsubscribeFromTransfer(string transferId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"transfer_{transferId}");
            _logger.LogDebug("Client {ConnectionId} unsubscribed from transfer {TransferId}", 
                Context.ConnectionId, transferId);
        }
    }
}

