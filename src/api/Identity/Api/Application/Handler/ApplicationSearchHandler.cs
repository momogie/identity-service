namespace Identity.Api.User.Handler;

[Authorize]
[Get("/api/application/search")]
public class ApplicationSearchHandler(AppDbContext appDb
    , [FromQuery] string q
    , [FromQuery] string[] ids
    , [FromQuery] int c = 25) : CommandHandler
{
    public override List<ApplicationSearchResult> Response()
    {
        ids ??= [];
        var list = from p in appDb.Applications
                   where (
                    string.IsNullOrWhiteSpace(q)
                    || p.Name.Contains(q)
                    || p.Name.Equals(q)
                   )
                   && (ids.Length == 0 || ids.Contains(p.Id))
                   orderby p.Name
                   select new ApplicationSearchResult
                   {
                       Id = p.Id,
                       Name = p.Name
                   };

        return [.. list.Take(c)];
    }
}

public class ApplicationSearchResult
{
    public string Id { get; set; }
    public string Name { get; set; }
}