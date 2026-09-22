namespace StarterKit.Currencies.Contracts.Services;

/// <summary>
/// The rate resolved for a currency at a point in time: 1 unit of <see cref="CurrencyCode"/> =
/// <see cref="Rate"/> units of <see cref="BaseCurrencyCode"/>. For the base currency itself the rate is 1
/// and <see cref="EffectiveFrom"/> is <c>null</c> (no rate row backs an identity conversion).
/// </summary>
public sealed record ExchangeRateQuote(
    string CurrencyCode,
    string BaseCurrencyCode,
    decimal Rate,
    DateTimeOffset? EffectiveFrom);
