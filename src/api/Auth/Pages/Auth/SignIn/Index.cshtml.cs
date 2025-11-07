using Auth.Entities.DbSchema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace Auth.Pages.Auth.SignIn;

public class IndexModel : PageModel
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly IOpenIddictScopeManager _scopeManager;

    public IndexModel(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IOpenIddictScopeManager scopeManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _scopeManager = scopeManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ReturnUrl ??= Url.Content("~/");
        if (User?.Identity?.IsAuthenticated == true)
            return Redirect(ReturnUrl);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            ErrorMessage = "Invalid login attempt.";
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, false, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ErrorMessage = "Invalid login attempt.";
            return Page();
        }

        // Jika login berasal dari authorization request (OpenIddict)
        if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/connect/authorize"))
        {
            return Redirect(returnUrl);
        }

        // Login biasa
        return LocalRedirect(ReturnUrl);
    }
}
