using StarterKit.Currencies.Api.Data;
using StarterKit.Currencies.Api.Domain.Currencies;
using StarterKit.Currencies.Api.Domain.ExchangeRates;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Currencies.Api.Application.ExchangeRates.Queries;

internal sealed record SearchExchangeRatesQuery(SearchExchangeRateRequest Request) : IQuery<PagedResult<ExchangeRateDto>>;

/// <summary>The rate history, newest effective date first, optionally filtered by currency and date range.</summary>
internal class SearchExchangeRatesQueryHandler(CurrencyDbContext context)
    : IQueryHandler<SearchExchangeRatesQuery, PagedResult<ExchangeRateDto>>
{
    public async Task<PagedResult<ExchangeRateDto>> Handle(
        SearchExchangeRatesQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        var scoped = context.ExchangeRates
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(lookup.CurrencyCode))
        {
            var code = Currency.NormalizeCode(lookup.CurrencyCode);

            scoped = scoped.Where(x => x.CurrencyCode == code);
        }

        if (lookup.From.HasValue)
            scoped = scoped.Where(x => x.EffectiveFrom >= lookup.From.Value);

        if (lookup.To.HasValue)
            scoped = scoped.Where(x => x.EffectiveFrom <= lookup.To.Value);

        var paged = await scoped
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.Id)
            .ToPagedAsync(lookup, cancellationToken);

        var items = paged.Records
            .Select(ToDto)
            .ToList();

        return new PagedResult<ExchangeRateDto>(items, paged.PageNumber, paged.PageSize, paged.TotalRecords);
    }

    internal static ExchangeRateDto ToDto(ExchangeRate entity) => new()
    {
        Id = entity.Id,
        CurrencyCode = entity.CurrencyCode,
        Rate = entity.Rate,
        EffectiveFrom = entity.EffectiveFrom,
        RecordedBy = entity.RecordedBy,
        Note = entity.Note,
    };
}
