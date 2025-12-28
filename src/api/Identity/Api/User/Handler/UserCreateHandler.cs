using Identity.Api.User.Command;

namespace Identity.Api.User.Handler;

[Authorize]
[Post("/api/user/create")]
public class UserCreateHandler(AppDbContext appDb, [FromBody] UserCommand command) : CommandHandler
{
    public string RawPassword { get; set; }

    public Entities.DbSchema.User Data { get; set; }

    public override async Task<IResult> Validate()
    {
        if (await appDb.Users.AnyAsync(p => p.UserName == command.UserName))
            AddError("UserName", "The username field is already exist");

        return await Next();
    }

    [Pipeline(1)]
    public void Save()
    {
        RawPassword = Guid.NewGuid().UniqueId(8);
        Data = new Entities.DbSchema.User
        {
            Id = Guid.NewGuid().ToString(),
            Name = command.Name,
            UserName = command.UserName,
            RoleId = command.RoleId,
            Email = command.Email,
            PasswordHash = RawPassword.Sha256(),
            IsActive = true,
        };

        appDb.Users.Add(Data);
        appDb.SaveChanges();
    }

    [Pipeline(10, SkipWhenError = true)]
    public void SendEmailCredential()
    {
        //var model = new UserCredentialModel(Configuration);
        //model.OnGet(Data, RawPassword);
        //var template = RenderView.RenderToString(model).Result;

        //_ = Task.Run(() => Mailer.Send(Result.Email, "User Credential", template));
    }
}

