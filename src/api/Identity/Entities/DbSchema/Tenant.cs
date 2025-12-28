using System.ComponentModel.DataAnnotations;

namespace Identity.Entities.DbSchema;

public class Tenant
{
    [Key]
    [MaxLength(40)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(100)]
    public string Description { get; set; }

    [MaxLength(20)]
    public string Code { get; set; }

    [MaxLength(40)]
    public string DbServerId { get; set; }

    public bool IsActive { get; set; } = true;

    public long CreatedAt { get; set; } = DateTime.UtcNow.ToUnixTimeMilliseconds();
}
