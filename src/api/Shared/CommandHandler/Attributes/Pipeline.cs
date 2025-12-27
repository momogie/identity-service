namespace Shared;

[AttributeUsage(AttributeTargets.Method)]
public class Pipeline(double order) : Attribute
{
    public double Order { get; set; } = order;
    public bool SkipWhenError { get; set; }
}