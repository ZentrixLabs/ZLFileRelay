using System.Runtime.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using ZLFileRelay.Core.Interfaces;
using ZLFileRelay.Core.Models;
using ZLFileRelay.WebPortal.Services;
using ZLFileRelay.WebPortal.ViewModels;

namespace ZLFileRelay.WebPortal.Pages
{
    [SupportedOSPlatform("windows")]
    public class UploadModel : PageModel
    {
        private readonly ILogger<UploadModel> _logger;
        private readonly FileUploadService _uploadService;
        private readonly AuthorizationService _authService;
        private readonly ZLFileRelayConfiguration _config;

        [BindProperty]
        public FileUploadViewModel UploadViewModel { get; set; } = new();

        public UploadModel(
            ILogger<UploadModel> logger,
            IFileUploadService uploadService,
            AuthorizationService authService,
            ZLFileRelayConfiguration config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uploadService = (uploadService as FileUploadService) ?? throw new ArgumentNullException(nameof(uploadService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public IActionResult OnGet()
        {
            // Check authorization
            if (_config.WebPortal.RequireAuthentication && !_authService.IsUserAllowed(User))
            {
                _logger.LogWarning("Unauthorized access attempt by {User}", User.Identity?.Name);
                return RedirectToPage("/NotAuthorized");
            }

            _logger.LogInformation("Upload page accessed by user: {User}", User.Identity?.Name);

            // Initialize the viewmodel
            UploadViewModel = new FileUploadViewModel
            {
                ShowTransferOption = _config.WebPortal.EnableUploadToTransfer,
                RequiresTransfer = true, // Default to sending to SCADA
                SiteName = _config.Branding.SiteName,
                ContactEmail = _config.Branding.SupportEmail,
                MaxFileSizeBytes = _config.Security.MaxUploadSizeBytes
            };

            return Page();
        }

        [ValidateAntiForgeryToken]
        [EnableRateLimiting("upload")]
        public async Task<IActionResult> OnPostAsync()
        {
            // Check authorization
            if (_config.WebPortal.RequireAuthentication && !_authService.IsUserAllowed(User))
            {
                _logger.LogWarning("Unauthorized upload attempt by {User}", User.Identity?.Name);
                return RedirectToPage("/NotAuthorized");
            }

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
            UploadViewModel.ShowTransferOption = _config.WebPortal.EnableUploadToTransfer;
            UploadViewModel.SiteName = _config.Branding.SiteName;
            UploadViewModel.ContactEmail = _config.Branding.SupportEmail;
            UploadViewModel.MaxFileSizeBytes = _config.Security.MaxUploadSizeBytes;
        }
    }
}

