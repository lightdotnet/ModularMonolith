using Mapster;
using StarterKit.Locations.Api.Data;

namespace StarterKit.Locations.Api.Application.LocationTypes.Queries;

internal sealed record GetLocationTypesQuery : IQuery<IList<LocationTypeDto>>;

internal class GetLocationTypesQueryHandler(LocationDbContext context)
    : IQueryHandler<GetLocationTypesQuery, IList<LocationTypeDto>>
{
    public async Task<IList<LocationTypeDto>> Handle(
        GetLocationTypesQuery request,
        CancellationToken cancellationToken) =>
        await context.LocationTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ProjectToType<LocationTypeDto>()
            .ToListAsync(cancellationToken);
}
