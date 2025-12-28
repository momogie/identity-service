namespace Identity.Api.Organization.Handler;

[Authorize]
[Patch("/api/organization/activation")]
public class OrganizationActivationHandler(AppDbContext appDb, [FromQuery] string id) : CommandHandler
{
    protected Entities.DbSchema.Organization Data { get; set; }

    public override async Task<IResult> Validate()
    {
        Data = appDb.Organizations.FirstOrDefault(p => p.Id == id);
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

    public override Entities.DbSchema.Organization Response()
    {
        return Data;
    }
}

