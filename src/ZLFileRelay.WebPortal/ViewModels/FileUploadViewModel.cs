using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ZLFileRelay.WebPortal.ViewModels
{
    public class FileUploadViewModel
    {
        [Required]
        public List<IFormFile>? Files { get; set; } = new();

        public string? Notes { get; set; }

        [Display(Name = "Send to SCADA")]
        public bool RequiresTransfer { get; set; } = true;

        public bool ShowTransferOption { get; set; } = true;

        public string SiteName { get; set; } = string.Empty;

        public string ContactEmail { get; set; } = string.Empty;

        public long MaxFileSizeBytes { get; set; }
    }
}

