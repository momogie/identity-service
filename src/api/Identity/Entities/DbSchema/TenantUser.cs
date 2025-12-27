using System.ComponentModel.DataAnnotations;

namespace Identity.Entities.DbSchema;

public class TenantUser
{
    [Key]
    [MaxLength(40)]
    public string Id { get; set; }

    [MaxLength(40)]
    public string UserId { get; set; }

    [MaxLength(40)]
    public string TenantId { get; set; }

    [MaxLength(40)]
    public string RoleId { get; set; }

    public bool IsActive { get; set; }

    public long CreatedAt { get; set; } = DateTime.UtcNow.ToUnixTimeMilliseconds();
}
