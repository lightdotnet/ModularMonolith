using StarterKit.Organization.Api.Data;

namespace StarterKit.Organization.Api.Application.OrgUnits.Queries;

internal sealed record GetOrgUnitTreeQuery(string CompanyId) : IQuery<IList<OrgUnitTreeNodeDto>>;

internal class GetOrgUnitTreeQueryHandler(OrganizationDbContext context)
    : IQueryHandler<GetOrgUnitTreeQuery, IList<OrgUnitTreeNodeDto>>
{
    public async Task<IList<OrgUnitTreeNodeDto>> Handle(
        GetOrgUnitTreeQuery request,
        CancellationToken cancellationToken)
    {
        var units = await context.OrgUnits
            .AsNoTracking()
            .Where(x => x.CompanyId == request.CompanyId)
            .Select(x => new OrgUnitTreeNodeDto
            {
                Id = x.Id,
                ParentId = x.ParentId,
                Type = x.Type,
                Name = x.Name,
                Code = x.Code,
                ManagerEmployeeId = x.ManagerEmployeeId,
                Status = x.Status,
            })
            .ToListAsync(cancellationToken);

        var byParent = units
            .GroupBy(x => x.ParentId ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var unit in units)
        {
            if (byParent.TryGetValue(unit.Id, out var children))
                unit.Children = children;
        }

        return byParent.TryGetValue(string.Empty, out var roots) ? roots : [];
    }
}
