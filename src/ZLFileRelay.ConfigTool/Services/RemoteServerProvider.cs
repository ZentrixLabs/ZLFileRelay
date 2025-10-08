using ZLFileRelay.ConfigTool.Interfaces;

namespace ZLFileRelay.ConfigTool.Services;

/// <summary>
/// Default implementation of IRemoteServerProvider
/// </summary>
public class RemoteServerProvider : IRemoteServerProvider
{
    private string? _serverName;
    private bool _isRemote;
    private bool _isConnected;

    public string? ServerName => _serverName;
    public bool IsRemote => _isRemote;
    public bool IsConnected => _isConnected;

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
}

