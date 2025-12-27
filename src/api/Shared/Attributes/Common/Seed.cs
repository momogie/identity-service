namespace Shared;

[AttributeUsage(AttributeTargets.Class)]
public class Seed : Attribute
{
    public Type Dependency { get; set; }
}
