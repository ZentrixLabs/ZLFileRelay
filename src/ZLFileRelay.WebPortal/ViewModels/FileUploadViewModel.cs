using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ZLFileRelay.WebPortal.ViewModels
{
    public class FileUploadViewModel
    {
        [Required]
        public List<IFormFile>? Files { get; set; } = new();

        [Required]
        public string SelectedDestination { get; set; } = "transfer";

        public Dictionary<string, string> AvailableDestinations { get; set; } = new();

        public string? Notes { get; set; }

        [Display(Name = "Queue for Automatic Transfer")]
        public bool RequiresTransfer { get; set; } = true;

        public bool ShowTransferOption { get; set; } = true;

        public string SiteName { get; set; } = string.Empty;

        public string ContactEmail { get; set; } = string.Empty;

        public long MaxFileSizeBytes { get; set; }
    }
}

