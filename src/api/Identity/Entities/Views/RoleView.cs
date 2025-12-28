namespace Identity.Entities.Views;

[SqlView]
public class RoleView : IDataTable
{
    public string Id { get; set; }
}
