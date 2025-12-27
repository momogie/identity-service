//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Modules.Identity.Entities;
//using Shared;

//namespace Identity.Api.Auth.Handlers;

//[Authorize]
//[Get("/api/workspace/list")]
//public class WorkspaceListHandler(AppDbContext appDb) : CommandHandler
//{
//    public override List<object> Response()
//    {
//        var list = from p in appDb.Workspaces
//                   where appDb.WorkspaceMembers.Any(q => q.WorkspaceId == p.Id && q.UserId == UserId && (q.Email == UserEmail && !string.IsNullOrWhiteSpace(UserEmail)))
//                   select new
//                   {
//                       p.Id,
//                       p.Name,
//                       p.Description,
//                       UserId = UserId,
//                       IsAdmin = appDb.WorkspaceMembers.FirstOrDefault(q => q.UserId ==UserId || q.Email == UserEmail).IsAdmin,
//                       p.IsSandbox,
//                       p.CreateAt,
//                       p.IsTrial,
//                       p.TrialEndDate,
//                       p.ExpiredDate,
//                       p.GracePeriod,
//                       TrialDayLeft = p.TrialEndDate == null ? 0 : p.TrialEndDate.Value.Subtract(DateTime.Now).TotalDays.ToInt32(),
//                   };

//        return [.. list];
//        //return Ok(new
//        //{

//        //});
//    }
//}
