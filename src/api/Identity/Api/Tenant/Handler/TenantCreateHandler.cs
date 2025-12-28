using Identity.Api.Organization.Command;
using Identity.Api.Tenant.Command;

namespace Identity.Api.Tenant.Handler;

[Authorize]
[Post("/api/tenant/create")]
public class TenantCreateHandler(AppDbContext appDb, [FromBody] TenantCommand command) : CommandHandler
{
    public Entities.DbSchema.Tenant Data { get; set; }

    public override async Task<IResult> Validate()
    {
        if (await appDb.Tenants.AnyAsync(p => p.Name == command.Name))
            AddError("Name", "The Tenant is already exist");

        return await Next();
    }

    [Pipeline(1)]
    public void Save()
    {
        Data = new Entities.DbSchema.Tenant
        {
            Id = Guid.NewGuid().ToString(),
            Code = command.Code,
            Name = command.Name,
            Description = command.Description,
            IsActive = true,
        };

        appDb.Tenants.Add(Data);
        appDb.SaveChanges();
    }
}

