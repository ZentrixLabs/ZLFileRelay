using System;
using Microsoft.Extensions.Configuration;
using Xunit;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.Core.Tests.Models
{
    public class ConfigurationTests
    {
        [Fact]
        public void ZLFileRelayConfiguration_DefaultValues_AreSet()
        {
            // Arrange & Act
            var config = new ZLFileRelayConfiguration();

            // Assert
            Assert.NotNull(config.Branding);
            Assert.NotNull(config.Paths);
            Assert.NotNull(config.Logging);
            Assert.NotNull(config.Service);
            Assert.NotNull(config.WebPortal);
            Assert.NotNull(config.Transfer);
            Assert.NotNull(config.Security);
        }

        [Fact]
        public void ServiceSettings_DefaultTransferMethod_IsSSH()
        {
            // Arrange & Act
            var config = new ZLFileRelayConfiguration();

            // Assert
            Assert.Equal("ssh", config.Service.TransferMethod);
        }

        [Fact]
        public void ServiceSettings_DefaultRetryAttempts_IsThree()
        {
            // Arrange & Act
            var config = new ZLFileRelayConfiguration();

            // Assert
            Assert.Equal(3, config.Service.RetryAttempts);
        }

        [Fact]
        public void SshSettings_DefaultPort_Is22()
        {
            // Arrange & Act
            var config = new ZLFileRelayConfiguration();

            // Assert
            Assert.Equal(22, config.Transfer.Ssh.Port);
        }

        [Fact]
        public void SecuritySettings_AllowExecutableFiles_DefaultIsTrue()
        {
            // Arrange & Act
            var config = new ZLFileRelayConfiguration();

            // Assert
            Assert.True(config.Security.AllowExecutableFiles);
        }

        [Fact]
        public void Configuration_BindsFromJson_Successfully()
        {
            // Arrange
            var jsonConfig = @"{
                ""ZLFileRelay"": {
                    ""Service"": {
                        ""TransferMethod"": ""smb"",
                        ""RetryAttempts"": 5
                    },
                    ""Transfer"": {
                        ""Ssh"": {
                            ""Host"": ""testhost"",
                            ""Port"": 2222
                        }
                    }
                }
            }";

            var configuration = new ConfigurationBuilder()
                .AddJsonStream(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
                .Build();

            var config = new ZLFileRelayConfiguration();

            // Act
            configuration.GetSection("ZLFileRelay").Bind(config);

            // Assert
            Assert.Equal("smb", config.Service.TransferMethod);
            Assert.Equal(5, config.Service.RetryAttempts);
            Assert.Equal("testhost", config.Transfer.Ssh.Host);
            Assert.Equal(2222, config.Transfer.Ssh.Port);
        }
    }
}

