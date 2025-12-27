
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Modules.Identity.Entities;
//using Modules.Identity.Entities.Views;
//using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

//namespace Modules.Identity.Api.User.Handler;

//[Authorize]
//[Get("/api/identity/user/detail")]
//public class UserDetailHandler(AppDbContext appDb, [FromQuery] int id) : CommandHandler
//{
//    protected UserView Data;

//    public override async Task<IResult> Validate()
//    {
//        Data = await appDb.Views.FirstOrDefaultAsync<UserView>(new { Id = id});
//        if (Data == null)
//            return NotFound();

//        return await Next();
//    }

//    public override UserView Response() => Data;
//}