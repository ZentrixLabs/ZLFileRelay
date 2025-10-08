namespace ZLFileRelay.ConfigTool.Interfaces;

/// <summary>
/// Provides remote server connection information to services
/// </summary>
public interface IRemoteServerProvider
{
    /// <summary>
    /// Gets the current remote server name, or null for local machine
    /// </summary>
    string? ServerName { get; }
    
    /// <summary>
    /// Gets whether currently connected to a remote server
    /// </summary>
    bool IsRemote { get; }
    
    /// <summary>
    /// Gets whether the connection is established
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Event raised when the server connection changes
    /// </summary>
    event EventHandler? ServerChanged;
    
    /// <summary>
    /// Sets the remote server name
    /// </summary>
    void SetServer(string? serverName, bool isRemote);
}

