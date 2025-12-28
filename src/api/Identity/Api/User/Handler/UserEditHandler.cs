using Identity.Api.User.Command;

namespace Identity.Api.User.Handler;

[Authorize]
[Patch("/api/user/edit")]
public class UserEditHandler(AppDbContext appDb, [FromQuery] string id, [FromBody] UserCommand command) : CommandHandler
{
    protected Entities.DbSchema.User Data;

    public override async Task<IResult> Validate()
    {
        Data = appDb.Users.FirstOrDefault(p => p.Id == id);
        if (Data == null)
            return NotFound();

        if (appDb.Users.Any(p => p.Id != id && p.UserName == command.UserName))
            AddError("Username", "The username is already exists");

        return await Next();
    }

    [Pipeline(1)]
    public void Save()
    {
        Data.UserName = command.UserName;
        Data.Name = command.Name;
        Data.RoleId = command.RoleId;
        Data.Email = command.Email;

        appDb.SaveChanges();
    }

    public override Entities.DbSchema.User Response()
    {
        return new Entities.DbSchema.User { Id = id, Name = Data.Name };
    }
}