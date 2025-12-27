namespace Shared;


[AttributeUsage(AttributeTargets.Class)]
public class HttpAttribute(params string[] paths) : Attribute
{
    public string[] Paths { get; set; } = paths;
    public string Group { get; set; }
}