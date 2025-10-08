using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ZLFileRelay.WebPortal.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        // Redirect to Upload page
        return RedirectToPage("/Upload");
    }
}
