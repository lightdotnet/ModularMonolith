using Mapster;
using StarterKit.Organization.Api.Data;

namespace StarterKit.Organization.Api.Application.OrgUnits.Queries;

internal sealed record GetOrgUnitEmployeesQuery(string OrgUnitId) : IQuery<IList<EmployeeDto>>;

internal class GetOrgUnitEmployeesQueryHandler(OrganizationDbContext context)
    : IQueryHandler<GetOrgUnitEmployeesQuery, IList<EmployeeDto>>
{
    public async Task<IList<EmployeeDto>> Handle(
        GetOrgUnitEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        return await context.EmployeeOrgUnitMemberships
            .AsNoTracking()
            .Where(x => x.OrgUnitId == request.OrgUnitId && x.EndDate == null)
            .Select(x => x.Employee)
            .ProjectToType<EmployeeDto>()
            .ToListAsync(cancellationToken);
    }
}
