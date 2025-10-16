using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLFileRelay.ConfigTool.Interfaces;

namespace ZLFileRelay.ConfigTool.Services;

/// <summary>
/// Manages the service account credentials for the ZLFileRelay Windows Service.
/// Uses sc.exe for maximum compatibility with older servers.
/// 
/// IMPORTANT: Service account credentials managed by this class are ONLY for running 
/// the ZLFileRelay service itself. They are NOT used for remote management operations.
/// Remote management uses either current user credentials or explicit admin credentials.
/// </summary>
public class ServiceAccountManager
{
    private readonly ILogger<ServiceAccountManager> _logger;
    private readonly IRemoteServerProvider _remoteServerProvider;
    private const string ServiceName = "ZLFileRelay";

    public ServiceAccountManager(
        ILogger<ServiceAccountManager> logger,
        IRemoteServerProvider remoteServerProvider)
    {
        _logger = logger;
        _remoteServerProvider = remoteServerProvider;
    }

    private string GetScTarget()
    {
        if (_remoteServerProvider.IsRemote && !string.IsNullOrWhiteSpace(_remoteServerProvider.ServerName))
        {
            return $"\\\\{_remoteServerProvider.ServerName} ";
        }
        return "";
    }

    public async Task<string?> GetCurrentServiceAccountAsync()
    {
        try
        {
            var scTarget = GetScTarget();
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"{scTarget}qc {ServiceName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse output for SERVICE_START_NAME
            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("SERVICE_START_NAME"))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        return parts[1].Trim();
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current service account");
            return null;
        }
    }

    /// <summary>
    /// Sets the service account for the ZLFileRelay Windows Service using sc.exe.
    /// These credentials are ONLY used for running the service, NOT for remote management.
    /// Prompts for admin credentials if current user is not an administrator.
    /// </summary>
    public async Task<bool> SetServiceAccountAsync(string username, string password, 
        string? adminUsername = null, string? adminPassword = null)
    {
        try
        {
            _logger.LogInformation("Setting service account credentials for ZLFileRelay service: {Username}", username);
            _logger.LogDebug("Note: Service account credentials are NOT used for remote management operations");
            
            // Use sc.exe to set service account
            return await SetServiceAccountWithScExeAsync(username, password, adminUsername, adminPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set service account for {Username}", username);
            return false;
        }
    }

    /// <summary>
    /// Sets the service account using sc.exe command.
    /// Handles both local and remote scenarios with admin credential elevation.
    /// </summary>
    private async Task<bool> SetServiceAccountWithScExeAsync(string username, string password, 
        string? adminUsername, string? adminPassword)
    {
        try
        {
            var scTarget = GetScTarget();
            var arguments = $"{scTarget}config {ServiceName} obj= \"{username}\" password= \"{password}\"";
            
            // Check if we're running as admin
            if (IsRunningAsAdministrator())
            {
                // Direct execution - we have admin rights
                _logger.LogDebug("Running sc.exe with current admin credentials");
                return await RunScExeAsync(arguments);
            }
            else if (!string.IsNullOrWhiteSpace(adminUsername) && !string.IsNullOrWhiteSpace(adminPassword))
            {
                // Use provided admin credentials to run sc.exe elevated
                _logger.LogDebug("Running sc.exe with provided admin credentials: {AdminUser}", adminUsername);
                return await RunScExeElevatedAsync(arguments, adminUsername, adminPassword);
            }
            else
            {
                _logger.LogError("Administrator rights required but no admin credentials provided");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set service account using sc.exe");
            return false;
        }
    }

    /// <summary>
    /// Runs sc.exe with current user credentials (must be admin)
    /// </summary>
    private async Task<bool> RunScExeAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            _logger.LogError("Failed to start sc.exe process");
            return false;
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            _logger.LogInformation("Service account updated successfully: {Output}", output);
            return true;
        }
        else
        {
            _logger.LogError("sc.exe failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
            return false;
        }
    }

    /// <summary>
    /// Runs sc.exe with elevated admin credentials
    /// </summary>
    private async Task<bool> RunScExeElevatedAsync(string arguments, string adminUsername, string adminPassword)
    {
        try
        {
            // Parse domain and username
            var parts = adminUsername.Split('\\');
            var domain = parts.Length > 1 ? parts[0] : ".";
            var user = parts.Length > 1 ? parts[1] : parts[0];

            var securePassword = new SecureString();
            foreach (char c in adminPassword)
            {
                securePassword.AppendChar(c);
            }
            securePassword.MakeReadOnly();

            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Domain = domain,
                UserName = user,
                Password = securePassword,
                LoadUserProfile = false
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start sc.exe process with admin credentials");
                return false;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Service account updated successfully with elevated credentials: {Output}", output);
                return true;
            }
            else
            {
                _logger.LogError("sc.exe failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run sc.exe with elevated credentials");
            return false;
        }
    }

    /// <summary>
    /// Checks if the current process is running with administrator privileges
    /// </summary>
    private bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public async Task<bool> GrantLogonAsServiceRightAsync(string username, 
        string? adminUsername = null, string? adminPassword = null)
    {
        try
        {
            _logger.LogInformation("Granting logon as service right to: {Username}", username);

            // Use secedit.exe for maximum compatibility
            return await GrantLogonAsServiceRightSecEditAsync(username, adminUsername, adminPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant logon as service right for {Username}", username);
            return false;
        }
    }

    /// <summary>
    /// Grants "Log on as a service" right using secedit.exe safely.
    /// This method preserves existing user rights by appending the new account.
    /// 
    /// Steps:
    /// 1. Export current security policy
    /// 2. Read and parse the exported file
    /// 3. Append the new account to existing accounts (don't overwrite)
    /// 4. Apply the updated policy
    /// 5. Optionally force group policy update
    /// </summary>
    private async Task<bool> GrantLogonAsServiceRightSecEditAsync(string username, 
        string? adminUsername = null, string? adminPassword = null)
    {
        // Use a predictable temp location for easier troubleshooting
        var tempDir = Path.Combine(Path.GetTempPath(), "ZLFileRelay");
        Directory.CreateDirectory(tempDir);
        
        var exportFile = Path.Combine(tempDir, "secconfig_export.cfg");
        var configFile = Path.Combine(tempDir, "secconfig_new.cfg");
        
        try
        {
            _logger.LogInformation("Granting 'Log on as a service' right to {Username}", username);

            // Step 1: Resolve username to SID
            var account = new NTAccount(username);
            var sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
            var sidString = $"*{sid.Value}";
            
            _logger.LogInformation("Resolved {Username} to SID: {Sid}", username, sid.Value);

            // Check if we need admin elevation
            bool needsElevation = !IsRunningAsAdministrator() && 
                                  !string.IsNullOrWhiteSpace(adminUsername) && 
                                  !string.IsNullOrWhiteSpace(adminPassword);

            _logger.LogDebug("Admin elevation {Status}", needsElevation ? "required" : "not needed");

            // Step 2: Export current security policy
            _logger.LogInformation("Step 1/4: Exporting current security policy...");
            var exportSuccess = needsElevation
                ? await RunSeceditElevatedAsync($"/export /cfg \"{exportFile}\"", adminUsername!, adminPassword!)
                : await RunSeceditAsync($"/export /cfg \"{exportFile}\"");

            if (!exportSuccess || !File.Exists(exportFile))
            {
                _logger.LogError("Failed to export security policy using secedit.exe");
                return false;
            }

            // Step 3: Read and parse current settings
            _logger.LogInformation("Step 2/4: Reading current SeServiceLogonRight settings...");
            var lines = await File.ReadAllLinesAsync(exportFile);
            var currentSetting = "";
            
            foreach (var line in lines)
            {
                if (line.StartsWith("SeServiceLogonRight", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length >= 2)
                    {
                        currentSetting = parts[1].Trim();
                    }
                    break;
                }
            }

            // Log existing accounts for transparency
            if (!string.IsNullOrEmpty(currentSetting))
            {
                _logger.LogInformation("Existing SeServiceLogonRight accounts: {Accounts}", currentSetting);
            }
            else
            {
                _logger.LogInformation("No existing SeServiceLogonRight accounts found");
            }

            // Step 4: Check if user already has the right
            if (currentSetting.Contains(sid.Value))
            {
                _logger.LogInformation("Account {Username} already has SeServiceLogonRight - no changes needed", username);
                return true;
            }

            // Step 5: Append new account to existing list (preserve existing accounts)
            _logger.LogInformation("Step 3/4: Appending {Username} to existing accounts...", username);
            
            string newSetting;
            if (string.IsNullOrEmpty(currentSetting))
            {
                // No existing accounts - just add this one
                newSetting = sidString;
                _logger.LogDebug("Creating new SeServiceLogonRight with: {Setting}", newSetting);
            }
            else
            {
                // Append to existing accounts (SAFE - preserves all existing rights)
                newSetting = $"{currentSetting},{sidString}";
                _logger.LogDebug("Appending to existing accounts. New setting: {Setting}", newSetting);
            }

            // Step 6: Create updated security configuration
            var configContent = $@"[Unicode]
Unicode=yes
[Version]
signature=""$CHICAGO$""
Revision=1
[Privilege Rights]
SeServiceLogonRight = {newSetting}";

            await File.WriteAllTextAsync(configFile, configContent);
            _logger.LogDebug("Created configuration file at: {Path}", configFile);

            // Step 7: Apply the updated policy
            _logger.LogInformation("Step 4/4: Applying updated security policy...");
            var applySuccess = needsElevation
                ? await RunSeceditElevatedAsync($"/configure /db secedit.sdb /cfg \"{configFile}\" /areas USER_RIGHTS", adminUsername!, adminPassword!)
                : await RunSeceditAsync($"/configure /db secedit.sdb /cfg \"{configFile}\" /areas USER_RIGHTS");
            
            if (!applySuccess)
            {
                _logger.LogError("Failed to apply security configuration using secedit.exe");
                return false;
            }

            _logger.LogInformation("✅ Successfully granted SeServiceLogonRight to {Username}", username);
            _logger.LogInformation("Existing accounts were preserved and {Username} was appended to the list", username);

            // Optional Step 8: Force group policy update
            try
            {
                _logger.LogDebug("Forcing group policy update (optional)...");
                var gpupdateSuccess = needsElevation
                    ? await RunCommandElevatedAsync("gpupdate.exe", "/force", adminUsername!, adminPassword!)
                    : await RunCommandAsync("gpupdate.exe", "/force");
                
                if (gpupdateSuccess)
                {
                    _logger.LogInformation("Group policy updated successfully");
                }
                else
                {
                    _logger.LogWarning("Group policy update failed, but SeServiceLogonRight was still granted");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not run gpupdate, but SeServiceLogonRight was still granted");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant SeServiceLogonRight using secedit.exe");
            return false;
        }
        finally
        {
            // Clean up temp files
            try
            {
                if (File.Exists(exportFile))
                {
                    File.Delete(exportFile);
                    _logger.LogDebug("Cleaned up export file: {Path}", exportFile);
                }
                if (File.Exists(configFile))
                {
                    File.Delete(configFile);
                    _logger.LogDebug("Cleaned up config file: {Path}", configFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temporary files");
            }
        }
    }

    private async Task<bool> RunSeceditAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "secedit.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) return false;

        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }

    private async Task<bool> RunSeceditElevatedAsync(string arguments, string adminUsername, string adminPassword)
    {
        return await RunCommandElevatedAsync("secedit.exe", arguments, adminUsername, adminPassword);
    }

    private async Task<bool> RunCommandAsync(string command, string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run {Command}", command);
            return false;
        }
    }

    private async Task<bool> RunCommandElevatedAsync(string command, string arguments, string adminUsername, string adminPassword)
    {
        try
        {
            var parts = adminUsername.Split('\\');
            var domain = parts.Length > 1 ? parts[0] : ".";
            var user = parts.Length > 1 ? parts[1] : parts[0];

            var securePassword = new SecureString();
            foreach (char c in adminPassword)
            {
                securePassword.AppendChar(c);
            }
            securePassword.MakeReadOnly();

            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Domain = domain,
                UserName = user,
                Password = securePassword,
                LoadUserProfile = false
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run {Command} with elevated credentials", command);
            return false;
        }
    }

    public async Task<bool> CheckProfileExistsAsync(string username)
    {
        try
        {
            // Extract just the username part (remove domain)
            var parts = username.Split('\\');
            var user = parts.Length > 1 ? parts[1] : parts[0];
            
            var profilePath = Path.Combine(@"C:\Users", user);
            return await Task.Run(() => Directory.Exists(profilePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check profile exists");
            return false;
        }
    }

    public async Task<string?> GetProfilePathAsync(string username)
    {
        try
        {
            var parts = username.Split('\\');
            var user = parts.Length > 1 ? parts[1] : parts[0];
            
            var profilePath = Path.Combine(@"C:\Users", user);
            return await Task.Run(() => Directory.Exists(profilePath) ? profilePath : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get profile path");
            return null;
        }
    }
}

