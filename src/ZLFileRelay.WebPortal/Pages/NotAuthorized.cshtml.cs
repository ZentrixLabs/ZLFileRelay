using Microsoft.AspNetCore.Mvc.RazorPages;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.WebPortal.Pages
{
    public class NotAuthorizedModel : PageModel
    {
        private readonly ZLFileRelayConfiguration _config;

        public string ContactEmail { get; set; } = string.Empty;

        public NotAuthorizedModel(ZLFileRelayConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void OnGet()
        {
            ContactEmail = _config.Branding.SupportEmail;
        }
    }
}

