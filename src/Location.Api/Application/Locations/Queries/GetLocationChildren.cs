using StarterKit.Locations.Api.Data;

namespace StarterKit.Locations.Api.Application.Locations.Queries;

internal sealed record GetLocationChildrenQuery(string Id) : IQuery<IList<LocationLookupDto>>;

internal class GetLocationChildrenQueryHandler(LocationDbContext context)
    : IQueryHandler<GetLocationChildrenQuery, IList<LocationLookupDto>>
{
    public async Task<IList<LocationLookupDto>> Handle(
        GetLocationChildrenQuery request,
        CancellationToken cancellationToken) =>
        await context.Locations
            .AsNoTracking()
            .Where(x => x.ParentLocationId == request.Id)
            .Select(x => new LocationLookupDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                LocationTypeId = x.LocationTypeId,
            })
            .ToListAsync(cancellationToken);
}
