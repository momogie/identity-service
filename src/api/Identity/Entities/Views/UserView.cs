namespace Identity.Entities.Views;

[SqlView]
public class UserView : IDataTable
{
    public string Id { get; set; }
}
