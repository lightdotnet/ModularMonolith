namespace StarterKit.Currencies.Contracts.Services;

/// <summary>
/// Cross-module, read-only seam onto the Currency module (DI-only; there is no HTTP surface for it).
/// The system has one base currency; every foreign amount is converted to it and a missing rate is an
/// error, never a silent 1:1.
/// </summary>
public interface ICurrencyService
{
    /// <summary>The single base currency; throws <see cref="InvalidOperationException"/> if none is configured.</summary>
    Task<CurrencyInfoDto> GetBaseCurrencyAsync(CancellationToken cancellationToken = default);

    /// <summary>Resolves a currency by ISO code (case-insensitive), or <c>null</c> when unknown.</summary>
    Task<CurrencyInfoDto?> GetAsync(
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the rate that applies at <paramref name="asOf"/>: the rate with the greatest
    /// <c>EffectiveFrom</c> that is not after <paramref name="asOf"/>. The base currency resolves to rate 1.
    /// Throws <see cref="ExchangeRateNotFoundException"/> when the currency is unknown or inactive, or no
    /// rate is effective yet at <paramref name="asOf"/>.
    /// </summary>
    Task<ExchangeRateQuote> GetRateToBaseAsync(
        string currencyCode,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch variant of <see cref="GetRateToBaseAsync"/>: one quote per distinct code (input order kept,
    /// codes compared case-insensitively); throws <see cref="ExchangeRateNotFoundException"/> for the first
    /// code that cannot be resolved.
    /// </summary>
    Task<IReadOnlyList<ExchangeRateQuote>> GetRatesToBaseAsync(
        IReadOnlyCollection<string> currencyCodes,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default);
}
