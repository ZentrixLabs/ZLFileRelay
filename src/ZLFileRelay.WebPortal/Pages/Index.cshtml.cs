using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.WebPortal.Pages;

[AllowAnonymous] // Landing page is public - no authentication required
public class IndexModel : PageModel
{
    private readonly ZLFileRelayConfiguration _config;

    public string SiteName { get; private set; } = string.Empty;
    public string ContactEmail { get; private set; } = string.Empty;
    public string? AuthenticationDomain { get; private set; }

    public IndexModel(ZLFileRelayConfiguration config)
    {
        _config = config;
    }

    public void OnGet()
    {
        SiteName = _config.Branding.SiteName;
        ContactEmail = _config.Branding.SupportEmail;
        AuthenticationDomain = _config.WebPortal.AuthenticationDomain;
    }
}
