namespace Identity.Api.Tenant.Handler;

[Authorize]
[Post("/api/tenant/list")]
public class TenantListHandler(AppDbContext appDb, [FromBody] RequestParameter parameter) : CommandHandler
{
    public override DataResult<TenantView> Response()
    {
        return appDb.Views.Filter<TenantView>(parameter);
    }
}

