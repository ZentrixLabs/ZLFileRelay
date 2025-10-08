using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ZLFileRelay.ConfigTool.Services;

public class PermissionManager
{
    private readonly ILogger<PermissionManager> _logger;

    public PermissionManager(ILogger<PermissionManager> logger)
    {
        _logger = logger;
    }

    public async Task<bool> GrantFolderPermissionsAsync(string folderPath, string username, FileSystemRights rights)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                _logger.LogWarning("Folder does not exist: {Path}", folderPath);
                return false;
            }

            await Task.Run(() =>
            {
                var directoryInfo = new DirectoryInfo(folderPath);
                var directorySecurity = directoryInfo.GetAccessControl();

                var identity = new NTAccount(username);
                var accessRule = new FileSystemAccessRule(
                    identity,
                    rights,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                directorySecurity.AddAccessRule(accessRule);
                directoryInfo.SetAccessControl(directorySecurity);
            });

            _logger.LogInformation("Granted {Rights} permissions to {User} on {Path}", 
                rights, username, folderPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant folder permissions");
            return false;
        }
    }

    public async Task<bool> FixSourceFolderPermissionsAsync(string sourcePath, string serviceAccount)
    {
        try
        {
            var rights = FileSystemRights.Read | FileSystemRights.Modify;
            return await GrantFolderPermissionsAsync(sourcePath, serviceAccount, rights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fix source folder permissions");
            return false;
        }
    }

    public async Task<bool> FixServiceFolderPermissionsAsync(string servicePath, string serviceAccount)
    {
        try
        {
            var rights = FileSystemRights.FullControl;
            return await GrantFolderPermissionsAsync(servicePath, serviceAccount, rights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fix service folder permissions");
            return false;
        }
    }

    public async Task<bool> CheckFolderPermissionsAsync(string folderPath, string username)
    {
        try
        {
            if (!Directory.Exists(folderPath))
                return false;

            return await Task.Run(() =>
            {
                try
                {
                    var directoryInfo = new DirectoryInfo(folderPath);
                    var directorySecurity = directoryInfo.GetAccessControl();
                    var identity = new NTAccount(username);
                    
                    // This is a simplified check - just verify user has some access
                    var rules = directorySecurity.GetAccessRules(true, true, typeof(NTAccount));
                    foreach (FileSystemAccessRule rule in rules)
                    {
                        if (rule.IdentityReference.Value.Equals(username, StringComparison.OrdinalIgnoreCase))
                        {
                            return rule.AccessControlType == AccessControlType.Allow;
                        }
                    }
                    
                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check folder permissions");
            return false;
        }
    }
}

