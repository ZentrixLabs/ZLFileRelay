using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using ZLFileRelay.Core.Models;
using ZLFileRelay.WebPortal.Services;

namespace ZLFileRelay.Core.Tests.Services
{
    public class FileUploadServiceTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly ZLFileRelayConfiguration _config;
        private readonly FileUploadService _service;

        public FileUploadServiceTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"upload_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);

            _config = new ZLFileRelayConfiguration
            {
                Paths = new PathSettings
                {
                    UploadDirectory = _testDirectory
                },
                Security = new SecuritySettings
                {
                    MaxUploadSizeBytes = 1024 * 1024 // 1MB
                },
                WebPortal = new WebPortalSettings
                {
                    BlockedFileExtensions = new List<string> { ".exe", ".dll" },
                    EnableUploadToTransfer = false
                }
            };

            // Create a mock IOptionsMonitor for testing
            var optionsMonitor = Microsoft.Extensions.Options.Options.Create(_config);
            var optionsMonitorWrapper = new OptionsMonitorWrapper(optionsMonitor);
            
            _service = new FileUploadService(optionsMonitorWrapper, NullLogger<FileUploadService>.Instance);
        }

        [Fact]
        public async Task UploadFileAsync_WithValidFile_Succeeds()
        {
            // Arrange
            var content = "Test file content";
            var fileName = "test.txt";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            var result = await _service.UploadFileAsync(stream, fileName, _testDirectory, "testuser");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(fileName, result.FileName);
            Assert.True(File.Exists(result.FilePath));
        }

        [Fact]
        public async Task UploadFileAsync_WithBlockedExtension_Fails()
        {
            // Arrange
            var fileName = "malicious.exe";
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

            // Act
            var result = await _service.UploadFileAsync(stream, fileName, _testDirectory, "testuser");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("blocked", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UploadFileAsync_WithOversizedFile_Fails()
        {
            // Arrange
            var fileName = "largefile.bin";
            var largeContent = new byte[2 * 1024 * 1024]; // 2MB (exceeds 1MB limit)
            using var stream = new MemoryStream(largeContent);

            // Act
            var result = await _service.UploadFileAsync(stream, fileName, _testDirectory, "testuser");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("exceeds maximum", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidateFile_WithValidFile_ReturnsTrue()
        {
            // Arrange
            var fileName = "document.pdf";
            var fileSize = 1024;

            // Act
            var (isValid, errorMessage) = _service.ValidateFile(fileName, fileSize);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void ValidateFile_WithBlockedExtension_ReturnsFalse()
        {
            // Arrange
            var fileName = "app.exe";
            var fileSize = 1024;

            // Act
            var (isValid, errorMessage) = _service.ValidateFile(fileName, fileSize);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
        }

        [Fact]
        public void GetUploadDestinations_ReturnsConfiguredDestinations()
        {
            // Act
            var destinations = _service.GetUploadDestinations();

            // Assert
            Assert.NotNull(destinations);
            Assert.True(destinations.Count > 0);
        }

        [Fact]
        public async Task UploadFileAsync_CreatesUserDirectory()
        {
            // Arrange
            var content = "Test content";
            var fileName = "test.txt";
            var username = "testuser";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            var result = await _service.UploadFileAsync(stream, fileName, _testDirectory, username);

            // Assert
            Assert.True(result.Success);
            var expectedUserDir = Path.Combine(_testDirectory, username);
            Assert.True(Directory.Exists(expectedUserDir));
        }

        public void Dispose()
        {
            // Cleanup
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    // Simple wrapper to convert IOptions to IOptionsMonitor for testing
    internal class OptionsMonitorWrapper : IOptionsMonitor<ZLFileRelayConfiguration>
    {
        private readonly IOptions<ZLFileRelayConfiguration> _options;

        public OptionsMonitorWrapper(IOptions<ZLFileRelayConfiguration> options)
        {
            _options = options;
        }

        public ZLFileRelayConfiguration CurrentValue => _options.Value;

        public ZLFileRelayConfiguration Get(string name) => _options.Value;

        public IDisposable OnChange(Action<ZLFileRelayConfiguration, string> listener) => new EmptyDisposable();
    }

    internal class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }
}

