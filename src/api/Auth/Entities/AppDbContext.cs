using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;
using Auth.Entities.DbSchema;
using Microsoft.EntityFrameworkCore.Design;

namespace Auth.Entities;

public class AppDbContext : IdentityDbContext<User, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // OpenIddict entity mappings
    public DbSet<OpenIddictEntityFrameworkCoreApplication> Applications { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreAuthorization> Authorizations { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreScope> Scopes { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreToken> Tokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 👉 Rename tabel Identity default
        builder.Entity<User>(b =>
        {
            b.ToTable("Users");
        });

        builder.Entity<IdentityRole>(b =>
        {
            b.ToTable("Roles");
        });

        builder.Entity<IdentityUserRole<string>>(b =>
        {
            b.ToTable("UserRoles");
        });

        builder.Entity<IdentityUserClaim<string>>(b =>
        {
            b.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<string>>(b =>
        {
            b.ToTable("UserLogins");
        });

        builder.Entity<IdentityUserToken<string>>(b =>
        {
            b.ToTable("UserTokens");
        });

        builder.Entity<IdentityRoleClaim<string>>(b =>
        {
            b.ToTable("RoleClaims");
        });
        builder.UseOpenIddict();
    }
}
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=s;User Id=;Password=;Integrated Security=true;TrustServerCertificate=True");

        return new AppDbContext(optionsBuilder.Options);
    }
}