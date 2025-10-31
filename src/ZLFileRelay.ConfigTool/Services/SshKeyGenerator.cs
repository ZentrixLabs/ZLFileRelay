using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace ZLFileRelay.ConfigTool.Services;

public class SshKeyGenerator
{
    private readonly ILogger<SshKeyGenerator> _logger;

    public SshKeyGenerator(ILogger<SshKeyGenerator> logger)
    {
        _logger = logger;
    }

    public async Task<SshKeyPair> GenerateAsync(
        SshKeyType keyType,
        string outputPath,
        string? passphrase = null,
        string? serviceAccountName = null)
    {
        try
        {
            // Try using Windows SSH first (ssh-keygen.exe)
            SshKeyPair keyPair;
            if (File.Exists(@"C:\Windows\System32\OpenSSH\ssh-keygen.exe"))
            {
                keyPair = await GenerateWithSshKeygenAsync(keyType, outputPath, passphrase);
            }
            else
            {
                // Fallback to SSH.NET library
                keyPair = await GenerateWithSshNetAsync(keyType, outputPath, passphrase);
            }

            // Set secure permissions on the private key (SSH requirement)
            await SetSecureKeyPermissionsAsync(keyPair.PrivateKeyPath, serviceAccountName);

            return keyPair;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SSH key");
            throw;
        }
    }

    private async Task<SshKeyPair> GenerateWithSshKeygenAsync(
        SshKeyType keyType,
        string outputPath,
        string? passphrase)
    {
        var keyTypeArg = keyType == SshKeyType.ED25519 ? "ed25519" : "rsa";
        var passphraseArg = string.IsNullOrWhiteSpace(passphrase) ? "-N \"\"" : $"-N \"{passphrase}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = @"C:\Windows\System32\OpenSSH\ssh-keygen.exe",
            Arguments = $"-t {keyTypeArg} -f \"{outputPath}\" {passphraseArg} -C \"ZLFileRelay\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start ssh-keygen process");

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"ssh-keygen failed: {error}");
        }

        var publicKeyPath = $"{outputPath}.pub";
        var publicKey = File.Exists(publicKeyPath) 
            ? await File.ReadAllTextAsync(publicKeyPath) 
            : string.Empty;

        _logger.LogInformation("SSH key generated successfully: {Path}", outputPath);

        return new SshKeyPair
        {
            PrivateKeyPath = outputPath,
            PublicKeyPath = publicKeyPath,
            PublicKey = publicKey.Trim(),
            KeyType = keyType
        };
    }

    private Task<SshKeyPair> GenerateWithSshNetAsync(
        SshKeyType keyType,
        string outputPath,
        string? passphrase)
    {
        // For now, we'll require ssh-keygen.exe
        // SSH.NET doesn't have built-in key generation in the current version
        throw new NotSupportedException(
            "SSH key generation requires OpenSSH to be installed. " +
            "Please install OpenSSH client for Windows or use ssh-keygen manually.");
    }

    public async Task<string?> GetPublicKeyAsync(string privateKeyPath)
    {
        try
        {
            var publicKeyPath = $"{privateKeyPath}.pub";
            if (!File.Exists(publicKeyPath))
            {
                _logger.LogWarning("Public key file not found: {Path}", publicKeyPath);
                return null;
            }

            var publicKey = await File.ReadAllTextAsync(publicKeyPath);
            return publicKey.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read public key");
            return null;
        }
    }

    public Task<bool> ValidateKeyAsync(string privateKeyPath)
    {
        try
        {
            if (!File.Exists(privateKeyPath))
                return Task.FromResult(false);

            // Try to load the key with SSH.NET
            using var keyFile = new PrivateKeyFile(privateKeyPath);
            _logger.LogInformation("SSH key is valid: {Path}", privateKeyPath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SSH key validation failed: {Path}", privateKeyPath);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Sets secure permissions on an SSH private key file (required by SSH - only owner should have access).
    /// This method grants access only to SYSTEM, Administrators, and optionally a service account.
    /// </summary>
    public async Task SetSecureKeyPermissionsAsync(string privateKeyPath, string? serviceAccountName = null)
    {
        try
        {
            if (!File.Exists(privateKeyPath))
            {
                _logger.LogWarning("Private key file not found: {Path}", privateKeyPath);
                return;
            }

            var fileInfo = new FileInfo(privateKeyPath);
            var fileSecurity = fileInfo.GetAccessControl();

            // Remove all inherited permissions
            fileSecurity.SetAccessRuleProtection(true, false);

            // Remove all existing rules
            var rules = fileSecurity.GetAccessRules(true, false, typeof(SecurityIdentifier));
            foreach (FileSystemAccessRule rule in rules)
            {
                fileSecurity.RemoveAccessRule(rule);
            }

            // Determine target owner and ACE: SSH on Windows is strict; only the key owner should have access.
            if (!string.IsNullOrWhiteSpace(serviceAccountName))
            {
                try
                {
                    var svcAccount = new NTAccount(serviceAccountName);
                    var svcSid = (SecurityIdentifier)svcAccount.Translate(typeof(SecurityIdentifier));

                    // Set owner to the service account
                    fileSecurity.SetOwner(svcSid);

                    // Grant read & execute only to the service account
                    fileSecurity.AddAccessRule(new FileSystemAccessRule(
                        svcSid,
                        FileSystemRights.Read | FileSystemRights.ReadAndExecute,
                        InheritanceFlags.None,
                        PropagationFlags.None,
                        AccessControlType.Allow));

                    _logger.LogInformation("Set SSH key owner to service account and restricted access: {Account}", serviceAccountName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set owner/ACE for service account {Account}", serviceAccountName);
                }
            }
            else
            {
                // No service account provided: restrict to current user
                var currentUser = WindowsIdentity.GetCurrent();
                if (currentUser.User != null)
                {
                    fileSecurity.SetOwner(currentUser.User);
                    fileSecurity.AddAccessRule(new FileSystemAccessRule(
                        currentUser.User,
                        FileSystemRights.Read | FileSystemRights.ReadAndExecute,
                        InheritanceFlags.None,
                        PropagationFlags.None,
                        AccessControlType.Allow));
                }
            }

            // Apply the new permissions
            fileInfo.SetAccessControl(fileSecurity);

            _logger.LogInformation("Set secure permissions on SSH private key: {Path}", privateKeyPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Permission denied setting secure permissions on {Path}. Run as Administrator.", privateKeyPath);
            throw new UnauthorizedAccessException(
                $"Permission denied setting secure permissions on SSH key. Please run as Administrator. Path: {privateKeyPath}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secure permissions on {Path}", privateKeyPath);
            throw;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Fixes permissions on an existing SSH private key file.
    /// This is useful when permissions have become too open and SSH is rejecting the key.
    /// </summary>
    public async Task<bool> FixKeyPermissionsAsync(string privateKeyPath, string? serviceAccountName = null)
    {
        try
        {
            await SetSecureKeyPermissionsAsync(privateKeyPath, serviceAccountName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fix permissions on {Path}", privateKeyPath);
            return false;
        }
    }
}

public enum SshKeyType
{
    ED25519,
    RSA
}

public class SshKeyPair
{
    public string PrivateKeyPath { get; set; } = string.Empty;
    public string PublicKeyPath { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public SshKeyType KeyType { get; set; }
}

