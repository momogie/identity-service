using System.ComponentModel.DataAnnotations;

namespace Identity.Entities.DbSchema;

public class DbServer
{
    [Key]
    [MaxLength(40)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(100)]
    public string ConnectionString { get; set; }

    public bool IsActive { get; set; }
}
