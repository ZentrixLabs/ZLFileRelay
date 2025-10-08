using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.ConfigTool.Services;

public class ConnectionTester
{
    private readonly ILogger<ConnectionTester> _logger;

    public ConnectionTester(ILogger<ConnectionTester> logger)
    {
        _logger = logger;
    }

    public async Task<ConnectionTestResult> TestSshAsync(SshSettings settings)
    {
        var result = new ConnectionTestResult { Method = "SSH/SCP" };

        try
        {
            // Validate settings
            if (string.IsNullOrWhiteSpace(settings.Host))
            {
                result.Success = false;
                result.Message = "SSH host is required";
                return result;
            }

            if (string.IsNullOrWhiteSpace(settings.PrivateKeyPath) || !File.Exists(settings.PrivateKeyPath))
            {
                result.Success = false;
                result.Message = $"SSH private key not found: {settings.PrivateKeyPath}";
                return result;
            }

            // Test connection
            var keyFile = new PrivateKeyFile(settings.PrivateKeyPath);
            var connectionInfo = new PrivateKeyConnectionInfo(
                settings.Host,
                settings.Port,
                settings.Username,
                keyFile);

            connectionInfo.Timeout = TimeSpan.FromSeconds(settings.ConnectionTimeout);

            using var client = new SshClient(connectionInfo);
            
            await Task.Run(() => client.Connect());

            if (client.IsConnected)
            {
                // Try to list destination directory
                var command = client.RunCommand($"ls -la {settings.DestinationPath}");
                
                result.Success = true;
                result.Message = $"Successfully connected to {settings.Host}:{settings.Port}";
                result.Details = command.Result;
                
                client.Disconnect();
            }
            else
            {
                result.Success = false;
                result.Message = "Failed to establish SSH connection";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"SSH connection failed: {ex.Message}";
            result.Details = ex.ToString();
            _logger.LogError(ex, "SSH connection test failed");
        }

        return result;
    }

    public async Task<ConnectionTestResult> TestSmbAsync(SmbSettings settings)
    {
        var result = new ConnectionTestResult { Method = "SMB/CIFS" };

        try
        {
            if (string.IsNullOrWhiteSpace(settings.SharePath))
            {
                result.Success = false;
                result.Message = "SMB share path is required";
                return result;
            }

            // Test if path is accessible
            var testResult = await Task.Run(() =>
            {
                try
                {
                    return Directory.Exists(settings.SharePath);
                }
                catch
                {
                    return false;
                }
            });

            if (testResult)
            {
                result.Success = true;
                result.Message = $"Successfully accessed {settings.SharePath}";
            }
            else
            {
                result.Success = false;
                result.Message = $"Cannot access {settings.SharePath}. Check path and credentials.";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"SMB connection failed: {ex.Message}";
            result.Details = ex.ToString();
            _logger.LogError(ex, "SMB connection test failed");
        }

        return result;
    }

    public async Task<bool> TestHostReachableAsync(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 5000);
            return reply.Status == IPStatus.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ping test failed for host: {Host}", host);
            return false;
        }
    }
}

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}

