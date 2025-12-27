
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace Shared;
public static class LinqExtension
{
    public static List<TResult> ToList<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
    {
        return source.Select(selector).ToList();
    }

    public static IOrderedQueryable<T> Order<T>(this IQueryable<T> source, string propertyName, bool descending)
    {
        var param = Expression.Parameter(typeof(T), string.Empty);
        var property = Expression.PropertyOrField(param, propertyName);
        var sort = Expression.Lambda(property, param);
        var call = Expression.Call(typeof(Queryable), (descending ? "OrderByDescending" : string.Empty), [typeof(T), property.Type], source.Expression, Expression.Quote(sort));

        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
    }

    public static object Filter<TEntity>(this IQueryable<TEntity> source, RequestParameter parameters)
    {
        var total = source.Count();

        var filteredList = source
                    //.WhereDynamic(parameters?.Filters)
                    .Sort(parameters?.Sorts);

        var list = filteredList.Paginate(parameters).SelectDynamic(parameters?.Fields);

        typeof(TEntity)
            .GetProperties()
            .Where(prop => prop.IsDefined(typeof(Included), false))
            .ToList()
            .ForEach(r => list = list.Include(r.Name));

        return parameters.SingleResult ? list.FirstOrDefault() : new DataResult
        {
            Items = list.ToList(),
            Length = parameters.Length.Value,
            Page = parameters.Page.Value,
            Total = total,
            Parameter = parameters,
            Filtered = filteredList.Count()
        };
    }

    public static object Filter<TEntity>(this IEnumerable<TEntity> source, RequestParameter parameters)
    {
        var total = source.Count();

        var filteredList = source.AsQueryable()
                    //.WhereDynamic(parameters.Filters)
                    .Sort(parameters.Sorts);

        var list = filteredList.Paginate(parameters).SelectDynamic(parameters.Fields);

        typeof(TEntity)
            .GetProperties()
            .Where(prop => prop.IsDefined(typeof(Included), false))
            .ToList()
            .ForEach(r => list = list.Include(r.Name));

        return parameters.SingleResult ? list.FirstOrDefault() : new DataResult
        {
            Items = list.ToList(),
            Length = parameters.Length.Value,
            Page = parameters.Page.Value,
            Total = total,
            Parameter = parameters,
            Filtered = filteredList.Count()
        };
    }

    public static object Filter<TEntity>(this IOrderedQueryable<TEntity> source, RequestParameter parameters)
    {
        var total = source.Count();

        var filteredList = source
                    //.WhereDynamic(parameters.Filters)
                    .Sort(parameters.Sorts);

        var list = filteredList.Paginate(parameters).SelectDynamic(parameters.Fields);

        typeof(TEntity)
            .GetProperties()
            .Where(prop => prop.IsDefined(typeof(Included), false))
            .ToList()
            .ForEach(r => list = list.Include(r.Name));

        return parameters.SingleResult ? list.FirstOrDefault() : new DataResult
        {
            Items = list.ToList(),
            Length = parameters.Length.Value,
            Page = parameters.Page.Value,
            Total = total,
            Parameter = parameters,
            Filtered = filteredList.Count()
        };
    }

    public static object Filter<TEntity>(this List<TEntity> source, RequestParameter parameters)
    {
        var total = source.Count;

        var filteredList = source.AsQueryable()
                    //.WhereDynamic(parameters.Filters)
                    .Sort(parameters.Sorts);

        var list = filteredList.Paginate(parameters).SelectDynamic(parameters.Fields);

        typeof(TEntity)
            .GetProperties()
            .Where(prop => prop.IsDefined(typeof(Included), false))
            .ToList()
            .ForEach(r => list = list.Include(r.Name));

        return parameters.SingleResult ? list.FirstOrDefault() : new DataResult
        {
            Items = list.ToList(),
            Length = parameters.Length.Value,
            Page = parameters.Page.Value,
            Total = total,
            Parameter = parameters,
            Filtered = filteredList.Count()
        };
    }

    private static IQueryable<T> Sort<T>(this IQueryable<T> source, Dictionary<string, string> sortOrders)
    {
        var i = 0;
        foreach (var r in sortOrders)
        {
            if (typeof(T).GetProperty(r.Key) != null)
            {
                var param = Expression.Parameter(typeof(T), string.Empty);
                var property = Expression.PropertyOrField(param, r.Key);
                var sort = Expression.Lambda(property, param);
                var OrderDirection = i == 0 ? "OrderBy" : "ThenBy";

                if (r.Value != null)
                {
                    if (r.Value.ToLower().Contains("desc"))
                        OrderDirection = i == 0 ? "OrderByDescending" : "ThenByDescending";
                }

                var call = Expression.Call(typeof(Queryable), OrderDirection, [typeof(T), property.Type], source.Expression, Expression.Quote(sort));
                source = (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
            }
            i++;
        }
        return source;
    }

    private static IQueryable<T> Sort<T>(this IOrderedQueryable<T> source, Dictionary<string, string> sortOrders)
    {
        var i = 0;
        foreach (var r in sortOrders)
        {
            if (typeof(T).GetProperty(r.Key) != null)
            {
                var param = Expression.Parameter(typeof(T), string.Empty);
                var property = Expression.PropertyOrField(param, r.Key);
                var sort = Expression.Lambda(property, param);
                var OrderDirection = i == 0 ? "OrderBy" : "ThenBy";

                if (r.Value != null)
                {
                    if (r.Value.ToLower().Contains("desc"))
                        OrderDirection = i == 0 ? "OrderByDescending" : "ThenByDescending";
                }

                var call = Expression.Call(typeof(Queryable), OrderDirection, new[] { typeof(T), property.Type }, source.Expression, Expression.Quote(sort));

                source = (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
            }
        }
        return source;
    }

    private static IQueryable<T> Paginate<T>(this IQueryable<T> source, RequestParameter parameters)
    {
        if (parameters.Page.HasValue && parameters.Length.HasValue)
        {
            var skip = (parameters.Page.Value == 0 ? 1 : parameters.Page.Value - 1) * parameters.Length.Value;

            source = source.Skip(skip);
        }

        return source.Take(parameters.Length.Value);
    }

    private static IQueryable<T> WhereDynamic<T>(this IQueryable<T> source, List<object[]> filters)
    {
        if (filters.Count == 0)
            return source;

        foreach (object[] r in filters)
        {
            if (r[0] == null)
                continue;

            if (r.Length < 2)
                continue;

            string propertyName = r[0].ToString();
            string opr = r.Length == 3 ? r[1].ToString() : "=";
            object propertyValue = r.Length == 2 ? r[1] : r[2];

            var d = propertyValue?.ToString();

            ParameterExpression pe = Expression.Parameter(typeof(T), typeof(T).Name);

            //combine them with and 1=1 Like no expression
            Expression combined = null;

            if (typeof(T).GetProperty(propertyName) != null)
            {
                //Expression for accessing Fields name property
                Expression columnNameProperty = Expression.Property(pe, propertyName);

                //the name constant to match 
                Expression columnValue = null;

                Type columnType = typeof(T).GetProperty(propertyName).PropertyType;

                if (columnType == typeof(DateTime) && propertyValue.IsDateTime() && opr.ToLower() != "in")
                    columnValue = Expression.Constant(propertyValue.ToDateTime());

                if (columnType == typeof(DateTime?) && propertyValue.IsDateTime() && opr.ToLower() != "in")
                    columnValue = Expression.Constant(propertyValue.ToNullableDateTime(), typeof(DateTime?));

                if (columnType == typeof(string) && opr.ToLower() != "in")
                    columnValue = Expression.Constant(propertyValue);

                if (columnType == typeof(int) && r.IsInt() && opr.ToLower() != "in")
                    columnValue = Expression.Constant(Convert.ToInt32(propertyValue.ToString()));

                if (columnType == typeof(int?) && r.IsInt() && opr.ToLower() != "in")
                    columnValue = Expression.Constant(propertyValue.ToNullableInt32(), typeof(int?));

                if (columnType == typeof(decimal) && propertyValue.IsDecimal() && opr.ToLower() != "in")
                    columnValue = Expression.Constant(Convert.ToDecimal(propertyValue.ToString()));

                if (columnType == typeof(decimal?) && propertyValue.IsDecimal() && opr.ToLower() != "in")
                    columnValue = Expression.Constant(propertyValue.ToNullableDecimal(), typeof(decimal?));

                if (columnType == typeof(long) && propertyValue.IsLong() && opr.ToLower() != "in")
                    columnValue = Expression.Constant(Convert.ToInt64(propertyValue?.ToString()));

                if (columnType == typeof(long?) && propertyValue.IsLong() && opr.ToLower() != "in")
                    columnValue = Expression.Constant(propertyValue.ToNullableLong(), typeof(long?));

                if (columnType == typeof(Int64) && propertyValue.IsLong() && opr.ToLower() != "in")
                    columnValue = Expression.Constant(Convert.ToInt64(propertyValue?.ToString()));

                if (columnType == typeof(Int64?) && propertyValue.IsLong() && opr.ToLower() != "in")
                    columnValue = Expression.Constant(propertyValue.ToNullableLong(), typeof(long?));

                if (columnType == typeof(bool) && opr.ToLower() != "in")
                    columnValue = Expression.Constant(propertyValue.ToBoolean());

                if (columnType == typeof(bool?) && propertyValue.IsBoolean() && opr.ToLower() != "in")
                    columnValue = Expression.Constant(propertyValue.ToNullableBoolean(), typeof(bool?));

                if (columnType == typeof(byte[]) && opr.ToLower() != "in")
                    columnValue = Expression.Constant(Convert.FromBase64String(propertyValue.ToString()));

                if (columnType == typeof(double) && propertyValue.IsDouble() && opr.ToLower() != "in")
                    columnValue = Expression.Constant(Convert.ToDouble(propertyValue.ToString()));

                if (columnType == typeof(double?) && propertyValue.IsDouble() && opr.ToLower() != "in")
                    columnValue = Expression.Constant(propertyValue.ToNullableDouble(), typeof(double?));

                Expression e1 = null;
                var c = Nullable.GetUnderlyingType(columnType);

                if (columnValue != null || (Nullable.GetUnderlyingType(columnType) != null && columnType == typeof(String)))
                {
                    if (opr == "=" || opr == "eq")
                        e1 = Expression.Equal(columnNameProperty, columnValue);

                    if (opr == ">" || opr == "ge")
                        e1 = Expression.GreaterThan(columnNameProperty, columnValue);

                    if (opr == ">=" || opr == "gte")
                        e1 = Expression.GreaterThanOrEqual(columnNameProperty, columnValue);

                    if (opr == "<" || opr == "lt")
                        e1 = Expression.LessThan(columnNameProperty, columnValue);

                    if (opr == "<=" || opr == "lte")
                        e1 = Expression.LessThanOrEqual(columnNameProperty, columnValue);

                    if (opr == "!=" || opr == "neq")
                        e1 = Expression.NotEqual(columnNameProperty, columnValue);

                    if (opr == "like")
                    {
                        var startWith = propertyValue.ToString().StartsWith("%");
                        var endsWith = propertyValue.ToString().EndsWith("%");

                        if (startWith)
                            propertyValue = propertyValue.ToString().Remove(0, 1);

                        if (endsWith)
                            propertyValue = propertyValue.ToString().Remove(propertyValue.ToString().Length - 1, 1);

                        columnValue = Expression.Constant(propertyValue);

                        Expression exp = Expression.Equal(columnNameProperty, columnValue);

                        if (startWith)
                            exp = Expression.Call(columnNameProperty, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), columnValue);

                        if (endsWith)
                            exp = Expression.Call(columnNameProperty, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), columnValue);

                        if (endsWith && startWith)
                            exp = Expression.Call(columnNameProperty, typeof(string).GetMethod("Contains", new[] { typeof(string) }), columnValue);

                        e1 = exp;
                    }

                    if (opr == "in")
                    {
                        var values = JsonConvert.DeserializeObject<string[]>(propertyValue.ToString());
                        Expression combineIn = null;
                        foreach (var ra in values)
                        {
                            var ex = Expression.Equal(columnNameProperty, Expression.Constant(ra));
                            if (combineIn == null)
                                combineIn = ex;
                            else
                                combineIn = Expression.Or(combineIn, ex);
                        }
                        if (combineIn != null)
                            source = source.Where(Expression.Lambda<Func<T, Boolean>>(combineIn, new ParameterExpression[] { pe }));
                    }
                }

                if (e1 != null)
                {
                    if (combined == null)
                        combined = e1;
                    else
                        combined = Expression.And(combined, e1);

                    source = source.Where(Expression.Lambda<Func<T, Boolean>>(combined, new ParameterExpression[] { pe }));
                }
            }

            if (propertyName.Split('.').Length > 1)
            {
                source = source.Where(BuildPredicate<T>(propertyName, opr, propertyValue.ToString()));
            }
        }
        return source;
    }

    private static IQueryable<dynamic> SelectDynamic<TEntity>(this IQueryable<TEntity> source, string[] fields)
    {
        if (fields.Length == 0)
            return source.Cast<dynamic>();

        return source.Select(DynamicFields<TEntity>(fields));
    }

    private static Expression<Func<TSource, dynamic>> DynamicFields<TSource>(string[] fields)
    {
        var source = Expression.Parameter(typeof(TSource), "o_o");
        var properties = fields
            .Select(f => typeof(TSource).GetProperty(f))
            .Select(p => new DynamicProperty(p.Name, p.PropertyType))
            .ToList();
        var resultType = DynamicClassFactory.CreateType(properties, false);
        var bindings = properties.Select(p => Expression.Bind(resultType.GetProperty(p.Name), Expression.Property(source, p.Name)));
        var result = Expression.MemberInit(Expression.New(resultType), bindings);
        return Expression.Lambda<Func<TSource, dynamic>>(result, source);
    }

    public static void UpdateDynamic<TEntity>(this DbSet<TEntity> dbSet, JObject obj) where TEntity : class
    {
        var primaryKey = typeof(TEntity).GetProperties().FirstOrDefault(prop => prop.IsDefined(typeof(KeyAttribute), false));
        if (primaryKey == null)
            throw new Exception("There's no primary key attribute assigned.");

        var id = obj.Children<JProperty>().FirstOrDefault(p => p.Name == primaryKey.Name).Value.ToString();

        ParameterExpression pe = Expression.Parameter(typeof(TEntity), typeof(TEntity).Name);
        Expression combined = Expression.Equal(Expression.Property(pe, primaryKey.Name), Expression.Constant(id.ToString()));
        var data = dbSet.FirstOrDefault(Expression.Lambda<Func<TEntity, Boolean>>(combined, new ParameterExpression[] { pe }));

        foreach (JProperty r in obj.Children())
        {
            if (typeof(TEntity).GetProperty(r.Name.ToString()) == null)
                continue;

            Type columnType = typeof(TEntity).GetProperty(r.Name.ToString()).PropertyType;
            var prop = data.GetType().GetProperty(r.Name.ToString());

            if (columnType == typeof(int))
                prop.SetValue(data, r.ToInt32());

            if (columnType == typeof(int?))
                prop.SetValue(data, r.ToNullableInt32());

            if (columnType == typeof(decimal))
                prop.SetValue(data, r.ToDecimal());

            if (columnType == typeof(decimal?))
                prop.SetValue(data, r.ToNullableDecimal());

            if (columnType == typeof(double))
                prop.SetValue(data, r.ToDecimal());

            if (columnType == typeof(double?))
                prop.SetValue(data, r.ToNullableDouble());

            if (columnType == typeof(DateTime))
                prop.SetValue(data, Convert.ToDateTime(r.Value));

            if (columnType == typeof(DateTime?))
                prop.SetValue(data, r.ToNullableDateTime());

            if (columnType == typeof(bool))
                prop.SetValue(data, r.ToBoolean());

            if (columnType == typeof(bool?))
                prop.SetValue(data, r.ToNullableBoolean());

            if (columnType == typeof(byte[]))
                prop.SetValue(data, Convert.FromBase64String(r.Value.ToString()));

            if (columnType == typeof(string))
                prop.SetValue(data, r.Value.ToString());
        }
    }

    public static IEnumerable<T> Traverse<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> childSelector)
    {
        var stack = new Stack<T>(items);
        while (stack.Count != 0)
        {
            var next = stack.Pop();
            yield return next;
            foreach (var child in childSelector(next))
                stack.Push(child);
        }
    }

    public static Task<TEntity> FirstOrDefaultIncludedAsync<TEntity>(this IQueryable<TEntity> dbSet, Expression<Func<TEntity, bool>> expression = default, CancellationToken cancellationToken = default) where TEntity : class
    {
        typeof(TEntity)
            .GetProperties()
            .Where(prop => prop.IsDefined(typeof(Included), false))
            .ToList()
            .ForEach(r => dbSet = dbSet.Include(r.Name));
        return dbSet.FirstOrDefaultAsync(expression, cancellationToken);
    }


    public static Expression<Func<T, bool>> BuildPredicate<T>(string propertyName, string comparison, string value)
    {
        var parameter = Expression.Parameter(typeof(T), "x_X");
        var left = propertyName.Split('.').Aggregate((Expression)parameter, Expression.Property);
        var body = MakeComparison(left, comparison, value);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static Expression MakeComparison(Expression left, string comparison, string value)
    {
        if (comparison.Equals("contains", StringComparison.CurrentCultureIgnoreCase))
            comparison = "Contains";
        if (comparison.Equals("startswith", StringComparison.CurrentCultureIgnoreCase))
            comparison = "StarsWith";
        if (comparison.Equals("endswith", StringComparison.CurrentCultureIgnoreCase))
            comparison = "EndsWith";

        var val = value;

        if (comparison == "like")
        {
            if (value.StartsWith("%"))
            {
                comparison = "StarsWith";
                val = val.Substring(1);
            }

            if (value.EndsWith("%"))
            {
                comparison = "EndsWith";
                val = val.Remove(val.Length - 1);
            }

            if (value.StartsWith("%") && value.EndsWith("%"))
            {
                comparison = "Contains";
                value = val;
            }
        }

        switch (comparison)
        {
            case "==":
            case "=":
            case "eq":
                return MakeBinary(ExpressionType.Equal, left, value);
            case "!=":
            case "neq":
                return MakeBinary(ExpressionType.NotEqual, left, value);
            case ">":
            case "gt":
                return MakeBinary(ExpressionType.GreaterThan, left, value);
            case ">=":
            case "gte":
                return MakeBinary(ExpressionType.GreaterThanOrEqual, left, value);
            case "<":
            case "lt":
                return MakeBinary(ExpressionType.LessThan, left, value);
            case "<=":
            case "lte":
                return MakeBinary(ExpressionType.LessThanOrEqual, left, value);
            case "Contains":
            case "StartsWith":
            case "EndsWith":
                return Expression.Call(MakeString(left), comparison, Type.EmptyTypes, Expression.Constant(value, typeof(string)));
            default:
                throw new NotSupportedException($"Invalid comparison operator '{comparison}'.");
        }
    }

    private static Expression MakeString(Expression source)
    {
        return source.Type == typeof(string) ? source : Expression.Call(source, "ToString", Type.EmptyTypes);
    }

    private static BinaryExpression MakeBinary(ExpressionType type, Expression left, string value)
    {
        object typedValue = value;
        if (left.Type != typeof(string))
        {
            if (string.IsNullOrEmpty(value))
            {
                typedValue = null;
                if (Nullable.GetUnderlyingType(left.Type) == null)
                    left = Expression.Convert(left, typeof(Nullable<>).MakeGenericType(left.Type));
            }
            else
            {
                var valueType = Nullable.GetUnderlyingType(left.Type) ?? left.Type;
                typedValue = valueType.IsEnum ? Enum.Parse(valueType, value) :
                    valueType == typeof(Guid) ? Guid.Parse(value) :
                    valueType == typeof(DateTimeOffset) ? DateTimeOffset.ParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) :
                    Convert.ChangeType(value, valueType);
            }
        }
        var right = Expression.Constant(typedValue, left.Type);
        return Expression.MakeBinary(type, left, right);
    }
}
