//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Modules.Identity.Api.User.Command;
//using Modules.Identity.Entities;

//namespace Modules.Identity.Api.User.Handler;

//[Authorize]
//[Patch("/api/identity/user/deactivate")]
//public class UserDeactivationHandler(AppDbContext appDb,
//    [FromQuery] int id,
//    [FromBody] UserActivationCommand command) : CommandHandler
//{
//    protected Entities.DbSchemas.User Data { get; set; }

//    public override async Task<IResult> Validate()
//    {
//        Data = appDb.Users.FirstOrDefault(p => p.Id == id);
//        if (Data == null)
//            return NotFound();

//        return await Next();
//    }

//    [Pipeline(1)]
//    public void Save()
//    {
//        Data.IsActive = false;
//        appDb.SaveChanges();
//    }

//    public override Entities.DbSchemas.User Response()
//    {
//        return new Entities.DbSchemas.User { Id = id, IsActive = command.IsActive, Name = Data.Name };
//    }
//}


