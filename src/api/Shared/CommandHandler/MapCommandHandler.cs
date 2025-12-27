global using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Reflection;
using Microsoft.OpenApi.Models;
using System.Collections;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Primitives;
using System.Globalization;

namespace Shared;

public static class Extensions
{
    public static void MapMessageHandlers(this WebApplication app, Assembly assembly)
    {
        using var scope = app.Services.CreateScope();
        var eventBus = scope.ServiceProvider.GetRequiredService<EventBus>();
        var list = assembly.ExportedTypes.Where(p => p.BaseType.IsGenericType && p.BaseType.GetGenericTypeDefinition() == typeof(MessageHandler<>));
        foreach (Type r in list)
            eventBus.Subscribe(r.GetCustomAttribute<EventKeyAttribute>().Key, scope.ServiceProvider.GetRequiredService(r));
    }

    public static void AddExportImport(this IServiceCollection services, Assembly assembly)
    {
        foreach (Type r in assembly.GetExportedTypes().Where(p => p.IsAssignableTo(typeof(IImportHandler))))
        {
            services.AddScoped(r);
            ImportExtension.AddImportHandler(r);
        }

        foreach (Type r in assembly.GetExportedTypes().Where(p => p.IsAssignableTo(typeof(IImportable))))
        {
            ImportExtension.AddImportTemplate(r);
        }

        foreach (Type r in assembly.GetExportedTypes().Where(p => p.IsAssignableTo(typeof(IDataTableExport))))
            ImportExtension.AddExportTemplate(r);
    }

    public static void RegisterDbView<T>(this WebApplication app, Assembly assembly, Type[] excludeTypes) where T : IModuleDbContext
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<T>();
        var name = typeof(Extensions).Namespace;
        foreach (Type r in assembly.GetExportedTypes().Where(p => p.GetCustomAttributes(true).Any(c => c.GetType() == typeof(SqlView))))
        {
            if(excludeTypes != null && excludeTypes.Any(c => c == r))
                continue;

            db.RegisterView(db.Schema, r);
        }
    }

    public static void RegisterDbView<T>(this WebApplication app, Assembly assembly) where T : IModuleDbContext
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<T>();
        var name = typeof(Extensions).Namespace;
        foreach (Type r in assembly.GetExportedTypes().Where(p => p.GetCustomAttributes(true).Any(c => c.GetType() == typeof(SqlView))))
            db.RegisterView(db.Schema, r);
    }

    public static void MapCommandHandlers(this WebApplication app, Assembly assembly)
    {
        var handlerTypes = from p in assembly.ExportedTypes
                           let httpAttr = p.GetCustomAttribute<HttpAttribute>()
                           let authAttr = p.GetCustomAttribute<AuthorizeAttribute>()
                           where p.IsAssignableTo(typeof(ICommandHandler))
                            && p != typeof(CommandHandler)
                            && p != typeof(ICommandHandler)
                            && httpAttr != null
                            select new
                            {
                                Type = p,
                                HttpAttribute = httpAttr,
                                IsRequireAuthorization = authAttr != null,
                                AuthorizeAttribute = authAttr,
                            };


        foreach (var handlerType in handlerTypes)
        {
            foreach (var path in handlerType.HttpAttribute.Paths)
            {
                var routeBuilder = MapRoute(app, path, handlerType.HttpAttribute, handlerType.Type);

                if (handlerType.IsRequireAuthorization)
                    routeBuilder.RequireAuthorization(handlerType.AuthorizeAttribute.AuthenticationSchemes);

                MapOpenApi(app, handlerType.Type, handlerType.HttpAttribute.Group, path, routeBuilder);
            }
        }
    }

    private static RouteHandlerBuilder MapRoute(WebApplication app, string path, HttpAttribute attribute, Type handlerType)
    {
        var delegatedFunction = (HttpContext context) => HandlerExecutor.RunHandler(context, handlerType);

        if (attribute is Post)
            return app.MapPost(path, delegatedFunction);

        if (attribute is Patch)
            return app.MapPatch(path, delegatedFunction);

        if (attribute is Put)
            return app.MapPut(path, delegatedFunction);

        if (attribute is Delete)
            return app.MapDelete(path, delegatedFunction);

        return app.MapGet(path, delegatedFunction);
    }


    static Type[] DefaultValueTypes = [
        typeof(int), typeof(bool), typeof(long), typeof(float), typeof(double), typeof(string),
        typeof(int?), typeof(bool?), typeof(long?), typeof(float?), typeof(double?),
        typeof(int[]), typeof(bool[]), typeof(long[]), typeof(float[]), typeof(double[]), typeof(string[]),
        typeof(Int32), typeof(Int64), typeof(Double), typeof(Single), typeof(DateTime),
        typeof(Int32?), typeof(Int64?), typeof(Double?), typeof(Single?), typeof(DateTime?),
        typeof(Int32[]), typeof(Int64[]), typeof(Double[]), typeof(Single[]), typeof(DateTime),
    ];

    static void MapOpenApi(WebApplication app, Type handlerType, string group, string routePath, RouteHandlerBuilder builder)
    {
        var scope = app.Services.CreateScope();

        var baseType = typeof(ModuleDbContext<>);
        var responseMethod = handlerType.GetMethod("Response");
        var parameters = handlerType.GetConstructors().First().GetParameters().Where(p => scope.ServiceProvider.GetServices<IModuleDbContext>().Any(c => baseType.IsAssignableFrom(c.GetType())));

        var d = parameters?.FirstOrDefault(p => !DefaultValueTypes.Contains(p.ParameterType));
        if (d != null)
        {
            if (d.ParameterType.GetProperties().Any(p => p.PropertyType.IsAssignableTo(typeof(IFormFile))))
                builder.Accepts(d.ParameterType, "multipart/form-data");
            else
                builder.Accepts(d.ParameterType, "application/json");
        }

        if(responseMethod.ReturnType is IResult)
        {
            builder.Produces(200, responseMethod.ReturnType, "application/json");
        }
        else if (responseMethod.ReturnType == typeof(FileContentHttpResult))
        {
            builder.Produces(200, contentType: "application/octet-stream");
        }
        else
        {
            var asd = typeof(DefaultApiResponse<>).MakeGenericType(responseMethod.ReturnType);
            builder.Produces(200, asd, "application/json");
        }

        builder.Produces(401);
        builder.Produces(403);
        builder.Produces(404);
        builder.Produces(500);

        if(!string.IsNullOrWhiteSpace(group))
            builder.WithGroupName(group);

        if (routePath.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            var aa = routePath.Split("/");
            if (aa.Length > 1) builder.WithTags(aa[2].ToUpper());
        }

        // Assign query strings
        builder.WithOpenApi(operation =>
        {
            operation.Parameters.Clear();
            var ls = parameters.Where(p => DefaultValueTypes.Contains(p.ParameterType));
            foreach (var r in ls)
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = r.Name.ToLower(),
                    In = ParameterLocation.Query,
                    Required = false,
                    Schema = OpenApiTypeMapper.MapTypeToOpenApiPrimitiveType(r.ParameterType),
                });
            }
            return operation;
        });
    }
}

public static class Binder
{
    public static async Task<T> BindCommandFormAsync<T>(this HttpContext context, Type commandType) where T : class
    {
        var modelBinderFactory = context.RequestServices.GetService<IModelBinderFactory>();
        var metadataProvider = new EmptyModelMetadataProvider();
        var modelMetadata = metadataProvider.GetMetadataForType(commandType);

        var modelBinder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
        {
            Metadata = modelMetadata,
            BindingInfo = new BindingInfo { BindingSource = BindingSource.Form }
        });

        var bindingContext = new DefaultModelBindingContext
        {
            ModelMetadata = modelMetadata,
            ModelName = string.Empty,
            BinderModelName = string.Empty,
            BindingSource = BindingSource.Form,
            FieldName = string.Empty,
            ModelState = new ModelStateDictionary(),
            ValidationState = [],
            ValueProvider = new FormValueProvider(BindingSource.Form, context.Request.Form, System.Globalization.CultureInfo.CurrentCulture),
            ActionContext = new ActionContext
            {
                HttpContext = context,
                RouteData = context.GetRouteData(),
                ActionDescriptor = new ActionDescriptor()
            }
        };

        await modelBinder.BindModelAsync(bindingContext);

        return bindingContext.Result.Model as T;
    }

    public static async Task<T> BindCommandQueryAsync<T>(this HttpContext context, Type type) where T : class
    {
        var modelBinderFactory = context.RequestServices.GetService<IModelBinderFactory>();
        var metadataProvider = new EmptyModelMetadataProvider();
        var modelMetadata = metadataProvider.GetMetadataForType(type);

        var modelBinder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
        {
            Metadata = modelMetadata,
            BindingInfo = new BindingInfo { BindingSource = BindingSource.Form }
        });

        var bindingContext = new DefaultModelBindingContext
        {
            ModelMetadata = modelMetadata,
            ModelName = string.Empty,
            BinderModelName = string.Empty,
            BindingSource = BindingSource.Form,
            FieldName = string.Empty,
            ModelState = new ModelStateDictionary(),
            ValidationState = [],
            ValueProvider = new QueryStringValueProvider(BindingSource.Query, context.Request.Query, System.Globalization.CultureInfo.InvariantCulture),
            ActionContext = new ActionContext
            {
                HttpContext = context,
                RouteData = context.GetRouteData(),
                ActionDescriptor = new ActionDescriptor()
            }
        };

        await modelBinder.BindModelAsync(bindingContext);

        if (!bindingContext.Result.IsModelSet)
            return Activator.CreateInstance(type) as T;

        return bindingContext.Result.Model as T;
    }

    public static async Task<T> BindCommandHeaderAsync<T>(this HttpContext context, Type type) where T : class
    {
        var modelBinderFactory = context.RequestServices.GetService<IModelBinderFactory>();
        var metadataProvider = new EmptyModelMetadataProvider();
        var modelMetadata = metadataProvider.GetMetadataForType(type);

        var modelBinder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
        {
            Metadata = modelMetadata,
            BindingInfo = new BindingInfo { BindingSource = BindingSource.Form }
        });

        var bindingContext = new DefaultModelBindingContext
        {
            ModelMetadata = modelMetadata,
            ModelName = string.Empty,
            BinderModelName = string.Empty,
            BindingSource = BindingSource.Form,
            FieldName = string.Empty,
            ModelState = new ModelStateDictionary(),
            ValidationState = [],
            ValueProvider = new HeaderValueProvider(context.Request.Headers, CultureInfo.InvariantCulture),
            ActionContext = new ActionContext
            {
                HttpContext = context,
                RouteData = context.GetRouteData(),
                ActionDescriptor = new ActionDescriptor()
            }
        };

        await modelBinder.BindModelAsync(bindingContext);

        if (!bindingContext.Result.IsModelSet)
            return Activator.CreateInstance(type) as T;

        return bindingContext.Result.Model as T;
    }

    public static Dictionary<string, List<string>> TryValidateRecursive(this object model, IServiceProvider serviceProvider, string propName = null)
    {
        var list = new Dictionary<string, List<string>>();
        if (model is null)
            return list;

        if (model is IEnumerable enumObjects)
        {
            int i = 0;
            foreach (var enumObj in enumObjects)
            {
                list = list.Concat(TryValidateRecursive(enumObj, serviceProvider, $"{propName}[{i}]")).ToDictionary(x => x.Key, x => x.Value);
                i++;
            }
        }
        else
        {
            var propTypes = new Type[]
            {
                typeof(string), typeof(bool), typeof(bool?),
                typeof(bool), typeof(bool?),
                typeof(int), typeof(int?),
                typeof(long), typeof(long?),
                typeof(double), typeof(double?),
                typeof(byte), typeof(byte?),
                typeof(float), typeof(float?),
                typeof(DateTime), typeof(DateTime?),
                //typeof(EpochDateTime), typeof(EpochDateTime?),
            };

            list = TryValidate(model, serviceProvider).ToDictionary(p => $"{(string.IsNullOrWhiteSpace(propName) ? "" : propName + ".")}{p.Key}", p => p.Value);
            var properties = model.GetType().GetProperties().Where(prop => !prop.PropertyType.IsPrimitive
            && !prop.PropertyType.IsValueType
            && prop.PropertyType != typeof(string)
            && prop.GetIndexParameters().Length == 0).ToList();

            foreach (var property in properties)
            {
                var value = property.GetValue(model);

                if (value == null)
                    continue;
                var nestedPropName = string.IsNullOrWhiteSpace(propName) ? property.Name : string.Concat(propName, ".", property.Name);
                list = list.Concat(TryValidateRecursive(value, serviceProvider, nestedPropName)).ToDictionary(x => x.Key, x => x.Value);
            }
        }
        return list;
    }

    public static Dictionary<string, List<string>> TryValidate(this object model, IServiceProvider serviceProvider)
    {
        ValidationContext vc = new(model, serviceProvider, null);
        ICollection<ValidationResult> results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(model, vc, results, true);

        return results.SelectMany(p => p.MemberNames.Select(c => c)).ToDictionary(p => p.Replace("$.", ""), p => results.Where(c => c.MemberNames.Any(d => d == p)).Select(c => c.ErrorMessage).ToList());
    }
}

/// <summary>
/// Extension methods for <see cref="Type"/>.
/// </summary>
public static class OpenApiTypeMapper
{
    private static readonly Dictionary<Type, Func<OpenApiSchema>> _simpleTypeToOpenApiSchema = new()
    {
        [typeof(bool)] = () => new() { Type = "boolean" },
        [typeof(byte)] = () => new() { Type = "string", Format = "byte" },
        [typeof(int)] = () => new() { Type = "integer", Format = "int32" },
        [typeof(uint)] = () => new() { Type = "integer", Format = "int32" },
        [typeof(long)] = () => new() { Type = "integer", Format = "int64" },
        [typeof(ulong)] = () => new() { Type = "integer", Format = "int64" },
        [typeof(float)] = () => new() { Type = "number", Format = "float" },
        [typeof(double)] = () => new() { Type = "number", Format = "double" },
        [typeof(decimal)] = () => new() { Type = "number", Format = "double" },
        [typeof(DateTime)] = () => new() { Type = "string", Format = "date-time" },
        [typeof(DateTimeOffset)] = () => new() { Type = "string", Format = "date-time" },
        [typeof(Guid)] = () => new() { Type = "string", Format = "uuid" },
        [typeof(char)] = () => new() { Type = "string" },

        // Nullable types
        [typeof(bool?)] = () => new() { Type = "boolean", Nullable = true },
        [typeof(byte?)] = () => new() { Type = "string", Format = "byte", Nullable = true },
        [typeof(int?)] = () => new() { Type = "integer", Format = "int32", Nullable = true },
        [typeof(uint?)] = () => new() { Type = "integer", Format = "int32", Nullable = true },
        [typeof(long?)] = () => new() { Type = "integer", Format = "int64", Nullable = true },
        [typeof(ulong?)] = () => new() { Type = "integer", Format = "int64", Nullable = true },
        [typeof(float?)] = () => new() { Type = "number", Format = "float", Nullable = true },
        [typeof(double?)] = () => new() { Type = "number", Format = "double", Nullable = true },
        [typeof(decimal?)] = () => new() { Type = "number", Format = "double", Nullable = true },
        [typeof(DateTime?)] = () => new() { Type = "string", Format = "date-time", Nullable = true },
        [typeof(DateTimeOffset?)] = () => new() { Type = "string", Format = "date-time", Nullable = true },
        [typeof(Guid?)] = () => new() { Type = "string", Format = "uuid", Nullable = true },
        [typeof(char?)] = () => new() { Type = "string", Nullable = true },

        [typeof(Uri)] = () => new() { Type = "string", Format = "uri" }, // Uri is treated as simple string
        [typeof(string)] = () => new() { Type = "string" },
        [typeof(object)] = () => new() { Type = "object" }
    };

    /// <summary>
    /// Maps a simple type to an OpenAPI data type and format.
    /// </summary>
    /// <param name="type">Simple type.</param>
    /// <remarks>
    /// All the following types from http://swagger.io/specification/#data-types-12 are supported.
    /// Other types including nullables and URL are also supported.
    /// Common Name      type    format      Comments
    /// ===========      ======= ======      =========================================
    /// integer          integer int32       signed 32 bits
    /// long             integer int64       signed 64 bits
    /// float            number  float
    /// double           number  double
    /// string           string  [empty]
    /// byte             string  byte        base64 encoded characters
    /// binary           string  binary      any sequence of octets
    /// boolean          boolean [empty]
    /// date             string  date        As defined by full-date - RFC3339
    /// dateTime         string  date-time   As defined by date-time - RFC3339
    /// password         string  password    Used to hint UIs the input needs to be obscured.
    /// If the type is not recognized as "simple", System.String will be returned.
    /// </remarks>
    public static OpenApiSchema MapTypeToOpenApiPrimitiveType(this Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return _simpleTypeToOpenApiSchema.TryGetValue(type, out var result)
            ? result()
            : new() { Type = "string" };
    }

    /// <summary>
    /// Maps an OpenAPI data type and format to a simple type.
    /// </summary>
    /// <param name="schema">The OpenApi data type</param>
    /// <returns>The simple type</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Type MapOpenApiPrimitiveTypeToSimpleType(this OpenApiSchema schema)
    {
        if (schema == null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        var type = (schema.Type?.ToLowerInvariant(), schema.Format?.ToLowerInvariant(), schema.Nullable) switch
        {
            ("boolean", null, false) => typeof(bool),
            ("integer", "int32", false) => typeof(int),
            ("integer", "int64", false) => typeof(long),
            ("number", "float", false) => typeof(float),
            ("number", "double", false) => typeof(double),
            ("number", "decimal", false) => typeof(decimal),
            ("string", "byte", false) => typeof(byte),
            ("string", "date-time", false) => typeof(DateTimeOffset),
            ("string", "uuid", false) => typeof(Guid),
            ("string", "duration", false) => typeof(TimeSpan),
            ("string", "char", false) => typeof(char),
            ("string", null, false) => typeof(string),
            ("object", null, false) => typeof(object),
            ("string", "uri", false) => typeof(Uri),
            ("integer", "int32", true) => typeof(int?),
            ("integer", "int64", true) => typeof(long?),
            ("number", "float", true) => typeof(float?),
            ("number", "double", true) => typeof(double?),
            ("number", "decimal", true) => typeof(decimal?),
            ("string", "byte", true) => typeof(byte?),
            ("string", "date-time", true) => typeof(DateTimeOffset?),
            ("string", "uuid", true) => typeof(Guid?),
            ("string", "char", true) => typeof(char?),
            ("boolean", null, true) => typeof(bool?),
            _ => typeof(string),
        };

        return type;
    }
}

public class DefaultApiResponse<T>
{
    public string Code { get; set; }
    public string Status { get; set; }

    public string Message { get; set; }

    public T Data { get; set; }

    public IDictionary<string, string[]> Errors { get; set; }
}

public class HeaderValueProvider : IValueProvider
{
    private readonly IDictionary<string, StringValues> _headers;
    private readonly CultureInfo _culture;

    public HeaderValueProvider(IDictionary<string, StringValues> headers, CultureInfo culture)
    {
        _headers = headers ?? throw new ArgumentNullException(nameof(headers));
        _culture = culture ?? CultureInfo.InvariantCulture;
    }

    public bool ContainsPrefix(string prefix)
    {
        return _headers.ContainsKey(prefix);
    }

    public ValueProviderResult GetValue(string key)
    {
        if (_headers.TryGetValue(key, out var value))
        {
            return new ValueProviderResult(value, _culture);
        }

        return ValueProviderResult.None;
    }
}
