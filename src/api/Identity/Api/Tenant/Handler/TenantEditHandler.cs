using Identity.Api.Organization.Command;
using Identity.Api.Tenant.Command;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Tenant.Handler;

[Authorize]
[Patch("/api/tenant/edit")]
public class TenantEditHandler(AppDbContext appDb, [FromQuery] string id, [FromBody] TenantCommand command) : CommandHandler
{
    protected Entities.DbSchema.Tenant Data;

    public override async Task<IResult> Validate()
    {
        Data = appDb.Tenants.FirstOrDefault(p => p.Id == id);
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

    public override Entities.DbSchema.Tenant Response()
    {
        return new Entities.DbSchema.Tenant { Id = id, Name = Data.Name };
    }
}