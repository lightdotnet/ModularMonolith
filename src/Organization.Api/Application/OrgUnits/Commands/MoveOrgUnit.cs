using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.OrgUnits;

namespace StarterKit.Organization.Api.Application.OrgUnits.Commands;

internal sealed record MoveOrgUnitCommand(string Id, MoveOrgUnitRequest Model) : ICommand<IResult>;

internal class MoveOrgUnitCommandHandler(OrganizationDbContext context)
    : ICommandHandler<MoveOrgUnitCommand, IResult>
{
    public async Task<IResult> Handle(
        MoveOrgUnitCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.OrgUnits
            .Where(new OrgUnitByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Org unit {request.Id} not found");

        var newParentId = request.Model.NewParentId;

        if (string.IsNullOrEmpty(newParentId))
        {
            entity.ParentId = null;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        if (newParentId == entity.Id)
            return Result.Error("An org unit cannot be its own parent.");

        var newParent = await context.OrgUnits
            .Where(new OrgUnitByIdSpec(newParentId))
            .FirstOrDefaultAsync(cancellationToken);

        if (newParent is null)
            return Result.NotFound($"Org unit {newParentId} not found");

        if (newParent.CompanyId != entity.CompanyId)
            return Result.Error("Cannot move an org unit under a different company.");

        var isDescendant = await IsDescendantAsync(context, entity.Id, newParent.Id, cancellationToken);

        if (isDescendant)
            return Result.Error("Cannot move an org unit under one of its own descendants.");

        entity.ParentId = newParentId;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static async Task<bool> IsDescendantAsync(
        OrganizationDbContext context, string ancestorId, string candidateId, CancellationToken cancellationToken)
    {
        var currentId = candidateId;

        while (!string.IsNullOrEmpty(currentId))
        {
            if (currentId == ancestorId)
                return true;

            currentId = await context.OrgUnits
                .Where(new OrgUnitByIdSpec(currentId))
                .Select(x => x.ParentId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return false;
    }
}
