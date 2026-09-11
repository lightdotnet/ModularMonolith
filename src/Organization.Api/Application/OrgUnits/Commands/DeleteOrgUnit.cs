using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.OrgUnits;

namespace StarterKit.Organization.Api.Application.OrgUnits.Commands;

internal sealed record DeleteOrgUnitCommand(string Id) : ICommand<IResult>;

internal class DeleteOrgUnitCommandHandler(OrganizationDbContext context)
    : ICommandHandler<DeleteOrgUnitCommand, IResult>
{
    public async Task<IResult> Handle(
        DeleteOrgUnitCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.OrgUnits
            .Where(new OrgUnitByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Org unit {request.Id} not found");

        var hasChildren = await context.OrgUnits.AnyAsync(x => x.ParentId == request.Id, cancellationToken);

        if (hasChildren)
            return Result.Error("Org unit still has sub-departments/teams. Remove them first.");

        var hasMembers = await context.EmployeeOrgUnitMemberships
            .AnyAsync(x => x.OrgUnitId == request.Id && x.EndDate == null, cancellationToken);

        if (hasMembers)
            return Result.Error("Org unit still has employees assigned. Remove them first.");

        // Any remaining rows here are already-ended memberships (historical only) — cascade
        // deletes them along with the unit rather than blocking, since they lose meaning once
        // the unit they reference is gone.
        context.OrgUnits.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
