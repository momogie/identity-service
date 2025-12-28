namespace Identity.Entities.DbSchema;

public class Application
{
    [Key]
    [MaxLength(40)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(255)]
    public string Url { get; set; }

    [MaxLength(40)]
    public string Secret { get; set; }

    [MaxLength(255)]
    public string RedirectUrl { get; set; }

    public bool IsActive { get; set; }

    public bool IsExternal { get; set; }
}
