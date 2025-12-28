using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Application.Handler;

[Authorize]
[Post("/api/application/list")]
public class ApplicationListHandler(AppDbContext appDb, [FromBody] RequestParameter parameter) : CommandHandler
{
    public override DataResult<ApplicationView> Response()
    {
        return appDb.Views.Filter<ApplicationView>(parameter);
    }
}

