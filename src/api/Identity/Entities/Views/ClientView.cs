namespace Identity.Entities.Views;

[SqlView]
public class ClientView : IDataTable
{
    public string Id { get; set; }

    public string ApplicationId { get; set; }

    public string ApplicationName { get; set; }

    public string Name { get; set; }

    public string Secret { get; set; }

    public bool IsActive { get; set; }
}
