using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Shared;

public class EventBus(IServiceProvider serviceProvider)
{
    private readonly ConcurrentDictionary<string, List<object>> _subscribers = new();

    public void Subscribe(string routeName, object handler)
    {
        if (!_subscribers.TryGetValue(routeName, out var handlers))
        {
            handlers = [];
            _subscribers[routeName] = handlers;
        }

        handlers.Add(handler);
    }

    public async Task Publish(long workspaceId, string routeName, object message) //where T : IEventMessage
    {
        var ok = _subscribers.TryGetValue(routeName, out var handlers);
        if (!ok) return;

        foreach (var handler in handlers)
        {
            var setWsId = handler.GetType().GetMethod("SetWorkspaceId");
            setWsId.Invoke(handler, [workspaceId]);

            var setSP = handler.GetType().GetMethod("SetServiceProvider");
            setSP.Invoke(handler, [serviceProvider]);

            var method = handler.GetType().GetMethod("Handle");


            var arg = method.GetParameters()[0].ParameterType;
            method.Invoke(handler, [JsonConvert.DeserializeObject(JsonConvert.SerializeObject(message), arg)]);
        }
    }
}


public interface IMessage { }

public interface IMessageHandler<T> where T : IMessage
{
    Task Handle(T message);
}

public abstract class MessageHandler<T> : IMessageHandler<T> where T : IMessage
{
    public virtual Task Handle(T message)
    {
        return Task.CompletedTask;
    }

    protected TDbContext CreateDbContext<TDbContext>() where TDbContext : ModuleDbContext
    {
        var connStr = ServiceProvider.GetRequiredService<IConfiguration>().GetConnectionString("AppDb").Replace("{ClientId}", WorkspaceId.ToString().PadLeft(10, '0'));
        var td = Activator.CreateInstance(typeof(TDbContext), [connStr]) as TDbContext;
        return td;
    }

    protected long WorkspaceId { get; set; }

    public void SetWorkspaceId(long workspaceId)
    {
        WorkspaceId = workspaceId;
    }

    protected IServiceProvider ServiceProvider { get; set; }

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class EventKeyAttribute : Attribute
{
    public string Key { get; set; }
}