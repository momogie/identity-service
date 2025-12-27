namespace Shared;


[AttributeUsage(AttributeTargets.Class)]
public class SqlView : Attribute
{
    public string FileName { get; set; }
}