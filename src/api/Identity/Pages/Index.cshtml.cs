using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Identity.Pages;

[Authorize(AuthenticationSchemes = "Cookies")]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
