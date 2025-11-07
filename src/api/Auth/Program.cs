using Auth;
using Auth.Entities;
using Auth.Entities.DbSchema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddRazorPages();

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
    // register OpenIddict entity sets
    options.UseOpenIddict();
});

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    //options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequiredLength = 4;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Authentication (cookie for Razor)
builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.LoginPath = "/Auth/SignIn";
    opts.LogoutPath = "/Auth/Logout";
});

// OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<AppDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetUserInfoEndpointUris("/connect/userinfo")
               .SetIntrospectionEndpointUris("/connect/introspect");

        // Supported flows
        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange(); // PKCE

        options.AllowRefreshTokenFlow();

        // Use ephemeral/dev keys or configure a real certificate
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // Register scopes
        options.RegisterScopes("api", "email", "profile", "openid", "offline_access");

        // ASP.NET Core integration
        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        // Jika ingin memakai validation middleware di resource server
        options.UseLocalServer();
        options.UseAspNetCore();
    });

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapRazorPages();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
dbContext.Database.Migrate();

Seeder.SeedOpenIddictAsync(app.Services).GetAwaiter().GetResult();
app.Run();


