using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Shared;

namespace StarterKit.Organization.Api.Application.Employees.Commands;

internal sealed record AssignEmployeeToOrgUnitCommand(string EmployeeId, AssignEmployeeOrgUnitRequest Model)
    : ICommand<IResult>;

internal class AssignEmployeeToOrgUnitCommandHandler(OrganizationDbContext context, IDateTime clock)
    : ICommandHandler<AssignEmployeeToOrgUnitCommand, IResult>
{
    public async Task<IResult> Handle(
        AssignEmployeeToOrgUnitCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await context.Employees
            .Where(new EmployeeByIdSpec(request.EmployeeId))
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null)
            return Result.NotFound($"Employee {request.EmployeeId} not found");

        var model = request.Model;

        var orgUnit = await context.OrgUnits
            .Where(new OrgUnitByIdSpec(model.OrgUnitId))
            .FirstOrDefaultAsync(cancellationToken);

        if (orgUnit is null)
            return Result.NotFound($"Org unit {model.OrgUnitId} not found");

        if (orgUnit.CompanyId != employee.CompanyId)
            return Result.Error("Org unit belongs to a different company than the employee.");

        if (!string.IsNullOrEmpty(model.LevelId))
        {
            var levelExists = await context.EmployeeLevels
                .AnyAsync(x => x.Id == model.LevelId && x.CompanyId == employee.CompanyId, cancellationToken);

            if (!levelExists)
                return Result.NotFound($"Employee level {model.LevelId} not found");
        }

        var alreadyAssigned = await context.EmployeeOrgUnitMemberships
            .Where(new ActiveEmployeeOrgUnitMembershipSpec(request.EmployeeId, model.OrgUnitId))
            .AnyAsync(cancellationToken);

        if (alreadyAssigned)
            return Result.Error("Employee is already assigned to this org unit.");

        if (model.IsPrimary)
            await ClearPrimaryAsync(request.EmployeeId, cancellationToken);

        var membership = new EmployeeOrgUnitMembership
        {
            EmployeeId = request.EmployeeId,
            OrgUnitId = model.OrgUnitId,
            LevelId = model.LevelId,
            IsPrimary = model.IsPrimary,
            AssignmentType = model.AssignmentType,
            IsManager = model.IsManager,
            StartDate = clock.UtcNow,
        };

        await context.EmployeeOrgUnitMemberships.AddAsync(membership, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task ClearPrimaryAsync(string employeeId, CancellationToken cancellationToken)
    {
        await context.EmployeeOrgUnitMemberships
            .Where(x => x.EmployeeId == employeeId && x.EndDate == null && x.IsPrimary)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.IsPrimary, false), cancellationToken);
    }
}
