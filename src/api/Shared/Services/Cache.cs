using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Shared;

public interface ICaching
{
    T Get<T>(string key);
    Task<T> GetAsync<T>(string key);
    T Set<T>(string key, T defaultValue);
    Task<T> SetAsync<T>(string key, T defaultValue);
}

public class MemCache(IMemoryCache memoryCache) : ICaching
{
    private readonly IMemoryCache _cache = memoryCache;

    T ICaching.Get<T>(string key)
    {
        return _cache.Get<T>(key);
    }

    public Task<T> GetAsync<T>(string key)
    {
        var x = _cache.Get<T>(key);
        return Task.FromResult(_cache.Get<T>(key));
    }

    T ICaching.Set<T>(string key, T defaultValue)
    {
        return _cache.Set(key, defaultValue);
    }

    public Task<T> SetAsync<T>(string key, T defaultValue)
    {
        return Task.FromResult(_cache.Set(key, defaultValue));
    }
}

public class RedisCache(IDistributedCache cache) : ICaching
{
    private readonly IDistributedCache _cache = cache;

    T ICaching.Get<T>(string key)
    {
        object c = _cache.GetStringAsync(key).Result;
        if (c == null) return default;
        if (typeof(T) == typeof(string)) return (T)c;

        return JsonConvert.DeserializeObject<T>((string)c);
    }

    public async Task<T> GetAsync<T>(string key)
    {
        object c = await _cache.GetStringAsync(key);
        if (c == null) return default;
        if (typeof(T) == typeof(string)) return (T)c;

        return JsonConvert.DeserializeObject<T>((string)c);
    }

    T ICaching.Set<T>(string key, T defaultValue)
    {
        if (typeof(T) != typeof(string))
        {
            _ = _cache.SetStringAsync(key, JsonConvert.SerializeObject(defaultValue));
            return defaultValue;
        }

        _ = _cache.SetStringAsync(key, defaultValue as string);
        return defaultValue;
    }

    Task<T> ICaching.SetAsync<T>(string key, T defaultValue)
    {
        if (typeof(T) != typeof(string))
        {
            _ = _cache.SetStringAsync(key, JsonConvert.SerializeObject(defaultValue));
            return Task.FromResult(defaultValue);
        }

        _ = _cache.SetStringAsync(key, defaultValue as string);
        return Task.FromResult(defaultValue);
    }
}

public static class CacheExtension
{
    public static void AddCaching(this IServiceCollection services, IConfiguration config)
    {
        if (config["Caching:Provider"].Equals("redis", StringComparison.CurrentCultureIgnoreCase))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = config["Caching:Redis:Host"];
                options.InstanceName = config["Caching:Redis:InstanceName"];
            });
            services.AddScoped<ICaching, RedisCache>();
            return;
        }
        services.AddMemoryCache();
        services.AddScoped<ICaching, MemCache>();
    }
}
