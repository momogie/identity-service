using Identity.Api.Application.Command;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Application.Handler;

[Authorize]
[Post("/api/application/create")]
public class ApplicationCreateHandler(AppDbContext appDb, [FromBody] ApplicationCommand command) : CommandHandler
{
    public string RawPassword { get; set; }

    public Entities.DbSchema.Application Data { get; set; }

    public override async Task<IResult> Validate()
    {
        if (await appDb.Applications.AnyAsync(p => p.Name == command.Name))
            AddError("Name", "The Application is already exist");

        return await Next();
    }

    [Pipeline(1)]
    public void Save()
    {
        Data = new Entities.DbSchema.Application
        {
            Id = Guid.NewGuid().ToString(),
            Name = command.Name,
            Secret = Guid.NewGuid().UniqueId(40),
            Url = command.Url,
            RedirectUrl = command.RedirectUrl,
            IsActive = true,
        };

        appDb.Applications.Add(Data);
        appDb.SaveChanges();
    }
}

