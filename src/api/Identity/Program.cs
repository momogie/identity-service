using Identity.Entities;
using Identity.Entities.DbSchema;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContextPool<AppDbContext>(p => p.UseSqlServer(config.GetConnectionString("default")));
builder.Services.AddIdentity<User, Role>(opt =>
{
    opt.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// JWT
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = config["Auth:CookieName"];
        options.LoginPath = "/auth/signin";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(Convert.ToInt32(config["Auth:ExpireTimeSpanMinute"]));
        options.SlidingExpiration = true;

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                // API → jangan redirect
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                // Web → redirect normal
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
        //options.Events.OnRedirectToLogin = context =>
        //{
        //    //context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        //    context.RedirectUri = "/auth/signin";
        //    return Task.CompletedTask;
        //};
        //options.Events.OnRedirectToAccessDenied = context =>
        //{
        //    context.Response.StatusCode = StatusCodes.Status403Forbidden;
        //    return Task.CompletedTask;
        //};
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

//// Add services to the container.
//services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    //.AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", options => { })
//    .AddCookie(options =>
//    {
//        options.Cookie.Name = configuration["Auth:CookieName"];
//        options.Events.OnRedirectToLogin = context =>
//        {
//            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
//            return Task.CompletedTask;
//        };
//        options.Events.OnRedirectToAccessDenied = context =>
//        {
//            context.Response.StatusCode = StatusCodes.Status403Forbidden;
//            return Task.CompletedTask;
//        };
//    });
//services.AddAuthorization();
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

db.Database.Migrate();

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapRazorPages();
app.MapControllers();

app.Run();