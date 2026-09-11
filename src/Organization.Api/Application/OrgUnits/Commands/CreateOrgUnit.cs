using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.OrgUnits;

namespace StarterKit.Organization.Api.Application.OrgUnits.Commands;

internal sealed record CreateOrgUnitCommand(CreateOrgUnitRequest Model) : ICommand<IResult<string>>;

internal class CreateOrgUnitCommandHandler(OrganizationDbContext context)
    : ICommandHandler<CreateOrgUnitCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateOrgUnitCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var companyExists = await context.Companies.AnyAsync(x => x.Id == model.CompanyId, cancellationToken);

        if (!companyExists)
            return Result<string>.NotFound($"Company {model.CompanyId} not found");

        if (!string.IsNullOrEmpty(model.ParentId))
        {
            var parent = await context.OrgUnits
                .Where(new OrgUnitByIdSpec(model.ParentId))
                .FirstOrDefaultAsync(cancellationToken);

            if (parent is null)
                return Result<string>.NotFound($"Parent org unit {model.ParentId} not found");

            if (parent.CompanyId != model.CompanyId)
                return Result<string>.Error("Parent org unit belongs to a different company.");
        }

        var codeExists = await context.OrgUnits
            .AnyAsync(x => x.CompanyId == model.CompanyId && x.Code == model.Code, cancellationToken);

        if (codeExists)
            return Result<string>.Error($"Org unit code '{model.Code}' already exists in this company.");

        var entity = new OrgUnit
        {
            CompanyId = model.CompanyId,
            ParentId = model.ParentId,
            Type = model.Type,
            Name = model.Name,
            Code = model.Code,
            ManagerEmployeeId = model.ManagerEmployeeId,
            Description = model.Description,
        };

        await context.OrgUnits.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(entity.Id);
    }
}
