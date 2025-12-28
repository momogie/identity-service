using Identity.Api.Application.Command;
using Identity.Api.Client.Command;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Application.Handler;

[Authorize]
[Post("/api/client/create")]
public class ClientCreateHandler(AppDbContext appDb, [FromBody] ClientCommand command) : CommandHandler
{
    public Entities.DbSchema.Client Data { get; set; }

    public override async Task<IResult> Validate()
    {
        if (await appDb.Clients.AnyAsync(p => p.Name == command.Name))
            AddError("Name", "The Client is already exist");

        return await Next();
    }

    [Pipeline(1)]
    public void Save()
    {
        Data = new Entities.DbSchema.Client
        {
            Id = Guid.NewGuid().ToString(),
            ApplicationId = command.ApplicationId,
            Name = command.Name,
            Secret = Guid.NewGuid().UniqueId(40),
            IsActive = true,
        };

        appDb.Clients.Add(Data);
        appDb.SaveChanges();
    }
}

