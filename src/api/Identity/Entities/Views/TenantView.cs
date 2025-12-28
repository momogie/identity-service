namespace Identity.Entities.Views;

[SqlView]
public class TenantView : IDataTable
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string Code { get; set; }

    public bool IsActive { get; set; } = true;

    public long CreatedAt { get; set; }
}
