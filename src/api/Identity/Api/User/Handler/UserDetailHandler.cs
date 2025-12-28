namespace Identity.Api.User.Handler;

[Authorize]
[Get("/api/user/detail")]
public class UserDetailHandler(AppDbContext appDb, [FromQuery] int id) : CommandHandler
{
    protected UserView Data;

    public override async Task<IResult> Validate()
    {
        Data = await appDb.Views.FirstOrDefaultAsync<UserView>(new { Id = id });
        if (Data == null)
            return NotFound();

        return await Next();
    }

    public override UserView Response() => Data;
}