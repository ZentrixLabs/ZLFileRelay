using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.WebPortal.Pages;

[AllowAnonymous] // Landing page is public - no authentication required
public class IndexModel : PageModel
{
    private readonly IOptionsMonitor<ZLFileRelayConfiguration> _configMonitor;
    private readonly ILogger<IndexModel> _logger;

    public string SiteName { get; private set; } = string.Empty;
    public string ContactEmail { get; private set; } = string.Empty;
    public bool ShowEntraId { get; private set; }
    public bool ShowLocalAccounts { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public IndexModel(
        IOptionsMonitor<ZLFileRelayConfiguration> configMonitor,
        ILogger<IndexModel> logger)
    {
        _configMonitor = configMonitor;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var config = _configMonitor.CurrentValue;
        SiteName = config.Branding.SiteName;
        ContactEmail = config.Branding.SupportEmail;
        ShowEntraId = config.WebPortal.Authentication.EnableEntraId && 
                     !string.IsNullOrEmpty(config.WebPortal.Authentication.EntraIdTenantId);
        ShowLocalAccounts = config.WebPortal.Authentication.EnableLocalAccounts;
        
        // Check if user is authenticated with ANY scheme (Identity.Application OR AzureAD)
        var identityAuth = await HttpContext.AuthenticateAsync(Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);
        var azureAuth = await HttpContext.AuthenticateAsync("AzureAD");
        
        IsAuthenticated = (identityAuth?.Succeeded ?? false) || (azureAuth?.Succeeded ?? false);
        
        _logger.LogInformation("Index page: Identity.Application authenticated={IdentityAuth}, AzureAD authenticated={AzureAuth}, IsAuthenticated={IsAuth}", 
            identityAuth?.Succeeded, azureAuth?.Succeeded, IsAuthenticated);
        
        // Redirect authenticated users directly to upload page
        if (IsAuthenticated)
        {
            _logger.LogInformation("Redirecting authenticated user to /Upload");
            return RedirectToPage("/Upload");
        }
        
        return Page();
    }
}
