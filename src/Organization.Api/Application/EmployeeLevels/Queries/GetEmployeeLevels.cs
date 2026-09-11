using Mapster;
using StarterKit.Organization.Api.Data;

namespace StarterKit.Organization.Api.Application.EmployeeLevels.Queries;

internal sealed record GetEmployeeLevelsQuery(string CompanyId) : IQuery<IList<EmployeeLevelDto>>;

internal class GetEmployeeLevelsQueryHandler(OrganizationDbContext context)
    : IQueryHandler<GetEmployeeLevelsQuery, IList<EmployeeLevelDto>>
{
    public async Task<IList<EmployeeLevelDto>> Handle(
        GetEmployeeLevelsQuery request,
        CancellationToken cancellationToken)
    {
        return await context.EmployeeLevels
            .AsNoTracking()
            .Where(x => x.CompanyId == request.CompanyId)
            .OrderBy(x => x.Rank)
            .ProjectToType<EmployeeLevelDto>()
            .ToListAsync(cancellationToken);
    }
}
