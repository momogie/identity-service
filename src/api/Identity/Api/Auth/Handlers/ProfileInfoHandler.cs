namespace Identity.Api.Auth.Handlers;

[Authorize]
[Get("/api/profile/user-info")]
public class ProfileInfoHandler(AppDbContext appDb) : CommandHandler
{
    public override IResult Response()
    {
        var user = appDb.Users.FirstOrDefault(p => p.Id == UserId);
        return Ok(new
        {
            user.Name,
            user.UserName,
            user.Email,
            user.EmailConfirmed,
        });
    }
}
