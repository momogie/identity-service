//using Microsoft.AspNetCore.Http;
//using Modules.Identity.Api.User.Command;
//using Modules.Identity.Entities;

//namespace Modules.Identity.Api.User.Handler;

//[Authorize]
//[Patch("/api/identity/user/edit")]
//public class UserEditHandler(AppDbContext appDb, [FromQuery] int id, [FromBody] UserCommand command) : CommandHandler
//{
//    protected Entities.DbSchemas.User Data;

//    public override async Task<IResult> Validate()
//    {
//        Data = appDb.Users.FirstOrDefault(p => p.Id == id);
//        if (Data == null)
//            return NotFound();

//        if (appDb.Users.Any(p => p.Id != id && p.UserName == command.UserName))
//            AddError("Username", "The username is already exists");

//        return await Next();
//    }

//    [Pipeline(1)]
//    public void Save()
//    {
//        Data.UserName = command.UserName;
//        Data.Name = command.Name;
//        Data.RoleId = command.RoleId;
//        Data.Email = command.Email;

//        appDb.SaveChanges();
//    }

//    public override Entities.DbSchemas.User Response()
//    {
//        return new Entities.DbSchemas.User { Id = id, Name = Data.Name };
//    }
//}