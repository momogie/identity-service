using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;

namespace Auth;

public class Seeder
{
    public static async Task SeedOpenIddictAsync(IServiceProvider provider)
    {
        //var manager = provider.GetRequiredService<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>>();

        //if (await manager.FindByClientIdAsync("spa-client") == null)
        //{
        //    await manager.CreateAsync(new OpenIddictApplicationDescriptor
        //    {
        //        ClientId = "spa-client",
        //        DisplayName = "SPA Client",
        //        RedirectUris = { new Uri("https://localhost:3000/callback") },
        //        PostLogoutRedirectUris = { new Uri("https://localhost:3000/") },
        //        Permissions =
        //        {
        //            OpenIddictConstants.Permissions.Endpoints.Authorization,
        //            OpenIddictConstants.Permissions.Endpoints.Token,
        //            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
        //            OpenIddictConstants.Permissions.ResponseTypes.Code,
        //            OpenIddictConstants.Permissions.Scopes.Email,
        //            OpenIddictConstants.Permissions.Scopes.Profile,
        //        }
        //    });
        //}
    }
}
