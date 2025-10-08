using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.Extensions.Logging;
using ZLFileRelay.ConfigTool.Interfaces;

namespace ZLFileRelay.ConfigTool.Services;

/// <summary>
/// Service for executing PowerShell commands on remote servers via WinRM
/// </summary>
public class PowerShellRemotingService
{
    private readonly ILogger<PowerShellRemotingService> _logger;
    private readonly IRemoteServerProvider _remoteServerProvider;

    public PowerShellRemotingService(
        ILogger<PowerShellRemotingService> logger,
        IRemoteServerProvider remoteServerProvider)
    {
        _logger = logger;
        _remoteServerProvider = remoteServerProvider;
    }

    /// <summary>
    /// Tests if WinRM is available and accessible on the target server
    /// </summary>
    public async Task<bool> TestWinRMConnectionAsync(string? serverName = null)
    {
        var target = serverName ?? _remoteServerProvider.ServerName;
        if (string.IsNullOrWhiteSpace(target))
            return false;

        try
        {
            using var runspace = await CreateRunspaceAsync(target);
            runspace.Open();
            
            // Simple test command
            using var pipeline = runspace.CreatePipeline("Get-ComputerInfo -Property CsName");
            var results = await Task.Run(() => pipeline.Invoke());
            
            return results.Count > 0 && !pipeline.HadErrors;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WinRM connection test failed for {Server}", target);
            return false;
        }
    }

    /// <summary>
    /// Executes a PowerShell script on the remote server
    /// </summary>
    public async Task<PowerShellResult> ExecuteScriptAsync(string script, string? serverName = null)
    {
        var target = serverName ?? _remoteServerProvider.ServerName;
        
        // If no remote server, run locally
        if (string.IsNullOrWhiteSpace(target) || !_remoteServerProvider.IsRemote)
        {
            return await ExecuteLocalScriptAsync(script);
        }

        try
        {
            using var runspace = await CreateRunspaceAsync(target);
            runspace.Open();

            using var pipeline = runspace.CreatePipeline(script);
            var results = await Task.Run(() => pipeline.Invoke());

            var output = new List<string>();
            foreach (var result in results)
            {
                output.Add(result?.ToString() ?? string.Empty);
            }

            var errors = new List<string>();
            if (pipeline.Error.Count > 0)
            {
                foreach (var error in pipeline.Error.ReadToEnd())
                {
                    errors.Add(error?.ToString() ?? string.Empty);
                }
            }

            _logger.LogInformation("Executed script on {Server}: {Lines} output lines, {Errors} errors",
                target, output.Count, errors.Count);

            return new PowerShellResult
            {
                Success = !pipeline.HadErrors,
                Output = output,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute PowerShell script on {Server}", target);
            return new PowerShellResult
            {
                Success = false,
                Output = new List<string>(),
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Executes a PowerShell command on the remote server
    /// </summary>
    public async Task<PowerShellResult> ExecuteCommandAsync(string command, Dictionary<string, object>? parameters = null, string? serverName = null)
    {
        var target = serverName ?? _remoteServerProvider.ServerName;
        
        // If no remote server, run locally
        if (string.IsNullOrWhiteSpace(target) || !_remoteServerProvider.IsRemote)
        {
            return await ExecuteLocalCommandAsync(command, parameters);
        }

        try
        {
            using var runspace = await CreateRunspaceAsync(target);
            runspace.Open();

            using var powershell = PowerShell.Create();
            powershell.Runspace = runspace;
            powershell.AddCommand(command);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    powershell.AddParameter(param.Key, param.Value);
                }
            }

            var results = await Task.Run(() => powershell.Invoke());

            var output = new List<string>();
            foreach (var result in results)
            {
                output.Add(result?.ToString() ?? string.Empty);
            }

            var errors = new List<string>();
            if (powershell.HadErrors)
            {
                foreach (var error in powershell.Streams.Error)
                {
                    errors.Add(error?.ToString() ?? string.Empty);
                }
            }

            _logger.LogInformation("Executed command '{Command}' on {Server}: Success={Success}",
                command, target, !powershell.HadErrors);

            return new PowerShellResult
            {
                Success = !powershell.HadErrors,
                Output = output,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute PowerShell command on {Server}", target);
            return new PowerShellResult
            {
                Success = false,
                Output = new List<string>(),
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private async Task<Runspace> CreateRunspaceAsync(string serverName)
    {
        return await Task.Run(() =>
        {
            var connectionInfo = new WSManConnectionInfo(
                new Uri($"http://{serverName}:5985/wsman"));
            
            // IMPORTANT: Always use current user credentials or explicitly provided admin credentials
            // NEVER use service account credentials for remote management operations
            
            // Check if alternate admin credentials should be used
            if (!_remoteServerProvider.UseCurrentCredentials && 
                !string.IsNullOrWhiteSpace(_remoteServerProvider.AlternateUsername) &&
                !string.IsNullOrWhiteSpace(_remoteServerProvider.AlternatePassword))
            {
                // Use alternate admin credentials
                var securePassword = new System.Security.SecureString();
                foreach (char c in _remoteServerProvider.AlternatePassword)
                {
                    securePassword.AppendChar(c);
                }
                securePassword.MakeReadOnly();
                
                connectionInfo.Credential = new System.Management.Automation.PSCredential(
                    _remoteServerProvider.AlternateUsername, 
                    securePassword);
                
                _logger.LogInformation("Using alternate admin credentials for remote connection: {Username}", 
                    _remoteServerProvider.AlternateUsername);
            }
            else
            {
                // Use current user credentials (default)
                _logger.LogInformation("Using current user credentials for remote connection");
            }
            
            return RunspaceFactory.CreateRunspace(connectionInfo);
        });
    }

    private async Task<PowerShellResult> ExecuteLocalScriptAsync(string script)
    {
        try
        {
            using var powershell = PowerShell.Create();
            powershell.AddScript(script);

            var results = await Task.Run(() => powershell.Invoke());

            var output = new List<string>();
            foreach (var result in results)
            {
                output.Add(result?.ToString() ?? string.Empty);
            }

            var errors = new List<string>();
            if (powershell.HadErrors)
            {
                foreach (var error in powershell.Streams.Error)
                {
                    errors.Add(error?.ToString() ?? string.Empty);
                }
            }

            return new PowerShellResult
            {
                Success = !powershell.HadErrors,
                Output = output,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute local PowerShell script");
            return new PowerShellResult
            {
                Success = false,
                Output = new List<string>(),
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private async Task<PowerShellResult> ExecuteLocalCommandAsync(string command, Dictionary<string, object>? parameters = null)
    {
        try
        {
            using var powershell = PowerShell.Create();
            powershell.AddCommand(command);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    powershell.AddParameter(param.Key, param.Value);
                }
            }

            var results = await Task.Run(() => powershell.Invoke());

            var output = new List<string>();
            foreach (var result in results)
            {
                output.Add(result?.ToString() ?? string.Empty);
            }

            var errors = new List<string>();
            if (powershell.HadErrors)
            {
                foreach (var error in powershell.Streams.Error)
                {
                    errors.Add(error?.ToString() ?? string.Empty);
                }
            }

            return new PowerShellResult
            {
                Success = !powershell.HadErrors,
                Output = output,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute local PowerShell command");
            return new PowerShellResult
            {
                Success = false,
                Output = new List<string>(),
                Errors = new List<string> { ex.Message }
            };
        }
    }
}

/// <summary>
/// Result from PowerShell execution
/// </summary>
public class PowerShellResult
{
    public bool Success { get; set; }
    public List<string> Output { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    
    public string OutputText => string.Join(Environment.NewLine, Output);
    public string ErrorText => string.Join(Environment.NewLine, Errors);
}
