namespace Identity.Api.Organization.Handler;

[Authorize]
[Post("/api/organization/list")]
public class OrganizationListHandler(AppDbContext appDb, [FromBody] RequestParameter parameter) : CommandHandler
{
    public override DataResult<OrganizationView> Response()
    {
        return appDb.Views.Filter<OrganizationView>(parameter);
    }
}

