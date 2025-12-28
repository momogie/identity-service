using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Client.Handler;

[Authorize]
[Post("/api/client/list")]
public class ClientListHandler(AppDbContext appDb, [FromBody] RequestParameter parameter) : CommandHandler
{
    public override DataResult<ClientView> Response()
    {
        return appDb.Views.Filter<ClientView>(parameter);
    }
}

