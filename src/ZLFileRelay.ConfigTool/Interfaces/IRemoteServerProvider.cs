namespace ZLFileRelay.ConfigTool.Interfaces;

/// <summary>
/// Provides remote server connection information to services
/// 
/// IMPORTANT: This interface provides server connection details AND optional admin credentials.
/// Remote management credentials are handled separately from service account credentials:
///   - Current user credentials (default) - provided by Windows authentication
///   - Alternate admin credentials - provided via PowerShell remoting (stored here)
///   - Service account credentials - NEVER used for remote management (only for ZLFileRelay service itself)
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
    /// Gets whether to use current credentials (true) or alternate credentials (false)
    /// </summary>
    bool UseCurrentCredentials { get; }
    
    /// <summary>
    /// Gets the alternate admin username (if UseCurrentCredentials is false)
    /// </summary>
    string? AlternateUsername { get; }
    
    /// <summary>
    /// Gets the alternate admin password (if UseCurrentCredentials is false)
    /// </summary>
    string? AlternatePassword { get; }
    
    /// <summary>
    /// Event raised when the server connection changes
    /// </summary>
    event EventHandler? ServerChanged;
    
    /// <summary>
    /// Sets the remote server name
    /// </summary>
    void SetServer(string? serverName, bool isRemote);
    
    /// <summary>
    /// Sets the alternate admin credentials for remote connections
    /// </summary>
    void SetCredentials(bool useCurrentCredentials, string? username = null, string? password = null);
}

