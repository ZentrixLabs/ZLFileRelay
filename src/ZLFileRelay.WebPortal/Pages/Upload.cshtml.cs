using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ZLFileRelay.Core.Interfaces;
using ZLFileRelay.Core.Models;
using ZLFileRelay.WebPortal.Services;
using ZLFileRelay.WebPortal.ViewModels;

namespace ZLFileRelay.WebPortal.Pages
{
    [Authorize] // Require authentication for upload page
    // CRITICAL: These attributes override ASP.NET Core's default 128 MB multipart body limit
    // Must be at class level for Razor Pages (not on handler methods)
    [RequestSizeLimit(10_737_418_240)] // 10 GB
    [RequestFormLimits(MultipartBodyLengthLimit = 10_737_418_240)] // 10 GB
    public class UploadModel : PageModel
    {
        private readonly ILogger<UploadModel> _logger;
        private readonly FileUploadService _uploadService;
        private readonly AuthorizationService _authService;
        private readonly IOptionsMonitor<ZLFileRelayConfiguration> _configMonitor;

        [BindProperty]
        public FileUploadViewModel UploadViewModel { get; set; } = new();

        public UploadModel(
            ILogger<UploadModel> logger,
            IFileUploadService uploadService,
            AuthorizationService authService,
            IOptionsMonitor<ZLFileRelayConfiguration> configMonitor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uploadService = (uploadService as FileUploadService) ?? throw new ArgumentNullException(nameof(uploadService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _configMonitor = configMonitor ?? throw new ArgumentNullException(nameof(configMonitor));
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get current config (reloads automatically if config file changed)
            var config = _configMonitor.CurrentValue;
            
            // Check authorization (role-based)
            if (!await _authService.IsUserAllowedAsync(User))
            {
                _logger.LogWarning("Unauthorized access attempt by {User}", User.Identity?.Name);
                return RedirectToPage("/NotAuthorized");
            }

            _logger.LogInformation("Upload page accessed by user: {User}", User.Identity?.Name);

            // Initialize the viewmodel
            UploadViewModel = new FileUploadViewModel
            {
                ShowTransferOption = config.WebPortal.EnableUploadToTransfer,
                RequiresTransfer = true, // Default to sending to SCADA
                SiteName = config.Branding.SiteName,
                ContactEmail = config.Branding.SupportEmail,
                MaxFileSizeBytes = config.Security.MaxUploadSizeBytes
            };

            return Page();
        }

        [EnableRateLimiting("upload")]
        public async Task<IActionResult> OnPostAsync()
        {
            // Check authorization (role-based)
            if (!await _authService.IsUserAllowedAsync(User))
            {
                _logger.LogWarning("Unauthorized upload attempt by {User}", User.Identity?.Name);
                return RedirectToPage("/NotAuthorized");
            }
            
            var config = _configMonitor.CurrentValue;

            if (!ModelState.IsValid)
            {
                RepopulateViewModel();
                return Page();
            }

            if (UploadViewModel.Files == null || !UploadViewModel.Files.Any() || 
                UploadViewModel.Files.All(f => f.Length == 0))
            {
                ModelState.AddModelError("UploadViewModel.Files", "Please select at least one file to upload.");
                RepopulateViewModel();
                return Page();
            }

            // Get the authenticated user
            var username = User.Identity?.Name ?? "unknown";

            // Process the file uploads
            var results = await _uploadService.UploadFormFilesAsync(
                UploadViewModel.Files,
                username,
                null,
                UploadViewModel.RequiresTransfer,
                UploadViewModel.Notes);

            // Store results in TempData
            TempData["UploadResults"] = System.Text.Json.JsonSerializer.Serialize(results);
            TempData["Username"] = username;

            return RedirectToPage("Result");
        }

        private void RepopulateViewModel()
        {
            var config = _configMonitor.CurrentValue;
            UploadViewModel.ShowTransferOption = config.WebPortal.EnableUploadToTransfer;
            UploadViewModel.SiteName = config.Branding.SiteName;
            UploadViewModel.ContactEmail = config.Branding.SupportEmail;
            UploadViewModel.MaxFileSizeBytes = config.Security.MaxUploadSizeBytes;
        }
    }
}

