using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLFileRelay.ConfigTool.Interfaces;

namespace ZLFileRelay.ConfigTool.Services;

public class ServiceManager
{
    private readonly ILogger<ServiceManager> _logger;
    private readonly IRemoteServerProvider _remoteServerProvider;
    private const string ServiceName = "ZLFileRelay";

    public ServiceManager(
        ILogger<ServiceManager> logger,
        IRemoteServerProvider remoteServerProvider)
    {
        _logger = logger;
        _remoteServerProvider = remoteServerProvider;
    }

    private string GetMachineName()
    {
        return _remoteServerProvider.IsRemote && !string.IsNullOrWhiteSpace(_remoteServerProvider.ServerName)
            ? _remoteServerProvider.ServerName
            : ".";
    }

    private string GetScTarget()
    {
        if (_remoteServerProvider.IsRemote && !string.IsNullOrWhiteSpace(_remoteServerProvider.ServerName))
        {
            return $"\\\\{_remoteServerProvider.ServerName} ";
        }
        return "";
    }

    public Task<ServiceStatus> GetStatusAsync()
    {
        try
        {
            var machineName = GetMachineName();
            using var service = new ServiceController(ServiceName, machineName);
            var status = service.Status switch
            {
                ServiceControllerStatus.Running => ServiceStatus.Running,
                ServiceControllerStatus.Stopped => ServiceStatus.Stopped,
                ServiceControllerStatus.Paused => ServiceStatus.Paused,
                ServiceControllerStatus.StartPending => ServiceStatus.StartPending,
                ServiceControllerStatus.StopPending => ServiceStatus.StopPending,
                _ => ServiceStatus.Unknown
            };

            return Task.FromResult(status);
        }
        catch (InvalidOperationException)
        {
            // Service not installed
            return Task.FromResult(ServiceStatus.NotInstalled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service status");
            return Task.FromResult(ServiceStatus.Unknown);
        }
    }

    public Task<bool> StartAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                var machineName = GetMachineName();
                using var service = new ServiceController(ServiceName, machineName);
                if (service.Status == ServiceControllerStatus.Running)
                {
                    _logger.LogInformation("Service is already running on {Machine}", machineName);
                    return true;
                }

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                
                _logger.LogInformation("Service started successfully on {Machine}", machineName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start service on {Machine}", GetMachineName());
                return false;
            }
        });
    }

    public Task<bool> StopAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                var machineName = GetMachineName();
                using var service = new ServiceController(ServiceName, machineName);
                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    _logger.LogInformation("Service is already stopped on {Machine}", machineName);
                    return true;
                }

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                
                _logger.LogInformation("Service stopped successfully on {Machine}", machineName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop service on {Machine}", GetMachineName());
                return false;
            }
        });
    }

    public async Task<bool> RestartAsync()
    {
        var stopped = await StopAsync();
        if (!stopped) return false;
        
        await Task.Delay(2000); // Wait 2 seconds between stop and start
        
        return await StartAsync();
    }

    public async Task<bool> InstallAsync(string servicePath)
    {
        if (!IsRunningAsAdministrator())
        {
            _logger.LogError("Administrator rights required to install service");
            return false;
        }

        try
        {
            var scTarget = GetScTarget();
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"{scTarget}create {ServiceName} binPath=\"{servicePath}\" start=auto DisplayName=\"ZL File Relay Service\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Service installed successfully on {Machine}", GetMachineName());
                return true;
            }

            var error = await process.StandardError.ReadToEndAsync();
            _logger.LogError("Failed to install service on {Machine}: {Error}", GetMachineName(), error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install service on {Machine}", GetMachineName());
            return false;
        }
    }

    public async Task<bool> UninstallAsync()
    {
        if (!IsRunningAsAdministrator())
        {
            _logger.LogError("Administrator rights required to uninstall service");
            return false;
        }

        try
        {
            // Stop service first if running
            await StopAsync();

            var scTarget = GetScTarget();
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"{scTarget}delete {ServiceName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Service uninstalled successfully from {Machine}", GetMachineName());
                return true;
            }

            var error = await process.StandardError.ReadToEndAsync();
            _logger.LogError("Failed to uninstall service from {Machine}: {Error}", GetMachineName(), error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall service from {Machine}", GetMachineName());
            return false;
        }
    }

    public bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public Task OpenServiceLogsAsync(string logDirectory)
    {
        try
        {
            if (Directory.Exists(logDirectory))
            {
                Process.Start("explorer.exe", logDirectory);
                _logger.LogInformation("Opened logs directory: {Directory}", logDirectory);
            }
            else
            {
                _logger.LogWarning("Logs directory does not exist: {Directory}", logDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open logs directory");
        }

        return Task.CompletedTask;
    }
}

public enum ServiceStatus
{
    Running,
    Stopped,
    Paused,
    StartPending,
    StopPending,
    NotInstalled,
    Unknown
}

