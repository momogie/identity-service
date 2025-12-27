using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Shared;

public class HandlerExecutor
{
    public static async Task<IResult> RunHandler(HttpContext context, Type type)
    {
        var instance = await CreateInstanceAsync(context, type);

        var validationResult = await RunValidate(instance);
        if (validationResult is not null)
            return validationResult;

        var pipelineResult = await RunPipline(instance);
        if (validationResult is not null)
            return pipelineResult;

        var response = instance.Response();
        if(response is IResult result)
            return result;

        return Results.Ok(new ApiResult<object>
        {
            Code = "",
            Status = "SUCCESS",
            Data = response,
            Message = ""
        });
    }

    private static async Task<CommandHandler> CreateInstanceAsync(HttpContext context, Type type)
    {
        var param = await MapParameters(context, type);
        var args = param.Select(p => p.Value);
        var instance = Activator.CreateInstance(type, [.. args]) as CommandHandler;

        instance.SetServiceProvider(context.RequestServices);

        var errors = param.Where(p => p.Errors != null).SelectMany(p => p.Errors);
        foreach ( var error in errors)
            instance.AddError(error.Key, error.Value);

        return instance;
    }

    private static async Task<List<RequestParam>> MapParameters(HttpContext context, Type handlerType)
    {
        var parameters = from p in handlerType.GetConstructors()?.FirstOrDefault()?.GetParameters()
                         select new
                         {
                             ParameterInfo = p,
                             p.ParameterType,
                             IsFromBody = p.GetCustomAttribute<FromBodyAttribute>() != null,
                             IsFromQuery = p.GetCustomAttribute<FromQueryAttribute>() != null,
                             IsFromForm = p.GetCustomAttribute<FromFormAttribute>() != null,
                             IsFromService = p.GetCustomAttribute<FromServicesAttribute>() != null,
                             IsFromHeader = p.GetCustomAttribute<FromHeaderAttribute>() != null,
                         };

        if (parameters is null)
            return default;

        var list = new List<RequestParam>();

        foreach (var p in parameters)
        {
            if (p.IsFromBody)
            {
                var data = await GetFromBodyAsync(context, p.ParameterType);
                var validate = data.TryValidateRecursive(context.RequestServices);
                list.Add(new RequestParam(p.ParameterInfo.Name, data, validate));
                continue;
            }

            if (p.IsFromForm)
            {
                if(p.ParameterType.IsAssignableTo(typeof(IFormFile)))
                {
                    //var validate1 = data.TryValidateRecursive(context.RequestServices);
                    list.Add(new RequestParam(p.ParameterInfo.Name, context.Request.Form.Files.GetFile(p.ParameterInfo.Name), null));
                    continue;
                }
                var data = await GetFromFormAsync(context, p.ParameterType);
                var validate = data.TryValidateRecursive(context.RequestServices);
                list.Add(new RequestParam(p.ParameterInfo.Name, data, validate));
                continue;
            }

            if (p.IsFromQuery)
            {
                var data = await GetFromQueryAsync(context, p.ParameterInfo);
                var validate = data.TryValidateRecursive(context.RequestServices);
                list.Add(new RequestParam(p.ParameterInfo.Name, data, validate));
                continue;
            }

            if (p.IsFromHeader)
            {
                var data = await GetFromHeaderAsync(context, p.ParameterInfo);
                var validate = data.TryValidateRecursive(context.RequestServices);
                list.Add(new RequestParam(p.ParameterInfo.Name, data, validate));
                continue;
            }

            var service = context.RequestServices.GetService(p.ParameterType);
            if (p.IsFromService || service != null)
            {
                list.Add(new RequestParam(p.ParameterInfo.Name, service, null));
                continue;
            }

            try
            {
                if (p.ParameterType.IsValueType || p.ParameterType.IsPrimitive || p.ParameterType == typeof(string))
                {
                    var data = await GetFromQueryAsync(context, p.ParameterInfo);
                    var validate = data.TryValidateRecursive(context.RequestServices);
                    list.Add(new RequestParam(p.ParameterInfo.Name, data, validate));
                    continue;
                }

                if (context.Request.ContentType.Contains("json"))
                {
                    var data = await GetFromBodyAsync(context, p.ParameterType);
                    var validate = data.TryValidateRecursive(context.RequestServices);
                    list.Add(new RequestParam(p.ParameterInfo.Name, data, validate));
                    continue;
                }

                if (context.Request.HasFormContentType)
                {
                    var data = await GetFromFormAsync(context, p.ParameterType);
                    var validate = data.TryValidateRecursive(context.RequestServices);
                    list.Add(new RequestParam(p.ParameterInfo.Name, data, validate));
                    continue;
                }
            }
            catch { }

            list.Add(new RequestParam(p.ParameterInfo.Name, null, null));
        }

        return list;
    }

    private static async Task<object> GetFromQueryAsync(HttpContext context, ParameterInfo parameterInfo)
    {
        Type[] defaultValueTypes = [
            typeof(int), typeof(bool), typeof(long), typeof(float), typeof(double), typeof(string),
            typeof(int?), typeof(bool?), typeof(long?), typeof(float?), typeof(double?),
            typeof(int[]), typeof(bool[]), typeof(long[]), typeof(float[]), typeof(double[]), typeof(string[]),
            typeof(Int32), typeof(Int64), typeof(Double), typeof(Single), typeof(DateTime),
            typeof(Int32?), typeof(Int64?), typeof(Double?), typeof(Single?), typeof(DateTime?),
            typeof(Int32[]), typeof(Int64[]), typeof(Double[]), typeof(Single[]), typeof(DateTime),
        ];

        if (defaultValueTypes.Contains(parameterInfo.ParameterType))
            return ConvertTo(context.Request.Query[parameterInfo.Name].ToString(), parameterInfo.ParameterType, parameterInfo.HasDefaultValue ? parameterInfo.DefaultValue : null);

        return await context.BindCommandQueryAsync<object>(parameterInfo.ParameterType);
    }

    private static async Task<object> GetFromHeaderAsync(HttpContext context, ParameterInfo parameterInfo)
    {
        Type[] defaultValueTypes = [
            typeof(int), typeof(bool), typeof(long), typeof(float), typeof(double), typeof(string),
            typeof(int?), typeof(bool?), typeof(long?), typeof(float?), typeof(double?),
            typeof(int[]), typeof(bool[]), typeof(long[]), typeof(float[]), typeof(double[]), typeof(string[]),
            typeof(Int32), typeof(Int64), typeof(Double), typeof(Single), typeof(DateTime),
            typeof(Int32?), typeof(Int64?), typeof(Double?), typeof(Single?), typeof(DateTime?),
            typeof(Int32[]), typeof(Int64[]), typeof(Double[]), typeof(Single[]), typeof(DateTime),
        ];

        if (defaultValueTypes.Contains(parameterInfo.ParameterType))
            return ConvertTo(context.Request.Headers[parameterInfo.Name].ToString(), parameterInfo.ParameterType, parameterInfo.HasDefaultValue ? parameterInfo.DefaultValue : null);

        return await context.BindCommandHeaderAsync<object>(parameterInfo.ParameterType);
    }

    private static object ConvertTo(string input, Type targetType, object defaultValue)
    {
        // Handle nullable types
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            // If input is null or empty, return null for nullable types
            if (string.IsNullOrEmpty(input))
                return defaultValue;

            // Get the underlying type of the nullable
            targetType = Nullable.GetUnderlyingType(targetType)!;
        }

        // Handle arrays
        if (targetType.IsArray)
        {
            var elementType = targetType.GetElementType()!;
            var elements = input.Split(',').Select(e => ConvertTo(e.Trim(), elementType, null)).Where(p => p!= null).ToArray();
            var array = Array.CreateInstance(elementType, elements.Length);
            elements.CopyTo(array, 0);
            return array;
        }

        // Handle individual types
        if (targetType == typeof(int))
            return int.TryParse(input, out var i) ? i : defaultValue ?? default;

        if (targetType == typeof(long))
            return long.TryParse(input, out var l) ? l : defaultValue ?? default;

        if (targetType == typeof(bool))
            return bool.TryParse(input, out var b) && b;

        if (targetType == typeof(float))
            return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : defaultValue ?? default;

        if (targetType == typeof(double))
            return double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : defaultValue ?? default;

        if (targetType == typeof(DateTime))
            return DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : defaultValue ?? default;

        if (targetType == typeof(int?))
            return int.TryParse(input, out var i) ? i : null;

        if (targetType == typeof(long?))
            return long.TryParse(input, out var l) ? l : null;

        if (targetType == typeof(bool?))
            return bool.TryParse(input, out var b) ? b : null;

        if (targetType == typeof(float?))
            return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : null;

        if (targetType == typeof(double?))
            return double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;

        if (targetType == typeof(DateTime?))
            return DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : null;

        // Direct assignment for strings
        if (targetType == typeof(string)) return input;

        throw new NotSupportedException($"Conversion for type {targetType.Name} is not supported.");
    }

    private static async Task<object> GetFromBodyAsync(HttpContext context, Type parameterType)
    {
        if(context.Request.ContentLength == 0) return Activator.CreateInstance(parameterType);

        //var json = await context.Request.ReadFromJsonAsync<object>();
        return await context.Request.ReadFromJsonAsync(parameterType);
    }

    private static async Task<object> GetFromFormAsync(HttpContext context, Type parameterType)
    {
        if (context.Request.ContentLength == 0) return Activator.CreateInstance(parameterType);
        return await context.BindCommandFormAsync<object>(parameterType);
    }

    private static Task<IResult> RunValidate(CommandHandler handlerInstance)
    {
        if (handlerInstance.Errors.Count > 0)
        {
            return Task.FromResult(Results.BadRequest(new ApiResult<object>
            {
                Code = "",
                Status = "INVALID",
                Errors = handlerInstance.Errors,
                Message = ""
            }));
        }
        return handlerInstance.Validate();
    }

    private static async Task<IResult> RunPipline(CommandHandler handlerInstance)
    {
        var methods = from p in handlerInstance.GetType().GetMethods()
                      let pipelineAttr = p.GetCustomAttribute<Pipeline>()
                      where pipelineAttr != null
                      orderby pipelineAttr.Order
                      select new
                      {
                          MethodInfo = p,
                          PipelineAttr = pipelineAttr,
                      };

        foreach (var method in methods)
        {
            try
            {
                if (method.MethodInfo.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
                {
                    await ((Task)method.MethodInfo.Invoke(handlerInstance, null));
                }
                else
                {
                    var result = method.MethodInfo.Invoke(handlerInstance, null);
                    if (result is IResult result2)
                        return result2;
                }
            }
            catch
            {
                if (!method.PipelineAttr.SkipWhenError)
                    throw;
            }

            if (handlerInstance.BreakPipeline)
                break;
        }
        return default;
    }

    public class RequestParam(string name, object value, IDictionary<string, List<string>> errors)
    {
        public string Name { get; set; } = name;

        public object Value { get; set; } = value;

        public IDictionary<string, List<string>> Errors { get; set; } = errors;
    }
}

public class ApiResult<T>
{
    public string Code {  get; set; }

    public string Message { get; set; }

    public string Status { get; set; }
    public IDictionary<string, List<string>> Errors { get; set; }

    public T Data { get; set; }
}