using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Application.Handler;

[Authorize]
[Delete("/api/client/remove")]
public class ClientRemoveHandler(AppDbContext appDb, [FromQuery] string id) : CommandHandler
{
    protected Entities.DbSchema.Application Data { get; set; }

    public override async Task<IResult> Validate()
    {
        Data = appDb.Applications.FirstOrDefault(p => p.Id == id);
        if (Data == null)
            return NotFound();

        return await Next();
    }

    [Pipeline(1)]
    public void Save()
    {
        appDb.Applications.Remove(Data);
        appDb.SaveChanges();
    }

    public override Entities.DbSchema.Application Response()
    {
        return Data;
    }
}

