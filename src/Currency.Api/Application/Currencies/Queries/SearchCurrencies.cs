using StarterKit.Currencies.Api.Data;
using StarterKit.Currencies.Api.Domain.Currencies;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Currencies.Api.Application.Currencies.Queries;

internal sealed record SearchCurrenciesQuery(SearchCurrencyRequest Request) : IQuery<PagedResult<CurrencyDto>>;

internal class SearchCurrenciesQueryHandler(CurrencyDbContext context)
    : IQueryHandler<SearchCurrenciesQuery, PagedResult<CurrencyDto>>
{
    public async Task<PagedResult<CurrencyDto>> Handle(
        SearchCurrenciesQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        var scoped = context.Currencies
            .AsNoTracking()
            .AsQueryable();

        if (lookup.IsActive.HasValue)
            scoped = scoped.Where(x => x.IsActive == lookup.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(lookup.SearchValue))
        {
            var term = lookup.SearchValue.Trim();

            scoped = scoped.Where(x => x.Id.Contains(term.ToUpperInvariant()) || x.Name.Contains(term));
        }

        // Base currency first, then by code.
        var paged = await scoped
            .OrderByDescending(x => x.IsBase)
            .ThenBy(x => x.Id)
            .ToPagedAsync(lookup, cancellationToken);

        var items = paged.Records
            .Select(GetCurrencyByCodeQueryHandler.ToDto)
            .ToList();

        return new PagedResult<CurrencyDto>(items, paged.PageNumber, paged.PageSize, paged.TotalRecords);
    }
}
