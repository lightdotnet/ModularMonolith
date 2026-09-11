using Mapster;
using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.Companies;

namespace StarterKit.Organization.Api.Application.Companies.Queries;

internal sealed record GetCompanyByIdQuery(string Id) : IQuery<IResult<CompanyDto>>;

internal class GetCompanyByIdQueryHandler(OrganizationDbContext context)
    : IQueryHandler<GetCompanyByIdQuery, IResult<CompanyDto>>
{
    public async Task<IResult<CompanyDto>> Handle(
        GetCompanyByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await context.Companies
            .AsNoTracking()
            .Where(new CompanyByIdSpec(request.Id))
            .ProjectToType<CompanyDto>()
            .SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
            return Result<CompanyDto>.NotFound($"Company {request.Id} not found");

        return Result<CompanyDto>.Success(dto);
    }
}
