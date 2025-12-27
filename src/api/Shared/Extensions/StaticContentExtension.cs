
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Shared;

public static class StaticContentExtension
{
    public static void UseStaticContent(this WebApplication app)
    {
        app.UseStaticFiles();

        MapHtml(app);
    }

    private static void MapHtml(WebApplication app)
    {
        string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        var htmlFiles = Directory.GetFiles(rootFolder, "*.html", SearchOption.AllDirectories);

        foreach (var file in htmlFiles)
        {
            string relativePath = Path.GetRelativePath(rootFolder, file);
            var aa = Path.GetFileNameWithoutExtension(file);

            aa = aa == "index" ? "" : aa;

            string route = "/" + Path.GetDirectoryName(relativePath)?.Replace("\\", "/") + "/" + aa;

            route = route.Replace("//", "/");
            if (route == "/index")
            {
                route = "/";
            }

            app.Map(route, () => Results.File(file, "text/html"));
        }

    }
}
