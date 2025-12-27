using Identity.Entities.DbSchema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.Entities;

public class AppDbContext : IdentityDbContext<User, Role, string,
        UserClaim, UserRole, UserLogin,
        RoleClaim, UserToken>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DbServer> DbServers { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public override DbSet<Role> Roles { get; set; }
    public override DbSet<RoleClaim> RoleClaims { get; set; }
    public override DbSet<User> Users { get; set; }
    public override DbSet<UserClaim> UserClaims { get; set; }
    public override DbSet<UserLogin> UserLogins { get; set; }
    public override DbSet<UserRole> UserRoles { get; set; }
    public override DbSet<UserToken> UserTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Users
        builder.Entity<User>().ToTable("Users");
        builder.Entity<Role>().ToTable("Roles");
        builder.Entity<UserRole>().ToTable("UserRoles");
        builder.Entity<UserClaim>().ToTable("UserClaims");
        builder.Entity<RoleClaim>().ToTable("RoleClaims");
        builder.Entity<UserLogin>().ToTable("UserLogins");
        builder.Entity<UserToken>().ToTable("UserTokens");

        // Refresh token
        builder.Entity<RefreshToken>().ToTable("RefreshTokens");
    }
}
