using StarterKit.Locations.Api.Data;

namespace StarterKit.Locations.Api.Application.Locations.Queries;

internal sealed record GetLocationTreeQuery : IQuery<IList<LocationTreeNodeDto>>;

internal class GetLocationTreeQueryHandler(LocationDbContext context)
    : IQueryHandler<GetLocationTreeQuery, IList<LocationTreeNodeDto>>
{
    public async Task<IList<LocationTreeNodeDto>> Handle(
        GetLocationTreeQuery request,
        CancellationToken cancellationToken)
    {
        var locations = await context.Locations
            .AsNoTracking()
            .Select(x => new LocationTreeNodeDto
            {
                Id = x.Id,
                ParentLocationId = x.ParentLocationId,
                LocationTypeId = x.LocationTypeId,
                Name = x.Name,
                Code = x.Code,
                Status = x.Status,
            })
            .ToListAsync(cancellationToken);

        var byParent = locations
            .GroupBy(x => x.ParentLocationId ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var location in locations)
        {
            if (byParent.TryGetValue(location.Id, out var children))
                location.Children = children;
        }

        return byParent.TryGetValue(string.Empty, out var roots) ? roots : [];
    }
}
