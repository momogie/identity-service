using System.ComponentModel.DataAnnotations;

namespace Identity.Entities.DbSchema;

public class Client
{
    [Key]
    [MaxLength(40)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(40)]
    public string ApplicationId { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(40)]
    public string Secret { get; set; } = Guid.NewGuid().UniqueId(40);

    public bool IsActive { get; set; }
}
