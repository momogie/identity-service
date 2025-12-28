namespace Identity.Api.User.Handler;

[Authorize]
[Get("/api/user/search")]
public class SupplierSearchHandler(AppDbContext appDb
    , [FromQuery] string q
    , [FromQuery] string[] ids
    , [FromQuery] int c = 25) : CommandHandler
{
    public override List<UserSearchResult> Response()
    {
        ids ??= [];
        var list = from p in appDb.Users
                   where (
                    string.IsNullOrWhiteSpace(q)
                    || p.Name.Contains(q)
                    || p.Name.Equals(q)
                   )
                   && (ids.Length == 0 || ids.Contains(p.Id))
                   orderby p.Name
                   select new UserSearchResult
                   {
                       Id = p.Id,
                       Name = p.Name
                   };

        return [.. list.Take(c)];
    }
}

public class UserSearchResult
{
    public string Id { get; set; }
    public string Name { get; set; }
}