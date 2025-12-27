using Hangfire;
//using Logger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Shared;

public static class ServiceCollectionExtensions
{
    public static void UseShared(this WebApplication app)
    {
        //app.UseWsLogger();
    }

    public static IServiceCollection AddShared(this IServiceCollection services, IConfiguration config)
    {
        // Configure JSON options to use PascalCase
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = null;
        });

        services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = null;
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.SerializerOptions.Converters.Add(new EpochDateTimeConverter());
        });
        services.AddControllers().ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);
        services.AddHangfire(p => p.SetDataCompatibilityLevel(CompatibilityLevel.Version_170));
        services.AddHangfireServer();
        services.AddTransient<IDbContextConnectionProvider, HeaderDbContextConnectionProvider>();

        var cnnStr = config.GetConnectionString("Hangfire");

        //dbContext.Database.ExecuteSqlRaw($"IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{hfDbName}') BEGIN CREATE DATABASE [{hfDbName}] END");
        GlobalConfiguration.Configuration
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(cnnStr);

        //services.AddWsLogger(config);

        services.AddScoped<CronManager>();
        services.AddSingleton<EventBus>();
        services.AddFileStorage(config);

        return services;
    }

    public static IServiceCollection AddDatabaseContext<T>(this IServiceCollection services) where T : ModuleDbContext<T>
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        //services.AddDbContext<IModuleDbContext, T>();
        services.AddDbContext<T>((serviceProvider, builder) =>
        {
            HttpContext context = serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            if (context == null)
                return;

            IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();

            var workspaceId = context.Request.Headers.ContainsKey("WorkspaceId") ? context.Request.Headers["workspaceid"].ToString() : context.Request.Query["workspace"].ToString();
            var connectionString = configuration.GetConnectionString("appDb").Replace("{ClientId}", workspaceId.PadLeft(10, '0'));
            builder.UseSqlServer(connectionString);

            builder.UseLoggerFactory(LoggerFactory.Create(builder =>
            {
                builder.AddFilter(_ => false);
            }));
            builder.EnableDetailedErrors(false);
            builder.EnableSensitiveDataLogging(false);
        });
        //var connectionString = config.GetConnectionString("Default");
        //services.AddMSSQL<T>(connectionString);

        //HttpContext context = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
        //if (context == null)
        //    return services;

        //var workspaceId = context.Request.Headers.ContainsKey("WorkspaceId") ? context.Request.Headers["workspaceid"].ToString() : context.Request.Query["workspace"].ToString();
        //var connectionString = config.GetConnectionString("appDb").Replace("{ClientId}", workspaceId.PadLeft(10, '0') + ".Logs");
        //services.AddMSSQL<T>(connectionString);

        //builder.UseLoggerFactory(LoggerFactory.Create(builder =>
        //{
        //    builder.AddFilter(_ => false);
        //}));
        //builder.EnableDetailedErrors(false);
        //builder.EnableSensitiveDataLogging(false);

        return services;
    }

    public static IServiceCollection AddMessageHandlers(this IServiceCollection services, Assembly assembly)
    {
        var list = assembly.ExportedTypes.Where(p => p.BaseType.IsGenericType && p.BaseType.GetGenericTypeDefinition() == typeof(MessageHandler<>));
        foreach (var r in list)
            services.AddScoped(r);

        return services;
    }

    //private static IServiceCollection AddMSSQL<T>(this IServiceCollection services, string connectionString) where T : ModuleDbContext
    //{
    //    services.AddDbContext<T>(m => m.UseSqlServer(connectionString, e => e.MigrationsAssembly(typeof(T).Assembly.FullName)));
    //    using var scope = services.BuildServiceProvider().CreateScope();
    //    var dbContext = scope.ServiceProvider.GetRequiredService(typeof(T)) as T;
    //    //dbContext.Database.Migrate();
    //    //dbContext.InitializeViews();
    //    return services;
    //}
}