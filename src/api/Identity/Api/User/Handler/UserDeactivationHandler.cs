using Identity.Api.User.Command;

namespace Identity.Api.User.Handler;

[Authorize]
[Patch("/api/user/deactivate")]
public class UserDeactivationHandler(AppDbContext appDb,
    [FromQuery] string id,
    [FromBody] UserActivationCommand command) : CommandHandler
{
    protected Entities.DbSchema.User Data { get; set; }

    public override async Task<IResult> Validate()
    {
        Data = appDb.Users.FirstOrDefault(p => p.Id == id);
        if (Data == null)
            return NotFound();

        return await Next();
    }

    [Pipeline(1)]
    public void Save()
    {
        Data.IsActive = false;
        appDb.SaveChanges();
    }

    public override Entities.DbSchema.User Response()
    {
        return new Entities.DbSchema.User { Id = id, IsActive = command.IsActive, Name = Data.Name };
    }
}


