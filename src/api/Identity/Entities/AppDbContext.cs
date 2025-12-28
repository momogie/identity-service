using Dapper;
using Identity.Entities.DbSchema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PluralizeService.Core;
using System.Data;
using System.Text;

namespace Identity.Entities;

public class AppDbContext : IdentityDbContext<User, Role, string,
        UserClaim, UserRole, UserLogin,
        RoleClaim, UserToken>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Application> Applications { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<ClientTenant> ClientTenants { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantApplication> TenantApplications { get; set; }
    public DbSet<TenantUser> TenantUsers { get; set; }
    public DbSet<DbServer> DbServers { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public override DbSet<Role> Roles { get; set; }
    public override DbSet<RoleClaim> RoleClaims { get; set; }
    public override DbSet<User> Users { get; set; }
    public override DbSet<UserClaim> UserClaims { get; set; }
    public override DbSet<UserLogin> UserLogins { get; set; }
    public override DbSet<UserRole> UserRoles { get; set; }
    public override DbSet<UserToken> UserTokens { get; set; }

    public DbView Views => new(this, "dbo");

    public static Dictionary<string, (string schema, Type type)> ViewList = new();

    protected IDbConnection Connection => Database.GetDbConnection();

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

    public void InitializeViews()
    {
        var name = GetType().Namespace;
        var nameSpace = GetType().Namespace.Split(".")[1];
        foreach (Type r in GetType().Assembly.GetExportedTypes().Where(p => p.GetCustomAttributes(true).Any(c => c.GetType() == typeof(SqlView))))
        {
            string dir = System.IO.Path.GetDirectoryName(AppContext.BaseDirectory);
            string sqlPath = r.Namespace.Replace(".Entities.Views", "/Entities/Views").Replace(".Entities.ExportViews", "/Entities/ExportViews");
            string fileNames = string.Concat(dir, "/", sqlPath, "/", r.Name, ".sql");
            if (!System.IO.File.Exists(fileNames))
                throw new Exception($"The sql file ${fileNames} does not exist.");

            var sqlLines = System.IO.File.ReadAllLines(fileNames, Encoding.UTF8);
            var sql = string.Join(Environment.NewLine, sqlLines);

            Views.Execute($"CREATE OR ALTER VIEW {GetViewName(nameSpace, r.Name)} AS {sql}");
        }
    }

    public string GetViewName(string nameSpace, string typeName)
    {
        return $"{nameSpace}_{PluralizationProvider.Pluralize(typeName)}";
#pragma warning disable CS0162 // Unreachable code detected
        if (ViewList.TryGetValue(typeName, out (string schema, Type type) value))
        {
            var (schema, type) = value;
            return $"{schema}_{PluralizationProvider.Pluralize(type.Name)}";
        }
#pragma warning restore CS0162 // Unreachable code detected
        throw new Exception($"View type with name {typeName} is not registered");
    }

    public IEnumerable<TA> List<TA>(string sql, object param = null)
    {
        return Connection.Query<TA>(sql, param, Database.CurrentTransaction?.GetDbTransaction());
    }

    public DataTable List(string sql, object param = null)
    {
        var reader = Connection.ExecuteReader(sql, param, Database.CurrentTransaction?.GetDbTransaction());
        var dt = new DataTable("DataView");
        dt.Load(reader);
        return dt;
    }
}
