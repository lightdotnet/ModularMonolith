namespace StarterKit.Currencies.Api.Domain.ExchangeRates;

public static class ExchangeRateQueryExtensions
{
    /// <summary>
    /// The rate that applies to <paramref name="currencyCode"/> at <paramref name="asOf"/>: the greatest
    /// <c>EffectiveFrom</c> that is not after <paramref name="asOf"/> (a rate is effective from its own
    /// instant, inclusive). Ties are impossible (unique per currency and effective date); the id
    /// tie-break only keeps the ordering deterministic.
    /// </summary>
    public static IQueryable<ExchangeRate> EffectiveAt(
        this IQueryable<ExchangeRate> rates,
        string currencyCode,
        DateTimeOffset asOf) =>
        rates
            .Where(x => x.CurrencyCode == currencyCode && x.EffectiveFrom <= asOf)
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.Id);
}
