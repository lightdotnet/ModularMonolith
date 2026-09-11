using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.OrgUnits;

namespace StarterKit.Organization.Api.Application.OrgUnits.Commands;

internal sealed record UpdateOrgUnitCommand(OrgUnitDto Model) : ICommand<IResult>;

internal class UpdateOrgUnitCommandHandler(OrganizationDbContext context)
    : ICommandHandler<UpdateOrgUnitCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateOrgUnitCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var entity = await context.OrgUnits
            .Where(new OrgUnitByIdSpec(model.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Org unit {model.Id} not found");

        var codeTaken = await context.OrgUnits
            .AnyAsync(x => x.Id != model.Id && x.CompanyId == entity.CompanyId && x.Code == model.Code, cancellationToken);

        if (codeTaken)
            return Result.Error($"Org unit code '{model.Code}' already exists in this company.");

        entity.Type = model.Type;
        entity.Name = model.Name;
        entity.Code = model.Code;
        entity.ManagerEmployeeId = model.ManagerEmployeeId;
        entity.Description = model.Description;
        entity.Status = model.Status;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
