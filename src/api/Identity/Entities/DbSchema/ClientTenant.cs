namespace Identity.Entities.DbSchema;

public class ClientTenant
{
    [Key]
    [MaxLength(40)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(40)]
    public string ClientId { get; set; }

    [MaxLength(40)]
    public string TenantId { get; set; }
}
