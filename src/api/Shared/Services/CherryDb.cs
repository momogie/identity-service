using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Shared;

public class CherryDb(IConfiguration configuration)
{
    protected bool DisableModifyDB { get; set; }

    protected SqlConnection SqlConnection { get; set; } = new SqlConnection(configuration.GetConnectionString("CherryDb"));

    public IEnumerable<T> Query<T>(string sql) where T : class
    {
        CheckDisabledQuery(sql);

        if (SqlConnection.State != System.Data.ConnectionState.Open)
            SqlConnection.Open();

        return SqlConnection.Query<T>(sql);
    }

    public T QueryFirst<T>(string sql) where T : class
    {
        CheckDisabledQuery(sql);

        if (SqlConnection.State != System.Data.ConnectionState.Open)
            SqlConnection.Open();

        return SqlConnection.QueryFirst<T>(sql);
    }

    public DataTable Result(string sql)
    {
        CheckDisabledQuery(sql);

        if (SqlConnection.State != System.Data.ConnectionState.Open)
            SqlConnection.Open();

        var cmd = new SqlCommand(sql, SqlConnection);
        var sqlDR = cmd.ExecuteReader();
        cmd.Dispose();

        var dt = new DataTable();
        dt.Load(sqlDR);
        sqlDR.Close();
        _ = sqlDR.DisposeAsync();

        return dt;
    }

    private void CheckDisabledQuery(string sql)
    {
        if (!DisableModifyDB) return;
        if (sql.Contains("update", StringComparison.OrdinalIgnoreCase))
            throw new Exception("The query contain text:'update' where is disabled.");

        if (sql.Contains("delete", StringComparison.OrdinalIgnoreCase))
            throw new Exception("The query contain text:'update' where is delete.");

        if (sql.Contains("truncate", StringComparison.OrdinalIgnoreCase))
            throw new Exception("The query contain text:'update' where is truncate.");

        if (sql.Contains("drop", StringComparison.OrdinalIgnoreCase))
            throw new Exception("The query contain text:'update' where is drop.");
    }

}
