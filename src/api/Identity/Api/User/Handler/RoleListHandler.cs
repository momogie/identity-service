
namespace Identity.Api.User.Handler;

[Authorize]
[Post("/api/role/list")]
public class RoleListHandler(AppDbContext appDb, [FromBody] RequestParameter parameter) : CommandHandler
{
    public override DataResult<RoleView> Response()
    {
        return appDb.Views.Filter<RoleView>(parameter);
    }
}

