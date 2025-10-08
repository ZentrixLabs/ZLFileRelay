using Microsoft.AspNetCore.Mvc.RazorPages;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.WebPortal.Pages
{
    public class ResultModel : PageModel
    {
        public List<UploadResult> Results { get; set; } = new();
        public string Username { get; set; } = string.Empty;
        public int TotalCount => Results.Count;
        public int SuccessCount => Results.Count(r => r.Success);
        public bool AllSuccessful => Results.All(r => r.Success);

        public void OnGet()
        {
            // Load results from TempData
            if (TempData["UploadResults"] is string resultsJson)
            {
                Results = System.Text.Json.JsonSerializer.Deserialize<List<UploadResult>>(resultsJson) 
                    ?? new List<UploadResult>();
            }

            Username = TempData["Username"] as string ?? "Unknown";
        }
    }
}

