using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using System.Reflection;
using System.Text;

namespace Shared.Cron;

public static class CronExtension
{
    public static void AddHangfire(this IServiceCollection services)
    {
        services.AddHangfire(p => p.SetDataCompatibilityLevel(CompatibilityLevel.Version_170));
        services.AddHangfireServer();
        services.AddScoped<CronManager>();
    }

    public static void AddCronJobs(this IServiceCollection services, Assembly assembly)
    {
        var list = assembly.ExportedTypes.Where(p => p.GetCustomAttribute<CronJob>() != null && p.BaseType == typeof(BaseCronJob)).ToList();
        foreach (var r in list)
            services.AddScoped(r);
    }
    public static void RunCronJobs(this WebApplication app, Assembly assembly)
    {
        using var scope = app.Services.CreateScope();
        var cronManager = scope.ServiceProvider.GetService<CronManager>();
        cronManager.RunJobs(assembly);
    }

    public static void UseHangfire(this WebApplication app)
    {
        //using var scope = app.Services.CreateScope();

        //var config = scope.ServiceProvider.GetService<IConfiguration>();
        //var dbContext = scope.ServiceProvider.GetRequiredService<IModuleDbContext>() as ModuleDbContext;

        //var hfDbName = $"{dbContext.Database.GetDbConnection().Database}.Hangfire";

        //var cnnStr = dbContext.Database.GetConnectionString().Replace(dbContext.Database.GetDbConnection().Database, hfDbName);
        //var cnnStr = config.GetConnectionString("Hangfire");

        //dbContext.Database.ExecuteSqlRaw($"IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{hfDbName}') BEGIN CREATE DATABASE [{hfDbName}] END");
        //GlobalConfiguration.Configuration
        //    .UseSimpleAssemblyNameTypeSerializer()
        //    .UseRecommendedSerializerSettings()
        //    .UseSqlServerStorage(cnnStr);


        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            //Authorization = [new BasicAuthAuthorizationFilter("momogie", "Momogie@123@")]
        });
    }

}

public class BasicAuthAuthorizationFilter(string username, string password) : IDashboardAuthorizationFilter
{
    private readonly string _username = username;
    private readonly string _password = password;

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Check if the request contains an Authorization header
        string authHeader = httpContext.Request.Headers.Authorization;
        if (authHeader != null && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            // Decode the base64 encoded credentials
            var encodedUsernamePassword = authHeader["Basic ".Length..].Trim();
            var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));

            // Split the username and password
            var parts = decodedUsernamePassword.Split(':');
            if (parts.Length == 2)
            {
                var username = parts[0];
                var password = parts[1];

                // Validate the username and password
                if (username == _username && password == _password)
                {
                    return true; // Allow access if the credentials are valid
                }
            }
        }

        // If the Authorization header is missing or invalid, prompt for authentication
        //httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire Dashboard\"";
        //httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return false; // Deny access if the credentials are invalid
    }
}

