using System.ComponentModel.DataAnnotations;

namespace Identity.Entities.DbSchema;

public class RefreshToken
{
    [Key]
    [MaxLength(40)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [MaxLength(40)]
    public string UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}
