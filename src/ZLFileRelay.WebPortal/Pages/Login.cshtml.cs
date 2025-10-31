using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ZLFileRelay.Core.Models;
using ZLFileRelay.WebPortal.Data;

namespace ZLFileRelay.WebPortal.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IOptionsMonitor<ZLFileRelayConfiguration> _configMonitor;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            IOptionsMonitor<ZLFileRelayConfiguration> configMonitor,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _configMonitor = configMonitor;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public bool ShowEntraId { get; set; }
        public bool ShowLocalAccounts { get; set; }
        public string SiteName { get; set; } = "ZL File Relay";

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            var config = _configMonitor.CurrentValue;
            ShowEntraId = config.WebPortal.Authentication.EnableEntraId && 
                         !string.IsNullOrEmpty(config.WebPortal.Authentication.EntraIdTenantId);
            ShowLocalAccounts = config.WebPortal.Authentication.EnableLocalAccounts;
            SiteName = config.Branding.SiteName ?? "ZL File Relay";
            ReturnUrl = returnUrl ?? Url.Content("~/");

            _logger.LogInformation("OnGetAsync called with returnUrl={ReturnUrl}, Request.QueryString={QueryString}", 
                returnUrl, Request.QueryString);

            // Check if this is an external auth callback - if so, don't sign out
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info != null)
            {
                _logger.LogInformation("External login info found in OnGetAsync, should not have been called - returning without processing");
                return;
            }

            // Clear existing external cookie
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            var config = _configMonitor.CurrentValue;
            ShowEntraId = config.WebPortal.Authentication.EnableEntraId && 
                         !string.IsNullOrEmpty(config.WebPortal.Authentication.EntraIdTenantId);
            ShowLocalAccounts = config.WebPortal.Authentication.EnableLocalAccounts;
            SiteName = config.Branding.SiteName ?? "ZL File Relay";
            ReturnUrl = returnUrl;

            if (!ShowLocalAccounts)
            {
                ErrorMessage = "Local account sign-in is not enabled.";
                return Page();
            }

            if (ModelState.IsValid)
            {
                // Sign in with local account
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("✅ User {Email} logged in successfully", Input.Email);
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("⚠️ User {Email} account locked out", Input.Email);
                    ErrorMessage = "Your account has been locked due to multiple failed login attempts. Please try again later.";
                    return Page();
                }
                else
                {
                    _logger.LogWarning("⚠️ Invalid login attempt for {Email}", Input.Email);
                    ErrorMessage = "Invalid email or password.";
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        public IActionResult OnPostMicrosoftSignIn(string? returnUrl = null)
        {
            var config = _configMonitor.CurrentValue;
            if (!config.WebPortal.Authentication.EnableEntraId)
            {
                return BadRequest("Microsoft sign-in is not enabled.");
            }

            returnUrl ??= Url.Content("~/");
            
            // Use ConfigureExternalAuthenticationProperties - this sets up the callback URL properly
            // Then challenge with OpenIdConnect scheme, not the cookie scheme
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("AzureAD", returnUrl);
            
            _logger.LogInformation("Redirect URL for external auth will be determined by OIDC middleware, returnUrl: {ReturnUrl}", returnUrl);
            
            // Challenge with OpenIdConnect scheme - this triggers the OIDC flow to Azure
            return Challenge(properties, Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> OnGetMicrosoftCallbackAsync(string? returnUrl = null, string? remoteError = null)
        {
            _logger.LogInformation("OnGetMicrosoftCallbackAsync called with returnUrl={ReturnUrl}, remoteError={RemoteError}", 
                returnUrl, remoteError);
            
            returnUrl ??= Url.Content("~/");
            
            if (remoteError != null)
            {
                _logger.LogWarning("Remote error during authentication: {RemoteError}", remoteError);
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogError("External login info is null - authentication may have failed");
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            
            _logger.LogInformation("External login info retrieved for provider: {Provider}, Key: {ProviderKey}", 
                info.LoginProvider, info.ProviderKey);

            // Sign in the user with this external login provider if the user already has a login
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("✅ User logged in with {Provider} provider", info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
            
            if (result.IsLockedOut)
            {
                _logger.LogWarning("⚠️ User account locked out");
                ErrorMessage = "Your account has been locked.";
                return RedirectToPage("./Login");
            }
            else
            {
                // User doesn't have a local account yet, create one
                var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ??
                           info.Principal.FindFirst("preferred_username")?.Value ??
                           info.Principal.FindFirst("emails")?.Value;
                
                if (string.IsNullOrEmpty(email))
                {
                    ErrorMessage = "Could not retrieve email from Microsoft account.";
                    return RedirectToPage("./Login");
                }

                var user = new ApplicationUser 
                { 
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true, // Entra ID emails are already confirmed
                    IsApproved = !_configMonitor.CurrentValue.WebPortal.Authentication.RequireApproval, // Auto-approve Entra ID users if not requiring approval
                    RegistrationDate = DateTime.UtcNow
                };
                
                var createResult = await _signInManager.UserManager.CreateAsync(user);
                if (createResult.Succeeded)
                {
                    createResult = await _signInManager.UserManager.AddLoginAsync(user, info);
                    if (createResult.Succeeded)
                    {
                        _logger.LogInformation("✅ User created an account using {Provider} provider", info.LoginProvider);
                        
                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                        return LocalRedirect(returnUrl);
                    }
                }
                
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }
    }
}

