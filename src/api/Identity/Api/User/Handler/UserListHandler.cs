namespace Identity.Api.User.Handler;

[Authorize]
[Post("/api/user/list")]
public class UserListHandler(AppDbContext appDb, [FromBody] RequestParameter parameter) : CommandHandler
{
    public override DataResult<UserView> Response()
    {
        return appDb.Views.Filter<UserView>(parameter);
    }
}

