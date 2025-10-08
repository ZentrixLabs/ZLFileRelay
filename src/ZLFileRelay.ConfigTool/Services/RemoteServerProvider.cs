using ZLFileRelay.ConfigTool.Interfaces;

namespace ZLFileRelay.ConfigTool.Services;

/// <summary>
/// Default implementation of IRemoteServerProvider
/// Stores remote server connection details and optional admin credentials (NOT service account credentials)
/// </summary>
public class RemoteServerProvider : IRemoteServerProvider
{
    private string? _serverName;
    private bool _isRemote;
    private bool _isConnected;
    private bool _useCurrentCredentials = true;
    private string? _alternateUsername;
    private string? _alternatePassword;

    public string? ServerName => _serverName;
    public bool IsRemote => _isRemote;
    public bool IsConnected => _isConnected;
    public bool UseCurrentCredentials => _useCurrentCredentials;
    public string? AlternateUsername => _alternateUsername;
    public string? AlternatePassword => _alternatePassword;

    public event EventHandler? ServerChanged;

    public void SetServer(string? serverName, bool isRemote)
    {
        var changed = _serverName != serverName || _isRemote != isRemote;
        
        _serverName = serverName;
        _isRemote = isRemote;
        _isConnected = isRemote && !string.IsNullOrWhiteSpace(serverName);

        if (changed)
        {
            ServerChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void SetCredentials(bool useCurrentCredentials, string? username = null, string? password = null)
    {
        _useCurrentCredentials = useCurrentCredentials;
        _alternateUsername = username;
        _alternatePassword = password;
    }
}

