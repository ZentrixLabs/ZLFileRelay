using System.Diagnostics;
using System.IO;
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
        string? passphrase = null)
    {
        try
        {
            // Try using Windows SSH first (ssh-keygen.exe)
            if (File.Exists(@"C:\Windows\System32\OpenSSH\ssh-keygen.exe"))
            {
                return await GenerateWithSshKeygenAsync(keyType, outputPath, passphrase);
            }

            // Fallback to SSH.NET library
            return await GenerateWithSshNetAsync(keyType, outputPath, passphrase);
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

