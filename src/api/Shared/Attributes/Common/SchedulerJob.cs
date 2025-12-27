namespace Shared;

[AttributeUsage(AttributeTargets.Class)]
public class SchedulerJob : Attribute
{
    public string Name { get; set; }

    public string CronExpression { get; set; }

    public bool IsRegisterFromApp { get; set; } = false;
}
