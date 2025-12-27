namespace Shared;

[AttributeUsage(AttributeTargets.Class)]
public class Command : Attribute
{
    public Type Handler { get; set; }
}
