using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace ZLFileRelay.ConfigTool.Services;

/// <summary>
/// Provides Windows impersonation capabilities to run operations under a service account context.
/// Uses Windows LogonUser API to impersonate a user for file access and other operations.
/// </summary>
public class ServiceAccountImpersonator
{
    private readonly ILogger<ServiceAccountImpersonator> _logger;

    public ServiceAccountImpersonator(ILogger<ServiceAccountImpersonator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Runs an async action under the impersonated context of the specified user.
    /// Automatically reverts impersonation when done or if an exception occurs.
    /// </summary>
    /// <typeparam name="T">Return type of the action</typeparam>
    /// <param name="username">Username to impersonate (format: DOMAIN\Username or .\Username)</param>
    /// <param name="password">Password for the user</param>
    /// <param name="action">Action to execute while impersonated</param>
    /// <returns>Result of the action</returns>
    public async Task<T> ImpersonateAsync<T>(string username, string password, Func<Task<T>> action)
    {
        SafeAccessTokenHandle? handle = null;
        var shouldRevert = false;

        try
        {
            // Parse domain and username
            var parts = username.Split('\\');
            var domain = parts.Length > 1 ? parts[0] : ".";
            var user = parts.Length > 1 ? parts[1] : parts[0];

            // Logon type for interactive login (service accounts typically use LOGON32_LOGON_BATCH or LOGON32_LOGON_SERVICE)
            // We use LOGON32_LOGON_INTERACTIVE first, fallback to LOGON32_LOGON_BATCH
            const int LOGON32_LOGON_INTERACTIVE = 2;
            const int LOGON32_LOGON_BATCH = 4;
            const int LOGON32_PROVIDER_DEFAULT = 0;

            // Try interactive logon first (more privileged, needed for some file operations)
            bool result = LogonUser(
                user,
                domain,
                password,
                LOGON32_LOGON_INTERACTIVE,
                LOGON32_PROVIDER_DEFAULT,
                out handle);

            // If interactive fails, try batch logon
            if (!result && !handle.IsInvalid)
            {
                result = LogonUser(
                    user,
                    domain,
                    password,
                    LOGON32_LOGON_BATCH,
                    LOGON32_PROVIDER_DEFAULT,
                    out handle);
            }

            if (!result || handle.IsInvalid)
            {
                var error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException(
                    $"Failed to logon user '{username}'. Error code: {error}. " +
                    $"Make sure the username and password are correct.");
            }

            _logger.LogDebug("Successfully logged on as user: {Username}", username);

            // Impersonate the logged-on user
            result = ImpersonateLoggedOnUser(handle);
            if (!result)
            {
                var error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException(
                    $"Failed to impersonate user '{username}'. Error code: {error}");
            }

            shouldRevert = true;
            _logger.LogDebug("Successfully impersonated user: {Username}", username);

            // Execute the action under impersonated context
            return await action();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during impersonation for user: {Username}", username);
            throw;
        }
        finally
        {
            // Always revert impersonation and close handle
            if (shouldRevert)
            {
                RevertToSelf();
                _logger.LogDebug("Reverted impersonation");
            }

            handle?.Dispose();
        }
    }

    /// <summary>
    /// Checks if a user can access (read) a specific file path.
    /// Uses impersonation to test file access under the user's security context.
    /// </summary>
    /// <param name="username">Username to test (format: DOMAIN\Username)</param>
    /// <param name="password">Password for the user</param>
    /// <param name="filePath">File path to check access for</param>
    /// <returns>True if user can access the file, false otherwise</returns>
    public async Task<bool> CanAccessFileAsync(string username, string password, string filePath)
    {
        try
        {
            return await ImpersonateAsync(username, password, async () =>
            {
                // Test if file exists and is readable under impersonated context
                try
                {
                    if (!File.Exists(filePath))
                    {
                        _logger.LogDebug("File does not exist: {FilePath}", filePath);
                        return false;
                    }

                    // Try to open the file for reading
                    using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var canRead = stream.CanRead;
                    
                    _logger.LogDebug("File access check for {FilePath}: {Result}", filePath, canRead);
                    return canRead;
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning(ex, "Access denied to file: {FilePath}", filePath);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking file access: {FilePath}", filePath);
                    return false;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check file access for user: {Username}, file: {FilePath}", 
                username, filePath);
            return false;
        }
    }

    /// <summary>
    /// Checks if the specified username represents a built-in system account that doesn't support impersonation.
    /// </summary>
    /// <param name="username">Username to check (format: DOMAIN\Username or .\Username)</param>
    /// <returns>True if this is a system account that doesn't need impersonation</returns>
    public bool IsSystemAccount(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        // Remove domain prefix for comparison
        var userPart = username.Contains('\\') ? username.Split('\\')[1] : username;

        // Common system accounts
        var systemAccounts = new[]
        {
            "SYSTEM",
            "LocalSystem",
            "NT AUTHORITY\\SYSTEM",
            "NT AUTHORITY\\LocalService",
            "NT AUTHORITY\\NetworkService"
        };

        return systemAccounts.Any(account => 
            account.Equals(username, StringComparison.OrdinalIgnoreCase) ||
            account.Contains(userPart, StringComparison.OrdinalIgnoreCase));
    }

    // Windows API declarations
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LogonUser(
        string userName,
        string domain,
        string password,
        int logonType,
        int logonProvider,
        out SafeAccessTokenHandle token);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool ImpersonateLoggedOnUser(SafeAccessTokenHandle userToken);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool RevertToSelf();
}

