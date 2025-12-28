namespace Identity.Api.Tenant.Handler;

[Authorize]
[Delete("/api/tenant/remove")]
public class TenantRemoveHandler(AppDbContext appDb, [FromQuery] string id) : CommandHandler
{
    protected Entities.DbSchema.Tenant Data { get; set; }

    public override async Task<IResult> Validate()
    {
        Data = appDb.Tenants.FirstOrDefault(p => p.Id == id);
        if (Data == null)
            return NotFound();

        return await Next();
    }

    [Pipeline(1)]
    public void Save()
    {
        appDb.Tenants.Remove(Data);
        appDb.SaveChanges();
    }

    public override Entities.DbSchema.Tenant Response()
    {
        return Data;
    }
}

