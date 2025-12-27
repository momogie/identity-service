//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Modules.Identity.Entities;
//using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

//namespace Modules.Identity.Api.User.Handler;

//[Authorize]
//[Delete("/api/identity/user/remove")]
//public class UserRemoveHandler(AppDbContext appDb, [FromQuery] int id) : CommandHandler
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
//        appDb.Users.Remove(Data);
//        appDb.SaveChanges();
//    }

//    public override Entities.DbSchemas.User Response()
//    {
//        return new Entities.DbSchemas.User { Id = id, Name = Data.Name };
//    }
//}

