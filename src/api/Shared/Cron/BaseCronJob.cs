namespace Shared;

public interface ICronJob { }

[AttributeUsage(AttributeTargets.Class)]
public class CronJob(string cronExpression) : Attribute
{
    public string CronExpression { get; set; } = cronExpression;
}

public abstract class BaseCronJob : ICronJob
{
    public abstract Task Run();
}
