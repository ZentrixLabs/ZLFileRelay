using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.WebPortal.Pages
{
    /// <summary>
    /// Error page displayed when uploaded files exceed the maximum file size limit
    /// </summary>
    public class FileTooLargeModel : PageModel
    {
        private readonly IOptionsMonitor<ZLFileRelayConfiguration> _configMonitor;

        public FileTooLargeModel(IOptionsMonitor<ZLFileRelayConfiguration> configMonitor)
        {
            _configMonitor = configMonitor;
        }

        public string? RequestId { get; set; }
        public long MaxFileSize { get; set; }
        public string MaxFileSizeFormatted { get; set; } = string.Empty;
        public long AttemptedSize { get; set; }
        public string AttemptedSizeFormatted { get; set; } = string.Empty;
        public string? SupportEmail { get; set; }

        public void OnGet(long? attemptedSize = null)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            var config = _configMonitor.CurrentValue;
            MaxFileSize = config.Security.MaxUploadSizeBytes;
            MaxFileSizeFormatted = FormatBytes(MaxFileSize);
            
            // If attempted size was passed in query string, display it
            if (attemptedSize.HasValue && attemptedSize.Value > 0)
            {
                AttemptedSize = attemptedSize.Value;
                AttemptedSizeFormatted = FormatBytes(AttemptedSize);
            }
            else
            {
                AttemptedSizeFormatted = "Unknown";
            }
            
            SupportEmail = config.Branding?.SupportEmail;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 Bytes";
            
            string[] sizes = { "Bytes", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }
    }
}

