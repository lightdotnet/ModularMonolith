using Mapster;
using StarterKit.Locations.Api.Data;
using StarterKit.Locations.Api.Domain.Locations;

namespace StarterKit.Locations.Api.Application.Locations.Queries;

internal sealed record GetLocationByIdQuery(string Id) : IQuery<IResult<LocationDto>>;

internal class GetLocationByIdQueryHandler(LocationDbContext context)
    : IQueryHandler<GetLocationByIdQuery, IResult<LocationDto>>
{
    public async Task<IResult<LocationDto>> Handle(
        GetLocationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await context.Locations
            .AsNoTracking()
            .Where(new LocationByIdSpec(request.Id))
            .ProjectToType<LocationDto>()
            .SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
            return Result<LocationDto>.NotFound($"Location {request.Id} not found");

        return Result<LocationDto>.Success(dto);
    }
}
