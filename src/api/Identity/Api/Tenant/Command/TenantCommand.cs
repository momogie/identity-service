namespace Identity.Api.Tenant.Command;

public class TenantCommand
{
    [Required]
    [MaxLength(20)]
    public string Code { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    public string Description { get; set; }
}
