using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using System.Reflection;

namespace Shared;

public class CronManager
{
    protected IServiceProvider ServiceProvider { get; }
    protected IRecurringJobManager JobManager { get; }
    public CronManager(IServiceProvider serviceProvider, IRecurringJobManager recurringJob)
    {
        ServiceProvider = serviceProvider;
        JobManager = recurringJob;
    }

    public void RunJobs(Assembly assembly)
    {
        var list = assembly.ExportedTypes.Where(p => p.GetCustomAttribute<CronJob>() != null && p.BaseType == typeof(BaseCronJob)).ToList();

        foreach (var r in list)
        {
            var c = ServiceProvider.GetRequiredService(r) as BaseCronJob;
            var expr = c.GetType().GetCustomAttribute<CronJob>();
            JobManager.AddOrUpdate(r.FullName, () => c.Run(), expr.CronExpression);
        }
    }
}