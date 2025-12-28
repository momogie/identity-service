using Identity.Api.Organization.Command;
using Identity.Api.Tenant.Command;

namespace Identity.Api.Organization.Handler;

[Authorize]
[Post("/api/organization/create")]
public class OrganizationCreateHandler(AppDbContext appDb, [FromBody] OrganizationCommand command) : CommandHandler
{
    public Entities.DbSchema.Organization Data { get; set; }

    public override async Task<IResult> Validate()
    {
        if (await appDb.Organizations.AnyAsync(p => p.Name == command.Name))
            AddError("Name", "The Tenant is already exist");

        return await Next();
    }

    [Pipeline(1)]
    public void Save()
    {
        Data = new Entities.DbSchema.Organization
        {
            Id = Guid.NewGuid().ToString(),
            Code = command.Code,
            Name = command.Name,
            Description = command.Description,
            IsActive = true,
        };

        appDb.Organizations.Add(Data);
        appDb.SaveChanges();
    }
}

