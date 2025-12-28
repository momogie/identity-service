using Identity.Api.Organization.Command;
using Identity.Api.Tenant.Command;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Organization.Handler;

[Authorize]
[Patch("/api/organization/edit")]
public class OrganizationEditHandler(AppDbContext appDb, [FromQuery] string id, [FromBody] OrganizationCommand command) : CommandHandler
{
    protected Entities.DbSchema.Organization Data;

    public override async Task<IResult> Validate()
    {
        Data = appDb.Organizations.FirstOrDefault(p => p.Id == id);
        if (Data == null)
            return NotFound();

        if (appDb.Tenants.Any(p => p.Id != id && p.Name == command.Name))
            AddError("Username", "The username is already exists");

        return await Next();
    }

    [Pipeline(1)]
    public void Save()
    {
        Data.Name = command.Name;
        Data.Description = command.Description;
        Data.Code = command.Code;

        appDb.SaveChanges();
    }

    public override Entities.DbSchema.Organization Response()
    {
        return new Entities.DbSchema.Organization { Id = id, Name = Data.Name };
    }
}