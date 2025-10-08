using System.IO;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
using System.Security.Principal;
using ZLFileRelay.ConfigTool.Interfaces;
using ZLFileRelay.ConfigTool.ViewModels;

namespace ZLFileRelay.ConfigTool.Services;

public class PreFlightCheckService
{
    private readonly ConfigurationService _configurationService;
    private readonly PowerShellRemotingService _psRemoting;
    private readonly IRemoteServerProvider _remoteServerProvider;

    public PreFlightCheckService(
        ConfigurationService configurationService,
        PowerShellRemotingService psRemoting,
        IRemoteServerProvider remoteServerProvider)
    {
        _configurationService = configurationService;
        _psRemoting = psRemoting;
        _remoteServerProvider = remoteServerProvider;
    }

    public async Task<PreFlightResult> RunAllChecksAsync()
    {
        var checks = new List<PreFlightCheck>();

        // 1. Configuration file validity
        checks.Add(await CheckConfigurationValidityAsync());

        // 2. Required directories
        checks.Add(CheckRequiredDirectories());

        // 3. Service account permissions
        checks.Add(CheckServiceAccountPermissions());

        // 4. WinRM availability (if remote)
        if (_remoteServerProvider.IsRemote && !string.IsNullOrWhiteSpace(_remoteServerProvider.ServerName))
        {
            checks.Add(await CheckWinRMAvailabilityAsync());
        }

        // 5. SSH key file accessibility
        checks.Add(CheckSshKeyFile());

        // 6. Port availability
        checks.Add(CheckPortAvailability());

        // 7. Disk space
        checks.Add(CheckDiskSpace());

        return new PreFlightResult(checks);
    }

    private async Task<PreFlightCheck> CheckConfigurationValidityAsync()
    {
        try
        {
            var config = _configurationService.CurrentConfiguration;
            if (config == null)
            {
                return new PreFlightCheck
                {
                    Name = "Configuration File",
                    Status = CheckStatus.Error,
                    Message = "Configuration not loaded",
                    Details = "The configuration file could not be loaded or is missing.",
                    CanAutoFix = false
                };
            }

            var validationResult = await _configurationService.ValidateAsync(config);
            
            if (validationResult.IsValid)
            {
                return new PreFlightCheck
                {
                    Name = "Configuration File",
                    Status = CheckStatus.Pass,
                    Message = "Configuration is valid",
                    Details = "All configuration settings are present and valid."
                };
            }
            else
            {
                return new PreFlightCheck
                {
                    Name = "Configuration File",
                    Status = CheckStatus.Error,
                    Message = $"Configuration has {validationResult.Errors.Count} error(s)",
                    Details = string.Join("\n", validationResult.Errors),
                    CanAutoFix = false
                };
            }
        }
        catch (Exception ex)
        {
            return new PreFlightCheck
            {
                Name = "Configuration File",
                Status = CheckStatus.Error,
                Message = "Failed to validate configuration",
                Details = ex.Message,
                CanAutoFix = false
            };
        }
    }

    private PreFlightCheck CheckRequiredDirectories()
    {
        try
        {
            var config = _configurationService.CurrentConfiguration;
            if (config == null)
            {
                return new PreFlightCheck
                {
                    Name = "Required Directories",
                    Status = CheckStatus.Error,
                    Message = "Cannot check directories - configuration not loaded"
                };
            }

            var requiredDirs = new[]
            {
                config.Paths.UploadDirectory,
                config.Paths.LogDirectory,
                config.Paths.ConfigDirectory,
                config.Service.WatchDirectory
            };

            var missingDirs = requiredDirs.Where(dir => !Directory.Exists(dir)).ToList();

            if (missingDirs.Count == 0)
            {
                return new PreFlightCheck
                {
                    Name = "Required Directories",
                    Status = CheckStatus.Pass,
                    Message = "All required directories exist",
                    Details = $"Checked {requiredDirs.Length} directories - all present."
                };
            }
            else
            {
                return new PreFlightCheck
                {
                    Name = "Required Directories",
                    Status = CheckStatus.Warning,
                    Message = $"{missingDirs.Count} director{(missingDirs.Count == 1 ? "y" : "ies")} missing",
                    Details = $"Missing directories:\n{string.Join("\n", missingDirs)}",
                    CanAutoFix = true,
                    AutoFixAction = () =>
                    {
                        foreach (var dir in missingDirs)
                        {
                            Directory.CreateDirectory(dir);
                        }
                        return $"Created {missingDirs.Count} director{(missingDirs.Count == 1 ? "y" : "ies")}";
                    }
                };
            }
        }
        catch (Exception ex)
        {
            return new PreFlightCheck
            {
                Name = "Required Directories",
                Status = CheckStatus.Error,
                Message = "Failed to check directories",
                Details = ex.Message
            };
        }
    }

    private PreFlightCheck CheckServiceAccountPermissions()
    {
        try
        {
            var config = _configurationService.CurrentConfiguration;
            if (config == null)
            {
                return new PreFlightCheck
                {
                    Name = "Service Account Permissions",
                    Status = CheckStatus.Warning,
                    Message = "Cannot verify - configuration not loaded"
                };
            }

            // Check if current user has write access to key directories
            var dirsToCheck = new[]
            {
                config.Paths.UploadDirectory,
                config.Paths.LogDirectory,
                config.Service.WatchDirectory
            };

            var inaccessibleDirs = new List<string>();

            foreach (var dir in dirsToCheck.Where(Directory.Exists))
            {
                if (!HasWriteAccess(dir))
                {
                    inaccessibleDirs.Add(dir);
                }
            }

            if (inaccessibleDirs.Count == 0)
            {
                return new PreFlightCheck
                {
                    Name = "Service Account Permissions",
                    Status = CheckStatus.Pass,
                    Message = "Current user has write access to all directories",
                    Details = "Permission check passed for all required directories."
                };
            }
            else
            {
                return new PreFlightCheck
                {
                    Name = "Service Account Permissions",
                    Status = CheckStatus.Warning,
                    Message = $"Write access denied to {inaccessibleDirs.Count} director{(inaccessibleDirs.Count == 1 ? "y" : "ies")}",
                    Details = $"Service account may not have access to:\n{string.Join("\n", inaccessibleDirs)}\n\nUse the Service Account tab to grant permissions.",
                    CanAutoFix = false
                };
            }
        }
        catch (Exception ex)
        {
            return new PreFlightCheck
            {
                Name = "Service Account Permissions",
                Status = CheckStatus.Warning,
                Message = "Could not verify permissions",
                Details = ex.Message
            };
        }
    }

    private async Task<PreFlightCheck> CheckWinRMAvailabilityAsync()
    {
        try
        {
            var serverName = _remoteServerProvider.ServerName;
            var isAvailable = await _psRemoting.TestWinRMConnectionAsync(serverName);

            if (isAvailable)
            {
                return new PreFlightCheck
                {
                    Name = "WinRM Availability",
                    Status = CheckStatus.Pass,
                    Message = $"WinRM is available on {serverName}",
                    Details = "PowerShell Remoting is enabled and accessible. Full remote management features are available."
                };
            }
            else
            {
                return new PreFlightCheck
                {
                    Name = "WinRM Availability",
                    Status = CheckStatus.Warning,
                    Message = "WinRM is not available",
                    Details = $"PowerShell Remoting is not enabled on {serverName}. Some advanced features will not work remotely (e.g., granting service account rights).\n\n" +
                              "To enable WinRM, run on the remote server:\n" +
                              "Enable-PSRemoting -Force\n\n" +
                              "Basic operations (start/stop service, configuration changes) will still work via SMB/RPC.",
                    CanAutoFix = false
                };
            }
        }
        catch (Exception ex)
        {
            return new PreFlightCheck
            {
                Name = "WinRM Availability",
                Status = CheckStatus.Warning,
                Message = "Unable to test WinRM",
                Details = $"Error testing WinRM: {ex.Message}\n\nThis check is optional. Basic remote operations should still work.",
                CanAutoFix = false
            };
        }
    }

    private PreFlightCheck CheckSshKeyFile()
    {
        try
        {
            var config = _configurationService.CurrentConfiguration;
            if (config == null || config.Service.TransferMethod.ToLower() != "ssh")
            {
                return new PreFlightCheck
                {
                    Name = "SSH Key File",
                    Status = CheckStatus.Info,
                    Message = "Not applicable (SSH not configured)"
                };
            }

            var keyPath = config.Transfer?.Ssh?.PrivateKeyPath;
            
            if (string.IsNullOrWhiteSpace(keyPath))
            {
                return new PreFlightCheck
                {
                    Name = "SSH Key File",
                    Status = CheckStatus.Warning,
                    Message = "SSH key path not configured",
                    Details = "Configure SSH key in the SSH Settings tab."
                };
            }

            if (!File.Exists(keyPath))
            {
                return new PreFlightCheck
                {
                    Name = "SSH Key File",
                    Status = CheckStatus.Error,
                    Message = "SSH key file not found",
                    Details = $"Key file does not exist: {keyPath}\n\nGenerate a key pair in the SSH Settings tab.",
                    CanAutoFix = false
                };
            }

            // Check if file is readable
            try
            {
                using (var fs = File.OpenRead(keyPath))
                {
                    // Just checking if we can open it
                }

                return new PreFlightCheck
                {
                    Name = "SSH Key File",
                    Status = CheckStatus.Pass,
                    Message = "SSH key file is accessible",
                    Details = $"Key file: {keyPath}"
                };
            }
            catch
            {
                return new PreFlightCheck
                {
                    Name = "SSH Key File",
                    Status = CheckStatus.Error,
                    Message = "Cannot read SSH key file",
                    Details = $"Key file exists but cannot be read: {keyPath}\n\nCheck file permissions."
                };
            }
        }
        catch (Exception ex)
        {
            return new PreFlightCheck
            {
                Name = "SSH Key File",
                Status = CheckStatus.Warning,
                Message = "Failed to check SSH key",
                Details = ex.Message
            };
        }
    }

    private PreFlightCheck CheckPortAvailability()
    {
        try
        {
            // Check if any critical ports are in use (this is a basic check)
            // In a real implementation, you'd check the specific ports the web portal uses
            
            return new PreFlightCheck
            {
                Name = "Port Availability",
                Status = CheckStatus.Info,
                Message = "Port check not implemented",
                Details = "Manual verification recommended if web portal is enabled."
            };
        }
        catch (Exception ex)
        {
            return new PreFlightCheck
            {
                Name = "Port Availability",
                Status = CheckStatus.Warning,
                Message = "Failed to check port availability",
                Details = ex.Message
            };
        }
    }

    private PreFlightCheck CheckDiskSpace()
    {
        try
        {
            var config = _configurationService.CurrentConfiguration;
            if (config == null)
            {
                return new PreFlightCheck
                {
                    Name = "Disk Space",
                    Status = CheckStatus.Warning,
                    Message = "Cannot check - configuration not loaded"
                };
            }

            var uploadDir = config.Paths.UploadDirectory;
            if (!Directory.Exists(uploadDir))
            {
                return new PreFlightCheck
                {
                    Name = "Disk Space",
                    Status = CheckStatus.Info,
                    Message = "Upload directory not found, cannot check disk space"
                };
            }

            var driveInfo = new DriveInfo(Path.GetPathRoot(uploadDir)!);
            var freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            var minimumGB = config.Service.MinimumFreeDiskSpaceBytes / (1024.0 * 1024.0 * 1024.0);

            if (freeSpaceGB >= minimumGB)
            {
                return new PreFlightCheck
                {
                    Name = "Disk Space",
                    Status = CheckStatus.Pass,
                    Message = $"Sufficient disk space available",
                    Details = $"Available: {freeSpaceGB:F2} GB\nRequired: {minimumGB:F2} GB"
                };
            }
            else
            {
                return new PreFlightCheck
                {
                    Name = "Disk Space",
                    Status = CheckStatus.Warning,
                    Message = "Low disk space",
                    Details = $"Available: {freeSpaceGB:F2} GB\nRequired: {minimumGB:F2} GB\n\nService may fail if disk fills up."
                };
            }
        }
        catch (Exception ex)
        {
            return new PreFlightCheck
            {
                Name = "Disk Space",
                Status = CheckStatus.Warning,
                Message = "Failed to check disk space",
                Details = ex.Message
            };
        }
    }

    private bool HasWriteAccess(string directoryPath)
    {
        try
        {
            var testFile = Path.Combine(directoryPath, $".test_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public enum CheckStatus
{
    Pass,      // ✓ Green - All good
    Warning,   // ⚠ Orange - Can proceed but with caution
    Error,     // ✗ Red - Should not proceed
    Info       // ℹ Blue - Informational only
}

public class PreFlightCheck
{
    public string Name { get; set; } = string.Empty;
    public CheckStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public bool CanAutoFix { get; set; }
    public Func<string>? AutoFixAction { get; set; }

    public string StatusIcon => Status switch
    {
        CheckStatus.Pass => "\uE73E",    // CheckMark
        CheckStatus.Warning => "\uE7BA", // Warning
        CheckStatus.Error => "\uE711",   // ErrorBadge
        CheckStatus.Info => "\uE946"     // Info
    };

    public System.Windows.Media.Brush StatusColor => Status switch
    {
        CheckStatus.Pass => System.Windows.Media.Brushes.Green,
        CheckStatus.Warning => System.Windows.Media.Brushes.Orange,
        CheckStatus.Error => System.Windows.Media.Brushes.Red,
        CheckStatus.Info => System.Windows.Media.Brushes.DodgerBlue
    };
}

public class PreFlightResult
{
    public List<PreFlightCheck> Checks { get; }
    public DateTime CheckTime { get; }

    public PreFlightResult(List<PreFlightCheck> checks)
    {
        Checks = checks;
        CheckTime = DateTime.Now;
    }

    public bool HasErrors => Checks.Any(c => c.Status == CheckStatus.Error);
    public bool HasWarnings => Checks.Any(c => c.Status == CheckStatus.Warning);
    public bool CanProceed => !HasErrors;

    public int PassCount => Checks.Count(c => c.Status == CheckStatus.Pass);
    public int WarningCount => Checks.Count(c => c.Status == CheckStatus.Warning);
    public int ErrorCount => Checks.Count(c => c.Status == CheckStatus.Error);
    public int InfoCount => Checks.Count(c => c.Status == CheckStatus.Info);

    public string Summary => $"{PassCount} passed, {WarningCount} warnings, {ErrorCount} errors";
}

