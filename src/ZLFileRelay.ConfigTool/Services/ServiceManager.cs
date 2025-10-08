using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ZLFileRelay.ConfigTool.Services;

public class ServiceManager
{
    private readonly ILogger<ServiceManager> _logger;
    private const string ServiceName = "ZLFileRelay";

    public ServiceManager(ILogger<ServiceManager> logger)
    {
        _logger = logger;
    }

    public Task<ServiceStatus> GetStatusAsync()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
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

    public async Task<bool> StartAsync()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            if (service.Status == ServiceControllerStatus.Running)
            {
                _logger.LogInformation("Service is already running");
                return true;
            }

            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
            
            _logger.LogInformation("Service started successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start service");
            return false;
        }
    }

    public async Task<bool> StopAsync()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            if (service.Status == ServiceControllerStatus.Stopped)
            {
                _logger.LogInformation("Service is already stopped");
                return true;
            }

            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            
            _logger.LogInformation("Service stopped successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop service");
            return false;
        }
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
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"create {ServiceName} binPath=\"{servicePath}\" start=auto DisplayName=\"ZL File Relay Service\"",
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
                _logger.LogInformation("Service installed successfully");
                return true;
            }

            var error = await process.StandardError.ReadToEndAsync();
            _logger.LogError("Failed to install service: {Error}", error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install service");
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

            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"delete {ServiceName}",
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
                _logger.LogInformation("Service uninstalled successfully");
                return true;
            }

            var error = await process.StandardError.ReadToEndAsync();
            _logger.LogError("Failed to uninstall service: {Error}", error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall service");
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

