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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save credentials");
                throw;
            }
        }

        private static byte[] ProtectData(string data)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            return ProtectedData.Protect(dataBytes, null, DataProtectionScope.CurrentUser);
        }

        private static string UnprotectData(byte[] encryptedData)
        {
            try
            {
                byte[] dataBytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(dataBytes);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException(
                    "Failed to decrypt credentials. Ensure the service is running as the correct user.", ex);
            }
        }
    }
}

