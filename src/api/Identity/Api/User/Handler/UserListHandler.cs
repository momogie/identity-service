//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Modules.Identity.Entities;
//using Modules.Identity.Entities.Views;

//namespace Modules.Identity.Api.User.Handler;

//[Authorize]
//[Post("/api/identity/user/list")]
//public class UserListHandler(AppDbContext appDb, [FromBody] RequestParameter parameter) : CommandHandler 
//{
//    public override DataResult<UserView> Response()
//    {
//        return appDb.Views.Filter<UserView>(parameter);
//    }
//}

