using Microsoft.AspNetCore.Identity;

namespace Identity.Entities.DbSchema;

public class User : IdentityUser<string>
{
    [MaxLength(100)]
    public string Name { get; set; }
    [MaxLength(40)]
    public string RoleId { get; set; }
    public bool IsActive { get; set; }
}
