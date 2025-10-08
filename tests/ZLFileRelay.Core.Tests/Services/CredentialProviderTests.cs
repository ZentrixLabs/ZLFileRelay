using System;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ZLFileRelay.Core.Services;

namespace ZLFileRelay.Core.Tests.Services
{
    public class CredentialProviderTests
    {
        private readonly string _testCredentialPath;
        private readonly CredentialProvider _provider;

        public CredentialProviderTests()
        {
            _testCredentialPath = Path.Combine(Path.GetTempPath(), $"test_creds_{Guid.NewGuid()}.dat");
            _provider = new CredentialProvider(NullLogger<CredentialProvider>.Instance, _testCredentialPath);
        }

        [Fact]
        public void StoreCredential_AndRetrieve_WorksCorrectly()
        {
            // Arrange
            var key = "test.password";
            var value = "SuperSecretPassword123!";

            try
            {
                // Act
                _provider.StoreCredential(key, value);
                var retrieved = _provider.GetCredential(key);

                // Assert
                Assert.Equal(value, retrieved);
            }
            finally
            {
                // Cleanup
                if (File.Exists(_testCredentialPath))
                    File.Delete(_testCredentialPath);
            }
        }

        [Fact]
        public void HasCredential_WithExistingKey_ReturnsTrue()
        {
            // Arrange
            var key = "test.key";
            var value = "test.value";

            try
            {
                // Act
                _provider.StoreCredential(key, value);
                var exists = _provider.HasCredential(key);

                // Assert
                Assert.True(exists);
            }
            finally
            {
                // Cleanup
                if (File.Exists(_testCredentialPath))
                    File.Delete(_testCredentialPath);
            }
        }

        [Fact]
        public void HasCredential_WithNonExistingKey_ReturnsFalse()
        {
            // Arrange
            var key = "nonexistent.key";

            // Act
            var exists = _provider.HasCredential(key);

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public void DeleteCredential_RemovesCredential()
        {
            // Arrange
            var key = "test.delete";
            var value = "delete.me";

            try
            {
                _provider.StoreCredential(key, value);
                Assert.True(_provider.HasCredential(key));

                // Act
                _provider.DeleteCredential(key);

                // Assert
                Assert.False(_provider.HasCredential(key));
            }
            finally
            {
                // Cleanup
                if (File.Exists(_testCredentialPath))
                    File.Delete(_testCredentialPath);
            }
        }

        [Fact]
        public void ClearAllCredentials_RemovesAllCredentials()
        {
            // Arrange
            try
            {
                _provider.StoreCredential("key1", "value1");
                _provider.StoreCredential("key2", "value2");
                _provider.StoreCredential("key3", "value3");

                // Act
                _provider.ClearAllCredentials();

                // Assert
                Assert.False(_provider.HasCredential("key1"));
                Assert.False(_provider.HasCredential("key2"));
                Assert.False(_provider.HasCredential("key3"));
            }
            finally
            {
                // Cleanup
                if (File.Exists(_testCredentialPath))
                    File.Delete(_testCredentialPath);
            }
        }

        [Fact]
        public void StoreNetworkCredential_AndRetrieve_WorksCorrectly()
        {
            // Arrange
            var username = "testuser";
            var password = "testpassword";
            var domain = "TESTDOMAIN";

            try
            {
                // Act
                _provider.StoreNetworkCredential(username, password, domain);
                var credential = _provider.GetCredential();

                // Assert
                Assert.Equal(username, credential.UserName);
                Assert.Equal(password, credential.Password);
                Assert.Equal(domain, credential.Domain);
            }
            finally
            {
                // Cleanup
                if (File.Exists(_testCredentialPath))
                    File.Delete(_testCredentialPath);
            }
        }

        [Fact]
        public void GetCredential_WithoutStoredCredentials_ThrowsException()
        {
            // Arrange
            var newProvider = new CredentialProvider(
                NullLogger<CredentialProvider>.Instance, 
                Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.dat"));

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => newProvider.GetCredential());
        }
    }
}

