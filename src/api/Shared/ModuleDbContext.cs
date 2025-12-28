using ClosedXML.Parser;
using Dapper;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using PluralizeService.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared;

public interface IModuleDbContext
{
    DbView Views { get; }
    string Schema { get; }
    //static Dictionary<string, (string schema, Type type)> ViewList { get; }
    public void RegisterView(string schema, Type type);
}

public abstract class ModuleDbContext : DbContext
{
    public virtual string Schema { get; }

    public DbView Views => new(this, Schema);

    private readonly IDbContextConnectionProvider _connectionProvider;

    public static Dictionary<string, (string schema, Type type)> ViewList = new();

    protected IDbConnection Connection => Database.GetDbConnection();

    public string ConnectionString { get; set; }

    public ModuleDbContext(DbContextOptions options, IDbContextConnectionProvider connectionProvider)
        : base(options)
    {
        _connectionProvider = connectionProvider;
    }

    protected ModuleDbContext(DbContextOptions options) : base(options)
    {
    }

    public ModuleDbContext()
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (!string.IsNullOrWhiteSpace(Schema))
        {
            modelBuilder.HasDefaultSchema(Schema);
            modelBuilder.Ignore<__ReparationHistory>();
        }

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }

    public void SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
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

public class ModuleDbContext<T> : ModuleDbContext, IModuleDbContext where T : DbContext
{

    public long WorkspaceId { get; set; }

    public void RegisterView(string schema, Type type)
    {
        if (ViewList.ContainsKey(type.Name))
            throw new Exception($"View type with name {type.Name} is already registered");

        ViewList.Add(type.Name, (schema, type));
    }


    protected ModuleDbContext(DbContextOptions options) : base(options)
    {
    }

    public ModuleDbContext()
    {

    }

    //public static IModuleDbContext CreateInstance<T>(string connectionString) where T : ModuleDbContext
    //{
    //    var opt = new DbContextOptions<T>
    //    {

    //    };
    //    var db = Activator.CreateInstance(typeof(T)) as ModuleDbContext;
    //    db.SetConnectionString(connectionString);
    //    return db;
    //}

    private readonly IDbContextConnectionProvider _connectionProvider;

    public ModuleDbContext(DbContextOptions options, IDbContextConnectionProvider connectionProvider)
        : base(options)
    {
        _connectionProvider = connectionProvider;
    }

    public ModuleDbContext(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public void SetWorkspaceId(long id)
    {
        WorkspaceId = id;
    }

    public void Migrate()
    {
        Database.Migrate();
        InitializeViews();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            if(!string.IsNullOrWhiteSpace(ConnectionString))
            {
                optionsBuilder.UseSqlServer(ConnectionString);
            }
            else
            {
                var connectionString = _connectionProvider.GetConnectionString();
                optionsBuilder.UseSqlServer(connectionString);
            }
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<EpochDateTime>()
            .HaveConversion<EFEpochDateTimeConverter>();
    }

    public IEnumerable<TA> GetList<TA>(string sql, object param = null)
    {
        return Connection.Query<TA>(sql, param, Database.CurrentTransaction?.GetDbTransaction());
    }

#pragma warning disable IDE1006 // Naming Styles
    public DbSet<__ReparationHistory> __ReparationHistories { get; set; }
#pragma warning restore IDE1006 // Naming Styles

    public async Task<TA> FirstOrDefaultAsync<TA>(string sql, object param = null)
    {
        return await Connection.QueryFirstOrDefaultAsync<TA>(sql, param, Database.CurrentTransaction?.GetDbTransaction());
    }

    public TA FirstOrDefault<TA>(string sql, object param = null)
    {
        return Connection.QueryFirstOrDefault<TA>(sql, param, Database.CurrentTransaction?.GetDbTransaction());
    }

    public TA ExecuteScalar<TA>(string sql, object param = null)
    {
        return Connection.ExecuteScalar<TA>(sql, param, Database.CurrentTransaction?.GetDbTransaction());
    }

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
    public DataTable List(string sql, object param = null)
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
    {
        var reader = Connection.ExecuteReader(sql, param, Database.CurrentTransaction?.GetDbTransaction());
        var dt = new DataTable("DataView");
        dt.Load(reader);
        return dt;
    }

    public IEnumerable<TA> ExecuteProcedure<TA>(string procedureName, object param)
    {
        return Connection.Query<TA>(procedureName, param, commandType: CommandType.StoredProcedure);
    }

    public class DbContextFactory : IDesignTimeDbContextFactory<T> 
    {
        public T CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<T>();
            optionsBuilder.UseSqlServer("*");

            return Activator.CreateInstance<T>();
        }
    }
}

#pragma warning disable IDE1006 // Naming Styles
public class __ReparationHistory
#pragma warning restore IDE1006 // Naming Styles
{
    [Key]
    [MaxLength(1000)]
    public string ReparationId { get; set; }

    public DateTimeOffset Timestamps { get; set; }
}

public interface IDbContextConnectionProvider
{
    string GetConnectionString();
}

public class HeaderDbContextConnectionProvider : IDbContextConnectionProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public HeaderDbContextConnectionProvider(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public string GetConnectionString()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null && context.Request.Headers.ContainsKey("WorkspaceId"))
        {
            var workspaceId = context.Request.Headers.ContainsKey("WorkspaceId") ? context.Request.Headers["workspaceid"].ToString() : context.Request.Query["workspace"].ToString();
            var connectionString = _configuration.GetConnectionString("AppDb");

            if (!string.IsNullOrEmpty(connectionString))
            {
                return connectionString.Replace("{ClientId}", workspaceId.PadLeft(10, '0') + "");
            }
        }
        return "";
        //return _configuration.GetConnectionString("Default") ?? throw new Exception("Default connection string is missing!");
    }
}
