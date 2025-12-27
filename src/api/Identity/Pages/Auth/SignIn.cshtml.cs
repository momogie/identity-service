using Identity.Entities;
using Identity.Entities.DbSchema;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Pages.Auth;

public class SignInModel(IConfiguration configuration, AppDbContext appDb) : PageModel
{
    [Required]
    [BindProperty]
    [MaxLength(255)]
    public string Username { get; set; }

    [Required]
    [BindProperty]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; }

    [BindProperty]
    public bool IsRememberMe { get; set; }

    public bool IsInvalidCredential { get; set; }

    public string ReturnUrl { get; set; }

    public IActionResult OnGet([FromQuery] string returnUrl)
    {
        ReturnUrl = configuration["App:AppUrl"];
        if (!string.IsNullOrWhiteSpace(returnUrl))
            ReturnUrl = returnUrl;

        if (User.Identity.IsAuthenticated)
            return Redirect(configuration["App:AppUrl"]);

        return Page();
    }

    public static string SHA256Hash(string InputText)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(InputText)));
    }

    public async Task<IActionResult> OnPost([FromQuery] string returnUrl)
    {
        ReturnUrl = configuration["App:AppUrl"];
        if (!string.IsNullOrWhiteSpace(returnUrl))
            ReturnUrl = returnUrl;

        var user = await appDb.Users.FirstOrDefaultAsync(p => p.UserName == Username && p.PasswordHash == SHA256Hash(Password));

        if (!appDb.Users.Any())
        {
            user = new User
            {
                Id =Guid.NewGuid().ToString(),
                UserName = Username,
                PasswordHash = SHA256Hash(Password),
                EmailConfirmed = true,
                Email =  Username,
            };
            appDb.Users.Add(user);
            appDb.SaveChanges();
        }

        if (user == null)
        {
            IsInvalidCredential = true;

            return Page();
        }

        // Create user claims
        var claims = new List<Claim>
        {
            new("UserId", user.Id.ToString()),
            new("Email", user.Email ?? "-"),
            new("UserName", Username),
            //new("Name", user.Name),
        };

        // Create identity and sign in
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true, // Ensure cookie persists
            ExpiresUtc = IsRememberMe ? 
                DateTimeOffset.UtcNow.AddMinutes(Convert.ToInt32(configuration["Auth:RememberMeExpireTimeSpanMinute"])) 
                : DateTimeOffset.UtcNow.AddMinutes(Convert.ToInt32(configuration["Auth:ExpireTimeSpanMinute"])) // Set expiration if needed
        }).Wait();

        // Redirect to another page after successful login
        return Redirect(ReturnUrl);
    }
}