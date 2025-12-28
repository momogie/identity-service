using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Identity.Pages;

[Authorize]
public class IndexModel : PageModel
{
    public void OnGet()
    {
        var user = User.Identity.IsAuthenticated;
    }
}
