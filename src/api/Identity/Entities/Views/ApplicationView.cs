namespace Identity.Entities.Views;

[SqlView]
public class ApplicationView : IDataTable
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string Url { get; set; }

    public string Secret { get; set; }

    public string RedirectUrl { get; set; }

    public bool IsActive { get; set; }
}
