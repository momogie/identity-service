namespace Identity.Api.User.Handler;

[Authorize]
[Delete("/api/user/remove")]
public class UserRemoveHandler(AppDbContext appDb, [FromQuery] string id) : CommandHandler
{
    protected Entities.DbSchema.User Data { get; set; }

    public override async Task<IResult> Validate()
    {
        Data = appDb.Users.FirstOrDefault(p => p.Id == id);
        if (Data == null)
            return NotFound();

        return await Next();
    }

    [Pipeline(1)]
    public void Save()
    {
        appDb.Users.Remove(Data);
        appDb.SaveChanges();
    }

    public override Entities.DbSchema.User Response()
    {
        return new Entities.DbSchema.User { Id = id, Name = Data.Name };
    }
}

