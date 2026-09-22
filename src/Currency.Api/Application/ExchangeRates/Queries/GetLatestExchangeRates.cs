using StarterKit.Currencies.Api.Data;
using StarterKit.Currencies.Api.Domain.ExchangeRates;
using StarterKit.Shared;

namespace StarterKit.Currencies.Api.Application.ExchangeRates.Queries;

internal sealed record GetLatestExchangeRatesQuery(DateTimeOffset? AsOf) : IQuery<IList<ExchangeRateDto>>;

/// <summary>
/// The rate currently in effect for each active foreign currency (as of now, or the supplied instant).
/// Currencies with no rate effective yet are omitted; a future-dated rate is not "latest" until it takes effect.
/// </summary>
internal class GetLatestExchangeRatesQueryHandler(
    CurrencyDbContext context,
    IDateTime clock)
    : IQueryHandler<GetLatestExchangeRatesQuery, IList<ExchangeRateDto>>
{
    public async Task<IList<ExchangeRateDto>> Handle(
        GetLatestExchangeRatesQuery request,
        CancellationToken cancellationToken)
    {
        var asOf = request.AsOf ?? clock.UtcNow;

        var codes = await context.Currencies
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsBase)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var result = new List<ExchangeRateDto>(codes.Count);

        foreach (var code in codes)
        {
            var rate = await context.ExchangeRates
                .AsNoTracking()
                .EffectiveAt(code, asOf)
                .FirstOrDefaultAsync(cancellationToken);

            if (rate is not null)
                result.Add(SearchExchangeRatesQueryHandler.ToDto(rate));
        }

        return result;
    }
}
