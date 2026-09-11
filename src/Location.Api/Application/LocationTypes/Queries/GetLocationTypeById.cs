using Mapster;
using StarterKit.Locations.Api.Data;
using StarterKit.Locations.Api.Domain.LocationTypes;

namespace StarterKit.Locations.Api.Application.LocationTypes.Queries;

internal sealed record GetLocationTypeByIdQuery(string Id) : IQuery<IResult<LocationTypeDto>>;

internal class GetLocationTypeByIdQueryHandler(LocationDbContext context)
    : IQueryHandler<GetLocationTypeByIdQuery, IResult<LocationTypeDto>>
{
    public async Task<IResult<LocationTypeDto>> Handle(
        GetLocationTypeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await context.LocationTypes
            .AsNoTracking()
            .Where(new LocationTypeByIdSpec(request.Id))
            .ProjectToType<LocationTypeDto>()
            .SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
            return Result<LocationTypeDto>.NotFound($"Location type {request.Id} not found");

        return Result<LocationTypeDto>.Success(dto);
    }
}
