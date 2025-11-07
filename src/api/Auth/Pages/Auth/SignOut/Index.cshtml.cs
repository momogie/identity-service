using Auth.Entities.DbSchema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Auth.Pages.Auth.SignOut
{
    public class IndexModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;

        public IndexModel(SignInManager<User> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            await _signInManager.SignOutAsync();

            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }

            return Redirect("/auth/signin");
        }
    }
}
