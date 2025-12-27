namespace Shared;

[AttributeUsage(AttributeTargets.Property)]
public class ColumnInfo : Attribute
{
    public string Type { get; set; } = "STATIC";

    public string Entity { get; set; }

    public string Name { get; set; }

    public string Query { get; set; }

    public string Description { get; set; }

    public object[] Parameters { get; set; }

    public string HexBgColor { get; set; }

    public string HexForeColor { get; set; }
}
