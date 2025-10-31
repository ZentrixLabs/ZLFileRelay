using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.WebPortal.Pages
{
    public class NotAuthorizedModel : PageModel
    {
        private readonly IOptionsMonitor<ZLFileRelayConfiguration> _configMonitor;

        public string ContactEmail { get; set; } = string.Empty;

        public NotAuthorizedModel(IOptionsMonitor<ZLFileRelayConfiguration> configMonitor)
        {
            _configMonitor = configMonitor ?? throw new ArgumentNullException(nameof(configMonitor));
        }

        public void OnGet()
        {
            var config = _configMonitor.CurrentValue;
            ContactEmail = config.Branding.SupportEmail;
        }
    }
}

