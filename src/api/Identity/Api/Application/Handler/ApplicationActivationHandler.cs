namespace Identity.Api.User.Handler;

[Authorize]
[Patch("/api/application/activation")]
public class ApplicationActivationHandler(AppDbContext appDb, [FromQuery] string id) : CommandHandler
{
    protected Entities.DbSchema.Application Data { get; set; }

    public override async Task<IResult> Validate()
    {
        Data = appDb.Applications.FirstOrDefault(p => p.Id == id);
        if (Data == null)
            return NotFound();

        return await Next();
    }

    [Pipeline(1)]
    public void Save()
    {
        Data.IsActive = !Data.IsActive;
        appDb.SaveChanges();
    }

    public override Entities.DbSchema.Application Response()
    {
        return Data;
    }
}

