using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PluralizeService.Core;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Shared;

public class DbView
{
    private readonly string Schema;
    private readonly Dictionary<string, string> Sql = [];

    private IDbConnection Connection { get; set; }//=> new SqlConnection(_connectionString);

    private IDbTransaction Transaction { get; set; }

    public DbView(DbContext appDb, string schema)
    {
        Schema = schema;
        Connection = appDb.Database.GetDbConnection();
        if (appDb.Database.CurrentTransaction != null)
        {
            Transaction = appDb.Database.CurrentTransaction.GetDbTransaction();
        }
    }

    private string SqlBuilder<T>() where T : IDataTable
    {
        var vwName = typeof(T).Name;
        if (Sql.TryGetValue(vwName, out string value))
            return value;

        var nameSpace = typeof(T).Namespace.Split(".")[1];

        return Sql[vwName] = $"SELECT * FROM {nameSpace}_{PluralizationProvider.Pluralize(vwName)}";
    }

    private static object ParamValue(Type type, object propertyValue)
    {
        if (type == typeof(DateTime) && DateTime.TryParse(propertyValue.ToString(), out DateTime valDate))
            return valDate;

        if (type == typeof(DateTime?) && DateTime.TryParse(propertyValue.ToString(), out DateTime valDate2))
            return string.IsNullOrWhiteSpace(propertyValue.ToString()) ? null : valDate2;

        if (type == typeof(int) && int.TryParse(propertyValue.ToString(), out int valInt))
            return valInt;

        if (type == typeof(int?) && int.TryParse(propertyValue.ToString(), out int valInt2))
            return string.IsNullOrWhiteSpace(propertyValue.ToString()) ? null : valInt2;

        //if (type == typeof(decimal) && propertyValue.IsDecimal())
        //    return propertyValue.ToDecimal();

        //if (type == typeof(decimal?) && propertyValue.IsDecimal())
        //    return propertyValue.ToNullableDecimal();

        //if (type == typeof(bool))
        //    return propertyValue.ToBoolean();

        //if (type == typeof(bool?) && propertyValue.IsBoolean())
        //    return propertyValue.ToNullableBoolean();

        //if (type == typeof(double) && propertyValue.IsDouble())
        //    return propertyValue.ToDouble();

        //if (type == typeof(double?) && propertyValue.IsDouble())
        //    return propertyValue.ToNullableDouble();

        return propertyValue?.ToString();
    }


    #region New

    private Parameter ParameterBuilder<T>(RequestParameter parameter) where T : IDataTable
    {
        var notMappedProps = typeof(T).GetProperties().Where(p => p.GetCustomAttribute<NotMappedAttribute>() != null);

        string sql = SqlBuilder<T>();
        StringBuilder sb = new();
        sb.Append(sql);
        sb.Append(" ");
        sb.Append("WHERE 1=1 ");

        var props = typeof(T).GetProperties();

        var data = new Dictionary<string, object>();
        int i = 0;

        foreach (JsonElement filter in (parameter.Filters ?? []))
        {
            var a = WhereClause(props, ref i, filter, data);
            if (a != "")
            {
                sb.Append(" AND ");
                sb.Append(a);
            }
        }

        var filteredCountSql = sb.ToString();

        if (parameter.Sorts.Where(p => !notMappedProps.Any(c => c.Name == p.Key)).Any())
        {
            string sort = string.Join(",", parameter.Sorts.Where(p => !notMappedProps.Any(c => c.Name == p.Key)).Select(x => x.Key + " " + (x.Value.ToLower() == "desc" ? "desc" : "asc")).ToArray());
            sb.Append($" Order By {sort}");
        }

        if (parameter.Page.HasValue && parameter.Length.HasValue)
        {
            if (parameter.Length.Value > 0)
            {
                if (!parameter.Sorts.Where(p => !notMappedProps.Any(c => c.Name == p.Key)).Any())
                    sb.Append(" ORDER BY 1 ");

                int start = parameter.Page.Value == 0 ? 0 : (parameter.Page.Value - 1) * parameter.Length.Value;
                sb.Append($" OFFSET {start} ROWS FETCH NEXT {parameter.Length.Value} ROWS ONLY ");
            }
        }

        var cc = sb.ToString();
        var sqlCnt = sql.Replace("*", "COUNT(*)");
        if (!data.Any())
        {
            cc = cc.Replace("where", "");
            sqlCnt = sqlCnt.Replace("where", "");
            filteredCountSql = filteredCountSql.Replace("where", "");
        }

        if (parameter?.Fields.Length > 0)
            cc = cc.Replace("SELECT * FROM", $"SELECT {string.Join(", ", parameter.Fields.Select(p => $"[{p}]"))} FROM");


        return new Parameter { Sql = cc, SqlForCount = sqlCnt, Payload = data, SqlForFilteredCount = filteredCountSql.Replace("*", "COUNT(*)"), };
        //return new Parameter
        //{
        //    Sql = sb.ToString(),
        //    Payload = data
        //};
    }

    private string WhereClause(PropertyInfo[] props, ref int idx, JsonElement par, Dictionary<string, object> data)
    {
        //var a = par[0].GetType();
        if (par[0].ValueKind == JsonValueKind.Array)
        {
            StringBuilder sb = new StringBuilder();
            //string[] oprrr = ["OR", "AND"];

            var vals = par.EnumerateArray().ToArray();
            foreach (var r in vals)
            {
                idx++;
                if (r.ValueKind == JsonValueKind.Array)
                {
                    sb.Append($" {WhereClause(props, ref idx, r, data)} ");
                }

                if (r.ValueKind == JsonValueKind.String && (r.GetString() == "AND" || r.GetString() == "OR"))
                    sb.Append($"{r.GetString()}");
            }
            //var ws = Test(par);
            return $"({sb})";
        }
        var propName = par[0].ToString();

        var prop = props.FirstOrDefault(p => p.Name.Equals(propName, StringComparison.CurrentCultureIgnoreCase));
        if (prop == null)
            return "";

        var oprator = "=";
        object value = null;
        if (par.GetArrayLength() > 2)
        {
            oprator = par[1].ToString();
            value = par[2];
        }
        if (par.GetArrayLength() == 2)
        {
            value = par[1];
        }

        if (oprator.ToUpper() == "IS" || oprator.ToUpper() == "IS NOT")
            value = "NULL";

        if (value is JsonElement)
        {
            if (((JsonElement)value).ValueKind == JsonValueKind.Array)
            {
                if (oprator.ToUpper() == "IN")
                {
                    var d = ((JsonElement)value).EnumerateArray().ToArray();
                    if (d.Length == 0)
                        return "";
                }
            }

            if (((JsonElement)value).ValueKind == JsonValueKind.Null)
            {
                if (oprator == "=")
                    oprator = "IS";
                if (oprator == "!=")
                    oprator = "IS NOT";

                value = "NULL";

                if (oprator.ToUpper() == "IN")
                {
                    return "";
                }
            }

        }

        if (value.ToString() == "NULL")
        {
            if (oprator == "=")
                oprator = "IS";
            if (oprator == "!=")
                oprator = "IS NOT";
        }

        if (!action.Contains(oprator.ToLower()))
            return "";

        var paramKey = $"P_{propName}_{idx++}_";
        var paramValue = ConvertValue(prop.PropertyType, value);

        data.Add(paramKey, paramValue);

        var paramKey2 = paramKey;
        if (oprator.ToLower() == "in" || oprator.ToLower() == "not in")
        {
            //paramKey2 =
            List<string> a = [];

            for (int i = 1; i <= ((object[])paramValue).Length; i++)
                a.Add(paramKey2 + i);

            paramKey2 = $"({string.Join(",", a.Select(p => "@" + p))})";
        }

        if (par.GetArrayLength() == 2 || par.GetArrayLength() == 3)
        {
            if (oprator.ToUpper() == "IS" || oprator.ToUpper() == "IS NOT")
                return $"[{propName}] {oprator} {value}";

            if (oprator.ToLower() == "in" || oprator.ToLower() == "not in")
                return $"[{propName}] {oprator} {paramKey2}";

            return $"[{propName}] {oprator} @{paramKey2}";
        }

        if (oprator.ToUpper() == "IS" || oprator.ToUpper() == "IS NOT")
            return $"[{propName}] {oprator} {value}";

        if (oprator.ToLower() == "in" || oprator.ToLower() == "not in")
            return $"([{propName}] {oprator} @{paramKey2})";

        return $"([{propName}] {oprator} {paramKey2})";
    }

    private object ConvertValue(Type type, object value)
    {
        if (value is JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.String:
                    return Convert.ChangeType(je.GetString(), type);
                case JsonValueKind.Number:
                    if (type == typeof(int) || type == typeof(int?)) return je.GetInt32();
                    if (type == typeof(decimal) || type == typeof(decimal?)) return je.GetDecimal();
                    if (type == typeof(long) || type == typeof(long?)) return je.GetInt64();
                    return je.GetDouble();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Null: return null;
                case JsonValueKind.Array:
                    // ✅ Convert JSON array → string[]
                    var list = new List<string>();
                    foreach (var item in je.EnumerateArray())
                    {
                        switch (item.ValueKind)
                        {
                            case JsonValueKind.String:
                                list.Add(item.GetString());
                                break;
                            case JsonValueKind.Number:
                                list.Add(item.GetRawText()); // simpan angka sebagai string juga
                                break;
                            case JsonValueKind.True:
                                list.Add("true");
                                break;
                            case JsonValueKind.False:
                                list.Add("false");
                                break;
                            case JsonValueKind.Null:
                                list.Add("null");
                                break;
                            default:
                                list.Add(item.ToString());
                                break;
                        }
                    }
                    return list.ToArray();
            }
        }
        return Convert.ChangeType(value, type);
    }

    #endregion

    //private Parameter ParameterBuilder<T>(RequestParameter param) where T : IDataTable
    //{
    //    string sql = SqlBuilder<T>();
    //    StringBuilder sb = new();
    //    sb.Append(sql);

    //    Dictionary<string, object> payload = [];
    //    int idx = 0;

    //    var notMappedProps = typeof(T).GetProperties().Where(p => p.GetCustomAttribute<NotMappedAttribute>() != null);

    //    if (param.Filters?.Count > 0)
    //        sb.Append(" where ");

    //    int ix = 0;
    //    param.Filters?.ForEach(r =>
    //    {

    //        if (r.Length > 0)
    //        {
    //            if (r[0] == null)
    //                return;
    //        }
    //        var xx = r as object[];
    //        if (r.Length < 2)
    //            return;

    //        string propName = r[0].ToString();
    //        if (notMappedProps.Any(p => p.Name == propName))
    //            return;

    //        string aliasPropName = payload.Any(p => p.Key == $"P_{propName}") ? $"P_{propName}_{idx++}" : $"P_{propName}";
    //        string opr = r.Length == 3 ? r[1].ToString() : "=";
    //        object propertyValue = r.Length == 2 ? r[1] : r[2];

    //        var isJsonArray = propertyValue is JsonElement ? ((JsonElement)propertyValue).ValueKind == JsonValueKind.Array : false;
    //        var isArray = propertyValue is Array;
    //        var isEnumerable = propertyValue is IEnumerable;
    //        var isArrayData = isJsonArray || isArray || isEnumerable;

    //        var allowedOpr = action;

    //        if (!allowedOpr.Contains(opr))
    //            return;

    //        Type columnType = typeof(T).GetProperty(propName)?.PropertyType;

    //        if (columnType == null)
    //            return;

    //        if (ix > 0)
    //            sb.Append(" AND ");

    //        if (opr == "in" && isArrayData)
    //        {
    //            var listIn = new List<object>();

    //            if (isJsonArray)
    //                foreach (var ra in ((JsonElement)propertyValue).EnumerateArray())
    //                    listIn.Add(ParamValue(columnType, ra));

    //            if (isArray)
    //                foreach (var ra in (Array)propertyValue)
    //                    listIn.Add(ParamValue(columnType, ra));

    //            if (isEnumerable)
    //                foreach (var ra in (IEnumerable)propertyValue)
    //                    listIn.Add(ParamValue(columnType, ra));

    //            propertyValue = listIn.ToArray();
    //        }
    //        else
    //        {
    //            propertyValue = ParamValue(columnType, propertyValue);
    //        }

    //        if (opr == "is" || opr == "is not")
    //        {
    //            sb.Append($" [{propName}] {opr} {(propertyValue ?? "null")} ");
    //        }
    //        else
    //        {
    //            if (opr == "in" && isArrayData)
    //            {
    //                var c = "";
    //                for (int i = 1; i <= ((object[])propertyValue).Length; i++)
    //                    c += (i > 1 ? ", " : "") + "@" + aliasPropName + i;

    //                sb.Append($"[{propName}] {opr} ({c}) ");
    //            }
    //            else if (opr == "in" && !isArrayData)
    //            {
    //                sb.Append($" [{propName}] = @{aliasPropName} ");
    //            }
    //            else
    //                sb.Append($" [{propName}] {opr} @{aliasPropName} ");

    //            payload.Add(aliasPropName, propertyValue);
    //        }
    //        ix++;
    //    });

    //    var filteredCountSql = sb.ToString();

    //    if (param.Sorts.Where(p => !notMappedProps.Any(c => c.Name == p.Key)).Any())
    //    {
    //        string sort = string.Join(",", param.Sorts.Where(p => !notMappedProps.Any(c => c.Name == p.Key)).Select(x => x.Key + " " + (x.Value.ToLower() == "desc" ? "desc" : "asc")).ToArray());
    //        sb.Append($" Order By {sort}");
    //    }

    //    if (param.Page.HasValue && param.Length.HasValue)
    //    {
    //        if (param.Length.Value > 0)
    //        {
    //            if (!param.Sorts.Where(p => !notMappedProps.Any(c => c.Name == p.Key)).Any())
    //                sb.Append(" ORDER BY 1 ");

    //            int start = param.Page.Value == 0 ? 0 : (param.Page.Value - 1) * param.Length.Value;
    //            sb.Append($" OFFSET {start} ROWS FETCH NEXT {param.Length.Value} ROWS ONLY ");
    //        }
    //    }
    //    var cc = sb.ToString();
    //    var sqlCnt = sql.Replace("*", "COUNT(*)");
    //    if (!payload.Any())
    //    {
    //        cc = cc.Replace("where", "");
    //        sqlCnt = sqlCnt.Replace("where", "");
    //        filteredCountSql = filteredCountSql.Replace("where", "");
    //    }

    //    if (param?.Fields.Length > 0)
    //        cc = cc.Replace("SELECT * FROM", $"SELECT {string.Join(", ", param.Fields.Select(p => $"[{p}]"))} FROM");


    //    return new Parameter { Sql = cc, SqlForCount = sqlCnt, Payload = payload, SqlForFilteredCount = filteredCountSql.Replace("*", "COUNT(*)"), };
    //}


    //private Parameter ParameterBuilder<T>(RequestParameter param, string sqlWhere) where T : IDataTable
    //{
    //    string sql = SqlBuilder<T>();
    //    StringBuilder sb = new();
    //    sb.Append(sql);

    //    if (param.Filters?.Count > 0)
    //        sb.Append(" where 1=1 ");

    //    Dictionary<string, object> payload = new();
    //    int idx = 0;

    //    //foreach (string[] r in param.Filters)
    //    param.Filters?.ForEach(r =>
    //    {
    //        if (r.Length > 0)
    //        {
    //            if (r[0] == null)
    //                return;
    //        }
    //        var xx = r as object[];
    //        if (r.Length < 2)
    //            return;

    //        string propName = r[0].ToString();
    //        string aliasPropName = payload.Any(p => p.Key == $"P_{propName}") ? $"P_{propName}_{idx++}" : $"P_{propName}";
    //        string opr = r.Length == 3 ? r[1].ToString() : "=";
    //        object propertyValue = r.Length == 2 ? r[1] : r[2];

    //        var isJsonArray = propertyValue is JsonElement ? ((JsonElement)propertyValue).ValueKind == JsonValueKind.Array : false;
    //        var isArray = propertyValue is Array;
    //        var isEnumerable = propertyValue is IEnumerable;
    //        var isArrayData = isJsonArray || isArray || isEnumerable;

    //        var allowedOpr = new string[]
    //        {
    //            "=", ">", "<", ">=", "<=", "in", "is", "is not", "like"
    //        };

    //        if (!allowedOpr.Contains(opr))
    //            return;

    //        Type columnType = typeof(T).GetProperty(propName).PropertyType;
    //        if (opr == "in" && isArrayData)
    //        {
    //            var listIn = new List<object>();

    //            if (isJsonArray)
    //                foreach (var ra in ((JsonElement)propertyValue).EnumerateArray())
    //                    listIn.Add(ParamValue(columnType, ra));

    //            if (isArray)
    //                foreach (var ra in (Array)propertyValue)
    //                    listIn.Add(ParamValue(columnType, ra));

    //            if (isEnumerable)
    //                foreach (var ra in (IEnumerable)propertyValue)
    //                    listIn.Add(ParamValue(columnType, ra));

    //            propertyValue = listIn.ToArray();
    //        }
    //        else
    //        {
    //            propertyValue = ParamValue(columnType, propertyValue);
    //        }

    //        if (opr == "is" || opr == "is not")
    //        {
    //            sb.Append($" AND [{propName}] {opr} {(propertyValue ?? "null")} ");
    //        }
    //        else
    //        {
    //            if (opr == "in" && isArrayData)
    //            {
    //                var c = "";
    //                for (int i = 1; i <= ((object[])propertyValue).Length; i++)
    //                    c += (i > 1 ? ", " : "") + "@" + aliasPropName + i;

    //                sb.Append($" AND [{propName}] {opr} ({c}) ");
    //            }
    //            else if (opr == "in" && !isArrayData)
    //            {
    //                sb.Append($" AND [{propName}] = @{aliasPropName} ");
    //            }
    //            else
    //                sb.Append($" AND [{propName}] {opr} @{aliasPropName} ");

    //            payload.Add(aliasPropName, propertyValue);
    //        }
    //    });

    //    if(param.Filters?.Count == 0 )
    //        sb.Append(" where 1=1 ");

    //    sb.Append(" ");
    //    sb.Append(sqlWhere);

    //    var filteredCountSql = sb.ToString();

    //    if (param.Sorts.Count > 0)
    //    {
    //        string sort = string.Join(",", param.Sorts.Select(x => x.Key + " " + (x.Value.ToLower() == "desc" ? "desc" : "asc")).ToArray());
    //        sb.Append($" Order By {sort}");
    //    }

    //    if (param.Page.HasValue && param.Length.HasValue)
    //    {
    //        if (param.Length.Value > 0)
    //        {
    //            if (param.Sorts.Count == 0)
    //                sb.Append(" ORDER BY 1 ");

    //            int start = param.Page.Value == 0 ? 0 : (param.Page.Value - 1) * param.Length.Value;
    //            sb.Append($" OFFSET {start} ROWS FETCH NEXT {param.Length.Value} ROWS ONLY ");
    //        }
    //    }
    //    var cc = sb.ToString();

    //    if (param?.Fields.Length > 0)
    //        cc = cc.Replace("SELECT * FROM", $"SELECT {string.Join(", ", param.Fields.Select(p => $"[{p}]"))} FROM");

    //    return new Parameter { Sql = cc, SqlForCount = sql.Replace("*", "COUNT(*)"), Payload = payload, SqlForFilteredCount = filteredCountSql.Replace("*", "COUNT(*)"), };
    //}

    public async Task<IEnumerable<T>> ListAsync<T>(RequestParameter param) where T : IDataTable
    {
        param.Page = null;
        param.Length = null;
        var data = ParameterBuilder<T>(param);
        return await Connection.QueryAsync<T>(data.Sql, data.Payload, Transaction);
    }

    public async Task<DataTable> ListAsync(Type type, RequestParameter param)
    {
        var method = typeof(DbView).GetMethod("ParameterBuilder", BindingFlags.NonPublic | BindingFlags.Instance);

        var data = method.MakeGenericMethod(type).Invoke(this, new object[] { param }) as Parameter;

        var reader = await Connection.ExecuteReaderAsync(data.Sql, data.Payload, Transaction);
        var dt = new DataTable("DataView");
        dt.Load(reader);
        return dt;
    }

    public async Task<DataTable> ListAsync(string sql, object param)
    {
        var reader = await Connection.ExecuteReaderAsync(sql, param, Transaction);
        var dt = new DataTable("DataView");
        dt.Load(reader);
        return dt;
    }

    public async Task<IEnumerable<T>> ListAsync<T>(object param = null) where T : IDataTable
    {
        var requestParam = new RequestParameter();
        if (param != null)
            foreach (var prop in param.GetType().GetProperties())
            {
                requestParam.AddFilter(prop.Name, "=", prop.GetValue(param));
            }
        var data = ParameterBuilder<T>(requestParam);
        return await Connection.QueryAsync<T>(data.Sql, data.Payload, Transaction);
    }

    public async Task<IEnumerable<T>> ListAsync<T>(string sqlTail, object param = null) where T : IDataTable
    {
        return await Connection.QueryAsync<T>(string.Concat(SqlBuilder<T>(), " ", sqlTail), param, Transaction);
    }

    public async Task<DataResult<T>> FilterAsync<T>(RequestParameter param) where T : IDataTable
    {
        //if (param.Page > 20)
        //    param.Page = 20;

        var data = ParameterBuilder<T>(param);
        var items = (await Connection.QueryAsync<T>(data.Sql, data.Payload)).ToList();
        return new DataResult<T>
        {
            Items = items,
            Length = param.Length.Value,
            Page = param.Page.Value,
            Parameter = param,
            Filtered = Connection.ExecuteScalar<int>(data.SqlForFilteredCount, data.Payload, Transaction),
            //Total = Connection.ExecuteScalar<int>(data.SqlForCount, Transaction),
        };
    }

    public async Task<DataResult<T>> FilterAsync<T>(Type type, RequestParameter param) where T : IDataTable
    {
        if (param.Page > 20)
            param.Page = 20;

        var method = typeof(DbView).GetMethod("ParameterBuilder", BindingFlags.NonPublic | BindingFlags.Instance);

        var data = method.MakeGenericMethod(type).Invoke(this, [param]) as Parameter;

        var items = (await Connection.QueryAsync<T>(data.Sql, data.Payload)).ToList();
        return new DataResult<T>
        {
            Items = items,
            Length = param.Length.Value,
            Page = param.Page.Value,
            Parameter = param,
            Filtered = Connection.ExecuteScalar<int>(data.SqlForFilteredCount, data.Payload, Transaction),
        };
    }

    //public async Task<DataResult<T>> FilterAsync<T>(RequestParameter param, string sqlTail) where T : IDataTable
    //{
    //    if (param.Page > 20)
    //        param.Page = 20;

    //    var data = ParameterBuilder<T>(param, sqlTail);
    //    var items = (await Connection.QueryAsync<T>(string.Concat(data.Sql), data.Payload)).ToList();
    //    return new DataResult<T>
    //    {
    //        Items = items,
    //        Length = param.Length.Value,
    //        Page = param.Page.Value,
    //        Parameter = param,
    //        Filtered = Connection.ExecuteScalar<int>(data.SqlForFilteredCount, data.Payload, Transaction),
    //        //Total = Connection.ExecuteScalar<int>(data.SqlForCount, Transaction),
    //    };
    //}

    private static Dictionary<Type, List<DataHeader>> DataHeaders { get; set; } = [];

    private static readonly string[] action = new string[]
    {
        "=", ">", "<", ">=", "<=", "in", "not in", "is", "is not", "like"
    };

    public DataResult<T> Filter<T>(RequestParameter param) where T : IDataTable
    {
        //if (param.Page > 20)
        //    param.Page = 20;

        var data = ParameterBuilder<T>(param);
        var items = Connection.QueryAsync<T>(data.Sql, data.Payload).Result.ToList();

        DataHeaders.TryGetValue(typeof(T), out List<DataHeader> headers);
        if(headers == null)
        {
            headers = [..typeof(T).GetProperties().Where(p => p.GetCustomAttribute<DataColumnAttribute>() != null).Select(p => new DataHeader
            {
                PropertyName = p.Name,
                PropertyType = Nullable.GetUnderlyingType(p.PropertyType) != null ? $"{Nullable.GetUnderlyingType(p.PropertyType).Name}?"
                    : p.PropertyType.Name,
                Label = p.GetCustomAttribute<DataColumnAttribute>()?.Name ?? p.Name,
                Attribute = new
                {
                    p.GetCustomAttribute<DataColumnAttribute>()?.Name,
                    p.GetCustomAttribute<DataColumnAttribute>()?.Description,
                    p.GetCustomAttribute<DataColumnAttribute>()?.IsSortable,
                    p.GetCustomAttribute<DataColumnAttribute>()?.IsVisible,
                    p.GetCustomAttribute<DataColumnAttribute>()?.Width,
                },
                Filter = p.GetCustomAttribute<FilterableAttribute>() != null ? new
                {
                    Type = p.GetCustomAttribute<FilterableAttribute>()?.Type,
                    DataSource = p.GetCustomAttribute<FilterableAttribute>()?.DataSource,
                    DataSourceKey = p.GetCustomAttribute<FilterableAttribute>()?.DataSourceKey,
                    DataSourceValue = p.GetCustomAttribute<FilterableAttribute>()?.DataSourceValue,
                    p.GetCustomAttribute<FilterableAttribute>()?.Filters,
                } : null,
            })];
        }
        return new DataResult<T>
        {
            Headers = headers, 
            Items = items,
            Length = param.Length.Value,
            Page = param.Page.Value,
            Parameter = param,
            Filtered = Connection.ExecuteScalar<int>(data.SqlForFilteredCount, data.Payload, Transaction),
            //Total = Connection.ExecuteScalar<int>(data.SqlForCount, Transaction),
        };
    }

    public DataResult<object> Filter(Type type, RequestParameter param)
    {
        //if (param.Page > 20)
        //    param.Page = 20;

        var method = typeof(DbView).GetMethod("ParameterBuilder", BindingFlags.NonPublic | BindingFlags.Instance);
        var data = method.MakeGenericMethod(type).Invoke(this, [param]) as Parameter;

        DataHeaders.TryGetValue(type, out List<DataHeader> headers);
        headers = [..type.GetProperties().Where(p => p.GetCustomAttribute<DataColumnAttribute>() != null).Select(p => new DataHeader
            {
                PropertyName = p.Name,
                PropertyType = Nullable.GetUnderlyingType(p.PropertyType) != null ? $"{Nullable.GetUnderlyingType(p.PropertyType).Name}?"
                    : p.PropertyType.Name,
                Label = p.GetCustomAttribute<DataColumnAttribute>()?.Name ?? p.Name,
                Attribute = new
                {
                    p.GetCustomAttribute<DataColumnAttribute>()?.Name,
                    p.GetCustomAttribute<DataColumnAttribute>()?.Description,
                    p.GetCustomAttribute<DataColumnAttribute>()?.Type,
                    p.GetCustomAttribute<DataColumnAttribute>()?.IsSortable,
                    p.GetCustomAttribute<DataColumnAttribute>()?.IsVisible,
                    p.GetCustomAttribute<DataColumnAttribute>()?.Width,
                    IsDateTime = p.GetCustomAttribute<DateTimeAttribute>() != null,
                    IsDate = p.GetCustomAttribute<DateAttribute>() != null
                },
                Filter = p.GetCustomAttribute<FilterableAttribute>() != null ? new
                {
                    p.GetCustomAttribute<FilterableAttribute>()?.Property,
                    p.GetCustomAttribute<FilterableAttribute>()?.Type,
                    p.GetCustomAttribute<FilterableAttribute>()?.DataSource,
                    p.GetCustomAttribute<FilterableAttribute>()?.DataSourceKey,
                    p.GetCustomAttribute<FilterableAttribute>()?.DataSourceValue,
                    p.GetCustomAttribute<FilterableAttribute>()?.Filters,
                } : null,
            })];
        data.Sql = data.Sql.Replace("1=1  AND", "");
        data.SqlForFilteredCount = data.SqlForFilteredCount.Replace("1=1  AND", "");
        var items = Connection.QueryAsync(type: type, data.Sql, data.Payload).Result.ToList();
        return new DataResult<object>
        {
            Headers = headers,
            Items = items,
            Length = param.Length.Value,
            Page = param.Page.Value,
            Parameter = param,
            Filtered = Connection.ExecuteScalar<int>(data.SqlForFilteredCount, data.Payload, Transaction),
        };
    }

    public async Task<T> FirstAsync<T>(RequestParameter param) where T : IDataTable
    {
        var data = ParameterBuilder<T>(param);
        return await Connection.QueryFirstAsync<T>(data.Sql, data.Payload, Transaction);
    }

    public async Task<T> FirstAsync<T>(object param = null) where T : IDataTable
    {
        var requestParam = new RequestParameter();
        foreach (var prop in param.GetType().GetProperties())
        {
            requestParam.AddFilter(prop.Name, "=", prop.GetValue(param));
        }
        var data = ParameterBuilder<T>(requestParam);
        return await Connection.QueryFirstAsync<T>(data.Sql, data.Payload, Transaction);
    }

    public async Task<T> FirstAsync<T>(string sqlTail, object param = null) where T : IDataTable
    {
        return await Connection.QueryFirstAsync<T>(string.Concat(SqlBuilder<T>(), " ", sqlTail), param, Transaction);
    }

    public async Task<T> FirstOrDefaultAsync<T>(RequestParameter param) where T : IDataTable
    {
        var data = ParameterBuilder<T>(param);
        return await Connection.QueryFirstOrDefaultAsync<T>(data.Sql, data.Payload, Transaction);
    }

    public async Task<T> FirstOrDefaultAsync<T>(object param = null) where T : IDataTable
    {
        var requestParam = new RequestParameter();
        foreach (var prop in param.GetType().GetProperties())
        {
            requestParam.AddFilter(prop.Name, "=", prop.GetValue(param));
        }
        var data = ParameterBuilder<T>(requestParam);
        return await Connection.QueryFirstOrDefaultAsync<T>(data.Sql, data.Payload, Transaction);
    }

    public T FirstOrDefault<T>(object param = null) where T : IDataTable
    {
        var requestParam = new RequestParameter();
        foreach (var prop in param.GetType().GetProperties())
        {
            requestParam.AddFilter(prop.Name, "=", prop.GetValue(param));
        }
        var data = ParameterBuilder<T>(requestParam);
        return Connection.QueryFirstOrDefaultAsync<T>(data.Sql, data.Payload, Transaction).Result;
    }

    public T FirstOrDefault<T>(RequestParameter param = null) where T : IDataTable
    {
        var data = ParameterBuilder<T>(param);
        return Connection.QueryFirstOrDefaultAsync<T>(data.Sql, data.Payload, Transaction).Result;
    }

    public async Task<T> FirstOrDefaultAsync<T>(string sqlTail, object param = null) where T : IDataTable
    {
        return await Connection.QueryFirstOrDefaultAsync<T>(string.Concat(SqlBuilder<T>(), " ", sqlTail), param, Transaction);
    }

    public async Task<int> ExecuteAsync(string sql, object param = null)
    {
        return await Connection.ExecuteAsync(sql, param, Transaction);
    }

    public int Execute(string sql, object param = null)
    {
#if DEBUG
#pragma warning disable CS0168 // Variable is declared but never used
        try
        {
            return Connection.Execute(sql, param, Transaction);
        }
        catch (Exception ex)
        {
            throw;
        }
#pragma warning restore CS0168 // Variable is declared but never used
#endif
#pragma warning disable CS0162 // Unreachable code detected
        return Connection.Execute(sql, param, Transaction);
#pragma warning restore CS0162 // Unreachable code detected
    }

    internal class Parameter
    {
        public string SqlForCount { get; set; }
        public string SqlForFilteredCount { get; set; }
        public string Sql { get; set; }
        public Dictionary<string, object> Payload { get; set; }
    }

    #region NewFiltering

    //private Parameter ParameterBuilder2<T>(RequestParameter param) where T : IDataTable
    //{
    //    string sql = SqlBuilder<T>();
    //    StringBuilder sb = new();
    //    sb.Append(sql);

    //    Dictionary<string, object> payload = [];
    //    int idx = 0;

    //    var notMappedProps = typeof(T).GetProperties()
    //        .Where(p => p.GetCustomAttribute<NotMappedAttribute>() != null);

    //    List<string> whereClauses = new();

    //    // =====================================================================
    //    // BAGIAN 1 — FILTERS (versi lama)
    //    // =====================================================================
    //    if (param.Filters?.Count > 0)
    //    {
    //        int ix = 0;
    //        param.Filters.ForEach(r =>
    //        {
    //            if (r.Length < 2 || r[0] == null) return;

    //            string propName = r[0].ToString();
    //            if (notMappedProps.Any(p => p.Name == propName))
    //                return;

    //            string aliasPropName = payload.ContainsKey($"P_{propName}") ? $"P_{propName}_{idx++}" : $"P_{propName}";
    //            string opr = r.Length == 3 ? r[1].ToString() : "=";
    //            object propertyValue = r.Length == 2 ? r[1] : r[2];

    //            var isJsonArray = propertyValue is JsonElement je && je.ValueKind == JsonValueKind.Array;
    //            var isArray = propertyValue is Array;
    //            var isEnumerable = propertyValue is IEnumerable && propertyValue is not string;
    //            var isArrayData = isJsonArray || isArray || isEnumerable;

    //            Type columnType = typeof(T).GetProperty(propName)?.PropertyType;
    //            if (columnType == null)
    //                return;

    //            if (opr == "in" && isArrayData)
    //            {
    //                var listIn = new List<object>();

    //                if (isJsonArray)
    //                    foreach (var ra in ((JsonElement)propertyValue).EnumerateArray())
    //                        listIn.Add(ParamValue(columnType, ra));

    //                if (isArray)
    //                    foreach (var ra in (Array)propertyValue)
    //                        listIn.Add(ParamValue(columnType, ra));

    //                if (isEnumerable)
    //                    foreach (var ra in (IEnumerable)propertyValue)
    //                        listIn.Add(ParamValue(columnType, ra));

    //                propertyValue = listIn.ToArray();

    //                var c = string.Join(", ", ((object[])propertyValue)
    //                    .Select((_, i) => "@" + aliasPropName + (i + 1)));

    //                for (int i = 0; i < ((object[])propertyValue).Length; i++)
    //                    payload[$"{aliasPropName}{i + 1}"] = ((object[])propertyValue)[i];

    //                whereClauses.Add($"[{propName}] {opr} ({c})");
    //            }
    //            else if (opr == "is" || opr == "is not")
    //            {
    //                whereClauses.Add($"[{propName}] {opr} {(propertyValue ?? "null")}");
    //            }
    //            else
    //            {
    //                propertyValue = ParamValue(columnType, propertyValue);
    //                whereClauses.Add($"[{propName}] {opr} @{aliasPropName}");
    //                payload.Add(aliasPropName, propertyValue);
    //            }

    //            ix++;
    //        });
    //    }

    //    // =====================================================================
    //    // BAGIAN 2 — QUERY (struktur JSON baru)
    //    // =====================================================================
    //    if (param.Query?.Count > 0)
    //    {
    //        var (querySql, queryParams) = BuildJsonQuery(param.Query, typeof(T));
    //        if (!string.IsNullOrWhiteSpace(querySql))
    //            whereClauses.Add($"({querySql})");

    //        foreach (var kv in queryParams)
    //            payload[kv.Key] = kv.Value;
    //    }

    //    // =====================================================================
    //    // Gabungkan semua WHERE clause
    //    // =====================================================================
    //    if (whereClauses.Any())
    //        sb.Append(" WHERE " + string.Join(" AND ", whereClauses));

    //    // =====================================================================
    //    // Sorting
    //    // =====================================================================
    //    if (param.Sorts != null && param.Sorts.Any())
    //    {
    //        var sort = string.Join(",", param.Sorts
    //            .Where(p => !notMappedProps.Any(c => c.Name == p.Key))
    //            .Select(x => $"{x.Key} {(x.Value.ToLower() == "desc" ? "DESC" : "ASC")}"));
    //        sb.Append($" ORDER BY {sort}");
    //    }

    //    // =====================================================================
    //    // Pagination
    //    // =====================================================================
    //    if (param.Page.HasValue && param.Length.HasValue && param.Length > 0)
    //    {
    //        if (!param.Sorts.Any())
    //            sb.Append(" ORDER BY 1 ");

    //        int start = (param.Page.Value - 1) * param.Length.Value;
    //        sb.Append($" OFFSET {start} ROWS FETCH NEXT {param.Length.Value} ROWS ONLY");
    //    }

    //    // =====================================================================
    //    // Field selection
    //    // =====================================================================
    //    string sqlFinal = sb.ToString();
    //    if (param.Fields?.Length > 0)
    //        sqlFinal = sqlFinal.Replace("SELECT * FROM", $"SELECT {string.Join(", ", param.Fields.Select(f => $"[{f}]"))} FROM");

    //    // =====================================================================
    //    // Count query
    //    // =====================================================================
    //    string sqlCount = sql.Replace("*", "COUNT(*)");
    //    string sqlFilteredCount = sqlFinal.Replace("*", "COUNT(*)");

    //    return new Parameter
    //    {
    //        Sql = sqlFinal,
    //        SqlForCount = sqlCount,
    //        SqlForFilteredCount = sqlFilteredCount,
    //        Payload = payload
    //    };
    //}
    //private (string Sql, Dictionary<string, object> Params) BuildJsonQuery(List<object> query, Type entityType)
    //{
    //    StringBuilder sb = new();
    //    Dictionary<string, object> parameters = [];
    //    int idx = 0;

    //    foreach (var item in query)
    //    {
    //        if (item is JsonElement je)
    //        {
    //            if (je.ValueKind == JsonValueKind.Object)
    //            {
    //                string op = je.GetProperty("operator").GetString();
    //                var conditions = je.GetProperty("conditions").EnumerateArray().ToList();
    //                var subParts = new List<string>();

    //                foreach (var c in conditions)
    //                {
    //                    if (c.ValueKind == JsonValueKind.Object && c.TryGetProperty("field", out var field))
    //                    {
    //                        string prop = field.GetString();
    //                        string opr = c.GetProperty("operator").GetString();
    //                        var val = c.GetProperty("value");

    //                        Type columnType = entityType.GetProperty(prop)?.PropertyType;
    //                        if (columnType == null) continue;

    //                        string paramName = $"Q_{prop}_{idx++}";
    //                        parameters[paramName] = ParamValue(columnType, val);
    //                        subParts.Add($"[{prop}] {opr} @{paramName}");
    //                    }
    //                    else if (c.ValueKind == JsonValueKind.Object && c.TryGetProperty("operator", out _))
    //                    {
    //                        var (subSql, subParams) = BuildJsonQuery(new List<object> { c }, entityType);
    //                        subParts.Add($"({subSql})");
    //                        foreach (var kv in subParams)
    //                            parameters[kv.Key] = kv.Value;
    //                    }
    //                }

    //                if (subParts.Any())
    //                {
    //                    string combined = string.Join($" {op.ToUpper()} ", subParts);
    //                    sb.Append(sb.Length > 0 ? $" AND ({combined})" : $"({combined})");
    //                }
    //            }
    //        }
    //    }

    //    return (sb.ToString(), parameters);
    //}

    #endregion
}

//public interface IDataTable
//{

//}

//public interface IDataTableExport : IDataTable
//{
//}

public class DataTableRequest
{
    public string DataSource { get; set; }
    public RequestParameter Parameter { get; set; }
}

public enum QueryParameterOperator
{
    And, Or, Not, Is
}

public class QueryParameter
{
    public string Key { get; set; }
    public string Value { get; set; }
    public QueryParameterOperator Operator { get; set; }
    public QueryParameter Child { get; set; }
}


//[AttributeUsage(AttributeTargets.Property)]
//public class Included : Attribute
//{
//}

public class RequestParameter
{

    public int? Page { get; set; } = 1;
    public int? Length { get; set; } = 10;
    public Dictionary<string, string> Sorts { get; set; } = new Dictionary<string, string>();
    //public List<object[]> Filters { get; set; } = new List<object[]>();
    public JsonElement[] Filters { get; set; } = [];
    public string[] Fields { get; set; } = Array.Empty<string>();
    public bool SingleResult { get; set; } = false;

    public void AddFilter(string key, string opr, string value)
    {
        string json = $"[\"{key}\", \"{opr}\", \"{value}\"]";

        JsonElement newFilter = JsonDocument.Parse(json).RootElement;

        List<JsonElement> list = [.. Filters];
        list.Add(newFilter);

        Filters = [.. list];
    }

    public void AddFilter(string key, string value)
    {
        string json = $"[\"" + key + "\", \"=\",  \"" + value + "\"]";
        JsonElement newFilter = JsonDocument.Parse(json).RootElement;

        List<JsonElement> list = [.. Filters];
        list.Add(newFilter);

        Filters = [.. list];
    }

    public void AddFilter(string key, string opr, object value)
    {
        string json = $"[\"" + key + "\", \"" + opr + "\",  " + value + "]";
        JsonElement newFilter = JsonDocument.Parse(json).RootElement;

        List<JsonElement> list = [.. Filters];
        list.Add(newFilter);

        Filters = [.. list];
    }

    public void AddFilter(string key, string opr, string[] value)
    {
        // Buat JSON array untuk value, misalnya: ["A", "B", "C"]
        string valueJson = "[" + string.Join(",", Array.ConvertAll(value, v => $"\"{v}\"")) + "]";

        // Buat JSON penuh: ["Code", "in", ["A", "B", "C"]]
        string json = $"[\"{key}\", \"{opr}\", {valueJson}]";

        // Parse ke JsonElement
        JsonElement newFilter = JsonDocument.Parse(json).RootElement;

        // Tambahkan ke Filters
        List<JsonElement> list = [.. Filters];
        list.Add(newFilter);
        Filters = [.. list];
    }

    public void AddFilter(string key, object value)
    {
        string json = $"[\"" + key + "\", \"=\",  " + value + "]";
        JsonElement newFilter = JsonDocument.Parse(json).RootElement;

        List<JsonElement> list = [.. Filters];
        list.Add(newFilter);

        Filters = [.. list];
    }

    public bool ContainFilterKey(string key)
    {
        if (Filters == null || Filters.Length == 0)
            return false;

        foreach (var filter in Filters)
        {
            if (ContainKeyRecursive(filter, key))
                return true;
        }

        return false;
    }

    private bool ContainKeyRecursive(JsonElement element, string key)
    {
        // Kalau elemen adalah array
        if (element.ValueKind == JsonValueKind.Array)
        {
            var items = element.EnumerateArray().ToArray();

            // Cek pola ["Key", "=", ...]
            if (items.Length > 0 && items[0].ValueKind == JsonValueKind.String && items[0].GetString() == key)
                return true;

            // Cek tiap item di dalam array (rekursif)
            foreach (var item in items)
            {
                if (ContainKeyRecursive(item, key))
                    return true;
            }
        }

        return false;
    }

    public void RemoveFilter(string key, string opr)
    {
        if (Filters == null || Filters.Length == 0)
            return;

        var list = new List<JsonElement>();

        foreach (var filter in Filters)
        {
            var result = RemoveFilterRecursive(filter, key, opr);

            // Hanya tambahkan kalau filter-nya masih ada isi valid
            if (result.HasValue)
                list.Add(result.Value);
        }

        Filters = [.. list];
    }

    private JsonElement? RemoveFilterRecursive(JsonElement element, string key, string opr)
    {
        if (element.ValueKind != JsonValueKind.Array)
            return element;

        var items = element.EnumerateArray().ToArray();
        var newItems = new List<JsonElement>();

        foreach (var item in items)
        {
            // Kalau item adalah filter sederhana ["Key", "Op", "Value"]
            if (item.ValueKind == JsonValueKind.Array)
            {
                var arr = item.EnumerateArray().ToArray();

                // Cek apakah match
                if (arr.Length > 1 &&
                    arr[0].ValueKind == JsonValueKind.String &&
                    arr[1].ValueKind == JsonValueKind.String &&
                    arr[0].GetString() == key &&
                    arr[1].GetString() == opr)
                {
                    continue; // Skip (hapus)
                }

                // Kalau nested, rekursif lagi
                var inner = RemoveFilterRecursive(item, key, opr);
                if (inner.HasValue)
                    newItems.Add(inner.Value);
            }
            else
            {
                // Simpan operator seperti "AND", "OR"
                newItems.Add(item);
            }
        }

        // Kalau kosong, berarti semua isinya dihapus
        if (newItems.Count == 0)
            return null;

        // Buat ulang JsonElement dari newItems
        var jsonArray = "[" + string.Join(",", newItems.Select(i => i.GetRawText())) + "]";
        return JsonDocument.Parse(jsonArray).RootElement;
    }

    public void SetFilter(string key, string opr, object value)
    {
        // 1️⃣ Hapus semua filter lama yang cocok (rekursif)
        var list = new List<JsonElement>();
        foreach (var f in Filters)
        {
            var cleaned = RemoveFilterRecursive(f, key, opr);
            if (cleaned.HasValue)
                list.Add(cleaned.Value);
        }

        // 2️⃣ Tambahkan filter baru
        string json = BuildFilterJson(key, opr, value);
        JsonElement newFilter = JsonDocument.Parse(json).RootElement;
        list.Add(newFilter);

        Filters = [.. list];
    }
    // 🧱 Utility buat bikin JSON filter baru
    private string BuildFilterJson(string key, string opr, object value)
    {
        string valueJson;

        if (value is Array arr)
        {
            var serialized = string.Join(",", arr.Cast<object>().Select(v =>
                v is string s ? $"\"{s}\"" : v.ToString()
            ));
            valueJson = $"[{serialized}]";
        }
        else if (value is string s)
        {
            valueJson = $"\"{s}\"";
        }
        else if (value is bool b)
        {
            valueJson = b.ToString().ToLower();
        }
        else
        {
            valueJson = value.ToString() ?? "null";
        }

        return $"[\"{key}\", \"{opr}\", {valueJson}]";
    }
}

public class RequestFilter
{
    public string Key { get; set; }
    public string Opr { get; set; }
    public object Value { get; set; }
}

public class RequestFilterGetMethod
{
    public string Key { get; set; }
    public string Opr { get; set; }
    public string Value { get; set; }
}

public class DataResult
{
    public List<DataHeader> Headers { get; set; }
    public List<object> Items { get; set; }
    public int Length { get; set; }
    public int Page { get; set; }
    public int Total { get; set; }
    public int Filtered { get; set; }
    public RequestParameter Parameter { get; set; }
}

public class DataResult<T>
{
    public List<DataHeader> Headers { get; set; }
    public List<T> Items { get; set; }
    public int Length { get; set; }
    public int Page { get; set; }
    public int Total { get; set; }
    public int Filtered { get; set; }
    public RequestParameter Parameter { get; set; }
}

public class DataHeader
{
    public string PropertyName { get; set; }
    public string PropertyType { get; set; }
    public string Label { get; set; }
    public object Attribute { get; set; }
    public object Filter { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public class DataColumnAttribute : Attribute
{
    public string Name { get; set; }

    public string Description { get; set; }

    public string Type { get; set; }

    public bool IsSortable { get; set; } = true;

    public bool IsVisible { get; set; } = true;

    public double Width => string.IsNullOrWhiteSpace(Name) ? 120 : (Name.Length * 10) + 40;
}

[AttributeUsage(AttributeTargets.Property)]
public class FilterableAttribute : Attribute
{
    public string Type { get; set; } = Filterable.Text;
    public string Property { get; set; }
    public string DataSource { get; set; }
    public string DataSourceKey { get; set; }
    public string DataSourceValue { get; set; }

    public string[] Filters { get; set; }
}

public class Filterable
{
    public const string Text = "String";
    public const string Date = "Date";
    public const string Time = "Time";
    public const string DateTime = "DateTime";
    public const string EpochDateTime = "EpochDateTime";
}

[AttributeUsage(AttributeTargets.Property)]
public class DateTimeAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property)]
public class DateAttribute : Attribute
{
}


[AttributeUsage(AttributeTargets.Property)]
public class EpochDateTimeAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property)]
public class EpochTimeAttribute : Attribute
{
}

public interface IDbSchema
{

}