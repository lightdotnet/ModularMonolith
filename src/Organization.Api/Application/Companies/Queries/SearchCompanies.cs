using Light.EntityFrameworkCore.Extensions;
using Light.Specification;
using Mapster;
using StarterKit.Organization.Api.Data;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Organization.Api.Application.Companies.Queries;

internal sealed record SearchCompaniesQuery(CompanySearchRequest Request) : IQuery<PagedResult<CompanyDto>>;

internal class SearchCompaniesQueryHandler(OrganizationDbContext context)
    : IQueryHandler<SearchCompaniesQuery, PagedResult<CompanyDto>>
{
    public Task<PagedResult<CompanyDto>> Handle(
        SearchCompaniesQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        return context.Companies
            .AsNoTracking()
            .WhereIf(!string.IsNullOrEmpty(lookup.SearchValue),
                x => x.Name.Contains(lookup.SearchValue!) || x.Code.Contains(lookup.SearchValue!))
            .WhereIf(lookup.Status.HasValue, x => x.Status == lookup.Status!.Value)
            .OrderBy(x => x.Name)
            .ProjectToType<CompanyDto>()
            .ToPagedResultAsync(lookup);
    }
}
