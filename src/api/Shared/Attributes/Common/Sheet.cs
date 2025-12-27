namespace Shared;

[AttributeUsage(AttributeTargets.Property)]
public class SheetAttribute : Attribute
{
    public string PropertyKey { get; set; }
    public string Name { get; set; }
    public string ErrorMessage { get; set; }

    public SheetAttribute(string name, string propertyKey)
    {
        Name = name;
        PropertyKey = propertyKey;
    }
}
