using Identity.Api.Application.Command;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Application.Handler;

[Authorize]
[Patch("/api/client/edit")]
public class ClientEditHandler(AppDbContext appDb, 
    [FromQuery] string id, 
    [FromBody] ApplicationCommand command) : CommandHandler
{
    protected Entities.DbSchema.Application Data;

    public override async Task<IResult> Validate()
    {
        Data = appDb.Applications.FirstOrDefault(p => p.Id == id);
        if (Data == null)
            return NotFound();

        if (appDb.Applications.Any(p => p.Id != id && p.Name == command.Name))
            AddError("Username", "The username is already exists");

        return await Next();
    }

    [Pipeline(1)]
    public void Save()
    {
        Data.Name = command.Name;
        Data.Url = command.Url;
        Data.RedirectUrl = command.Url;
        
        appDb.SaveChanges();
    }

    public override Entities.DbSchema.Application Response()
    {
        return Data;
    }
}