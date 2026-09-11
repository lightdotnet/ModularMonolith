using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.Services;

namespace StarterKit.Organization.Api.Services;

internal class OrgDirectoryService(OrganizationDbContext context) : IOrgDirectoryService
{
    public async Task<IReadOnlyList<ResolvedApproverDto>> GetApproverCandidatesAsync(
        string employeeId,
        CancellationToken cancellationToken = default)
    {
        var orgUnitId = await context.EmployeeOrgUnitMemberships
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.IsPrimary && x.EndDate == null)
            .Select(x => (string?)x.OrgUnitId)
            .FirstOrDefaultAsync(cancellationToken);

        while (orgUnitId is not null)
        {
            var candidates = await ResolveCandidatesInOrgUnitAsync(orgUnitId, employeeId, cancellationToken);

            if (candidates.Count > 0)
                return candidates;

            orgUnitId = await context.OrgUnits
                .AsNoTracking()
                .Where(x => x.Id == orgUnitId)
                .Select(x => x.ParentId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return [];
    }

    public Task<string?> GetEmployeeNameAsync(
        string employeeId,
        CancellationToken cancellationToken = default) =>
        context.Employees
            .AsNoTracking()
            .Where(x => x.Id == employeeId)
            .Select(x => (string?)(x.FirstName + " " + x.LastName))
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<List<ResolvedApproverDto>> ResolveCandidatesInOrgUnitAsync(
        string orgUnitId,
        string excludeEmployeeId,
        CancellationToken cancellationToken)
    {
        var candidates = context.EmployeeOrgUnitMemberships
            .AsNoTracking()
            .Where(x =>
                x.OrgUnitId == orgUnitId
                && x.EndDate == null
                && x.EmployeeId != excludeEmployeeId
                && x.Employee.UserId != null
                && x.Employee.EmploymentStatus == EmploymentStatus.Active);

        var managers = await ListAsync(candidates.Where(x => x.IsManager), cancellationToken);

        return managers.Count > 0 ? managers : await ListAsync(candidates, cancellationToken);
    }

    private static Task<List<ResolvedApproverDto>> ListAsync(
        IQueryable<EmployeeOrgUnitMembership> candidates,
        CancellationToken cancellationToken) =>
        candidates
            .OrderByDescending(x => x.Level != null ? x.Level!.Rank : int.MinValue)
            .ThenBy(x => x.EmployeeId)
            .Select(x => new ResolvedApproverDto
            {
                EmployeeId = x.EmployeeId,
                UserId = x.Employee.UserId!,
                Name = x.Employee.FirstName + " " + x.Employee.LastName,
            })
            .ToListAsync(cancellationToken);
}
