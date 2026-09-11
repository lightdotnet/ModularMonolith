using Mapster;
using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.OrgUnits;

namespace StarterKit.Organization.Api.Application.OrgUnits.Queries;

internal sealed record GetOrgUnitManagersQuery(string OrgUnitId) : IQuery<IList<EmployeeDto>>;

internal class GetOrgUnitManagersQueryHandler(OrganizationDbContext context)
    : IQueryHandler<GetOrgUnitManagersQuery, IList<EmployeeDto>>
{
    public async Task<IList<EmployeeDto>> Handle(
        GetOrgUnitManagersQuery request,
        CancellationToken cancellationToken)
    {
        return await context.EmployeeOrgUnitMemberships
            .AsNoTracking()
            .Where(new ActiveOrgUnitManagersSpec(request.OrgUnitId))
            .Select(x => x.Employee)
            .ProjectToType<EmployeeDto>()
            .ToListAsync(cancellationToken);
    }
}
