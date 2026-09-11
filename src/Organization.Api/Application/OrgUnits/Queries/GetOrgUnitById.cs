using Mapster;
using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.OrgUnits;

namespace StarterKit.Organization.Api.Application.OrgUnits.Queries;

internal sealed record GetOrgUnitByIdQuery(string Id) : IQuery<IResult<OrgUnitDto>>;

internal class GetOrgUnitByIdQueryHandler(OrganizationDbContext context)
    : IQueryHandler<GetOrgUnitByIdQuery, IResult<OrgUnitDto>>
{
    public async Task<IResult<OrgUnitDto>> Handle(
        GetOrgUnitByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await context.OrgUnits
            .AsNoTracking()
            .Where(new OrgUnitByIdSpec(request.Id))
            .ProjectToType<OrgUnitDto>()
            .SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
            return Result<OrgUnitDto>.NotFound($"Org unit {request.Id} not found");

        return Result<OrgUnitDto>.Success(dto);
    }
}
