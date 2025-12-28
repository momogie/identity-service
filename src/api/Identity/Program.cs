global using Identity.Entities;
global using Identity.Entities.DbSchema;
global using Identity.Entities.Views;
global using Shared;
global using System.ComponentModel.DataAnnotations;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddHttpContextAccessor();
builder.Services.AddOpenApi();
builder.Services.AddDbContextPool<AppDbContext>(p => p.UseSqlServer(config.GetConnectionString("default")));
//builder.Services.AddIdentity<User, Role>(opt =>
//{
//    opt.User.RequireUniqueEmail = true;
//})
//.AddEntityFrameworkStores<AppDbContext>()
//.AddDefaultTokenProviders();

// JWT
builder.Services
    .AddAuthentication("Cookies")
    .AddCookie(options =>
    {
        options.Cookie.Name = config["Auth:CookieName"];
        options.LoginPath = "/auth/signin";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(Convert.ToInt32(config["Auth:ExpireTimeSpanMinute"]));
        options.SlidingExpiration = true;

        //options.Cookie.SameSite = SameSiteMode.None;
        //options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
    });
    //.AddScheme("Bearer")
    //.AddJwtBearer("Bearer", opt =>
    //{
    //    opt.TokenValidationParameters = new()
    //    {
    //        ValidateIssuer = true,
    //        ValidateAudience = true,
    //        ValidateIssuerSigningKey = true,
    //        ValidIssuer = "auth-server",
    //        ValidAudience = "auth-client",
    //        IssuerSigningKey = new SymmetricSecurityKey(
    //            System.Text.Encoding.UTF8.GetBytes("SUPER_SECRET_KEY_HERE_32_CHARS"))
    //    };
    //});

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddRazorPages();

#if DEBUG
builder.Services.AddCors(confg =>
                confg.AddPolicy("AllowAll",
                    p => p.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3002")
                        .AllowCredentials()
                        .AllowAnyMethod()
                        .AllowAnyHeader()));
#endif
var app = builder.Build();

app.UseCors("AllowAll");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

db.Database.Migrate();
db.InitializeViews();

app.MapCommandHandlers(typeof(AppDbContext).Assembly);
app.UseAuthentication();
app.UseAuthorization();
//app.UseHttpsRedirection();
app.MapRazorPages();
app.MapControllers();

app.MapGet("/scheme", (IAuthenticationSchemeProvider service) =>
{
    var schemes = service.GetAllSchemesAsync().Result;
    return schemes.Select(p => new
    {
        p.Name,
        p.DisplayName,
    });
});
app.Run();