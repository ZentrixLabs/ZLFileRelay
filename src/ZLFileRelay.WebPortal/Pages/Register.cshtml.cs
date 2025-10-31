using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ZLFileRelay.Core.Models;
using ZLFileRelay.WebPortal.Data;

namespace ZLFileRelay.WebPortal.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOptionsMonitor<ZLFileRelayConfiguration> _configMonitor;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptionsMonitor<ZLFileRelayConfiguration> configMonitor,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configMonitor = configMonitor;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? StatusMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public bool RegistrationEnabled { get; set; }
        public bool RequireApproval { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet()
        {
            var config = _configMonitor.CurrentValue;
            RegistrationEnabled = config.WebPortal.Authentication.EnableLocalAccounts;
            RequireApproval = config.WebPortal.Authentication.RequireApproval;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var config = _configMonitor.CurrentValue;
            RegistrationEnabled = config.WebPortal.Authentication.EnableLocalAccounts;
            RequireApproval = config.WebPortal.Authentication.RequireApproval;

            if (!RegistrationEnabled)
            {
                ErrorMessage = "Local account registration is not enabled.";
                return Page();
            }

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser 
                { 
                    UserName = Input.Email,
                    Email = Input.Email,
                    IsApproved = !RequireApproval, // Auto-approve if not requiring approval
                    RegistrationDate = DateTime.UtcNow
                };
                
                var result = await _userManager.CreateAsync(user, Input.Password);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("✅ User {Email} created a new account", Input.Email);

                    // Assign default Uploader role
                    await _userManager.AddToRoleAsync(user, "Uploader");

                    if (RequireApproval)
                    {
                        StatusMessage = "Account created successfully! Your account is pending approval from an administrator. You will be able to upload files once approved.";
                        _logger.LogInformation("📧 User {Email} requires admin approval", Input.Email);
                        return Page();
                    }
                    else
                    {
                        // Auto-sign in if no approval required
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("✅ User {Email} automatically signed in after registration", Input.Email);
                        return RedirectToPage("/Upload");
                    }
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}

