using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.OrgUnits;

namespace StarterKit.Organization.Api.Application.Employees.Commands;

internal sealed record UpdateEmployeeMembershipCommand(
    string EmployeeId, string OrgUnitId, UpdateEmployeeMembershipRequest Model) : ICommand<IResult>;

internal class UpdateEmployeeMembershipCommandHandler(OrganizationDbContext context)
    : ICommandHandler<UpdateEmployeeMembershipCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateEmployeeMembershipCommand request,
        CancellationToken cancellationToken)
    {
        var membership = await context.EmployeeOrgUnitMemberships
            .Where(new ActiveEmployeeOrgUnitMembershipSpec(request.EmployeeId, request.OrgUnitId))
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is null)
            return Result.NotFound("Membership not found");

        var model = request.Model;

        if (!string.IsNullOrEmpty(model.LevelId))
        {
            var levelExists = await context.EmployeeLevels.AnyAsync(x => x.Id == model.LevelId, cancellationToken);

            if (!levelExists)
                return Result.NotFound($"Employee level {model.LevelId} not found");
        }

        if (model.IsPrimary && !membership.IsPrimary)
        {
            await context.EmployeeOrgUnitMemberships
                .Where(x => x.EmployeeId == request.EmployeeId && x.EndDate == null && x.IsPrimary)
                .ExecuteUpdateAsync(u => u.SetProperty(p => p.IsPrimary, false), cancellationToken);
        }

        membership.LevelId = model.LevelId;
        membership.IsPrimary = model.IsPrimary;
        membership.AssignmentType = model.AssignmentType;
        membership.IsManager = model.IsManager;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
