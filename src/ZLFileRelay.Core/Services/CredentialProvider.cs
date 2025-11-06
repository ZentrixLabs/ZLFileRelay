using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZLFileRelay.Core.Interfaces;

namespace ZLFileRelay.Core.Services
{
    /// <summary>
    /// Provides secure credential storage using Windows DPAPI encryption
    /// </summary>
    public class CredentialProvider : ICredentialProvider
    {
        private readonly string _configPath;
        private readonly ILogger<CredentialProvider> _logger;
        private Dictionary<string, byte[]> _credentials;

        public CredentialProvider(ILogger<CredentialProvider> logger, string credentialFilePath)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Support relative or absolute paths
            _configPath = Path.IsPathRooted(credentialFilePath)
                ? credentialFilePath
                : Path.Combine(AppContext.BaseDirectory, credentialFilePath);
            
            _credentials = LoadCredentials();
        }

        // ICredentialProvider implementation
        public void StoreCredential(string key, string value)
        {
            try
            {
                _credentials[key] = ProtectData(value);
                SaveCredentials();
                _logger.LogInformation("Credential stored for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store credential for key: {Key}", key);
                throw;
            }
        }

        public string? GetCredential(string key)
        {
            try
            {
                if (_credentials.TryGetValue(key, out var encryptedValue))
                {
                    return UnprotectData(encryptedValue);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve credential for key: {Key}", key);
                throw;
            }
        }

        public bool HasCredential(string key)
        {
            return _credentials.ContainsKey(key);
        }

        public void DeleteCredential(string key)
        {
            if (_credentials.Remove(key))
            {
                SaveCredentials();
                _logger.LogInformation("Credential deleted for key: {Key}", key);
            }
        }

        public void ClearAllCredentials()
        {
            _credentials.Clear();
            SaveCredentials();
            _logger.LogInformation("All credentials cleared");
        }

        // Helper methods for NetworkCredential (SMB support)
        public void StoreNetworkCredential(string username, string password, string domain)
        {
            StoreCredential("smb.username", username);
            StoreCredential("smb.password", password);
            StoreCredential("smb.domain", domain);
        }

        public NetworkCredential GetCredential()
        {
            var username = GetCredential("smb.username");
            var password = GetCredential("smb.password");
            var domain = GetCredential("smb.domain");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("SMB credentials not found. Run the configuration tool first.");
            }

            return new NetworkCredential(username, password, domain ?? string.Empty);
        }

        public bool HasCredentials()
        {
            return HasCredential("smb.username") && HasCredential("smb.password");
        }

        private Dictionary<string, byte[]> LoadCredentials()
        {
            if (!File.Exists(_configPath))
            {
                return new Dictionary<string, byte[]>();
            }

            try
            {
                string json = File.ReadAllText(_configPath);
                var stored = JsonSerializer.Deserialize<Dictionary<string, byte[]>>(json);
                return stored ?? new Dictionary<string, byte[]>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load credentials from {Path}, starting fresh", _configPath);
                return new Dictionary<string, byte[]>();
            }
        }

        private void SaveCredentials()
        {
            try
            {
                string? directory = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(_credentials);
                File.WriteAllText(_configPath, json);

                // SECURITY FIX (HIGH-1): Set file permissions to restrict access
                SetFilePermissions(_configPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save credentials");
                throw;
            }
        }

        /// <summary>
        /// Sets secure file permissions on the credentials file.
        /// Only SYSTEM and Administrators can access the file.
        /// </summary>
        private void SetFilePermissions(string filePath)
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                {
                    _logger.LogWarning("File permission setting is only supported on Windows");
                    return;
                }

                var fileInfo = new FileInfo(filePath);
                var fileSecurity = fileInfo.GetAccessControl();

                // Remove inheritance and existing permissions
                fileSecurity.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

                // Grant SYSTEM full control
                var systemSid = new System.Security.Principal.SecurityIdentifier(
                    System.Security.Principal.WellKnownSidType.LocalSystemSid, null);
                fileSecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                    systemSid,
                    System.Security.AccessControl.FileSystemRights.FullControl,
                    System.Security.AccessControl.AccessControlType.Allow));

                // Grant Administrators full control
                var adminsSid = new System.Security.Principal.SecurityIdentifier(
                    System.Security.Principal.WellKnownSidType.BuiltinAdministratorsSid, null);
                fileSecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                    adminsSid,
                    System.Security.AccessControl.FileSystemRights.FullControl,
                    System.Security.AccessControl.AccessControlType.Allow));

                fileInfo.SetAccessControl(fileSecurity);
                _logger.LogInformation("Set secure file permissions on credentials file: {Path}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set file permissions on {Path}. File is still encrypted but may be accessible to other users.", filePath);
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static byte[] ProtectData(string data)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("Credential encryption is only supported on Windows.");
            }

            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            // SECURITY FIX (HIGH-1): Use LocalMachine scope instead of CurrentUser
            // This allows both ConfigTool (Admin) and Service (service account) to access credentials
            return ProtectedData.Protect(dataBytes, null, DataProtectionScope.LocalMachine);
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static string UnprotectData(byte[] encryptedData)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("Credential decryption is only supported on Windows.");
            }

            try
            {
                // SECURITY FIX (HIGH-1): Use LocalMachine scope instead of CurrentUser
                byte[] dataBytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(dataBytes);
            }
            catch (CryptographicException ex)
            {
                // Provide helpful error message for migration from old CurrentUser encryption
                throw new CryptographicException(
                    "Failed to decrypt credentials. If you recently upgraded, please re-enter your credentials. " +
                    "The encryption method has been updated for better security across different user accounts.", ex);
            }
        }
    }
}

