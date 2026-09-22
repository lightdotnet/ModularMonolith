using Light.Exceptions;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Currencies.Contracts.Services;

/// <summary>
/// Thrown when a currency cannot be converted to the base currency: no rate is effective at the requested
/// time, or the currency is unknown/inactive. A missing rate is always an error, never a silent 1:1
/// fallback. Being a <see cref="ValidationException"/>, it surfaces to API callers as a clear 4xx message.
/// </summary>
public sealed class ExchangeRateNotFoundException : ValidationException
{
    public ExchangeRateNotFoundException(
        string currencyCode,
        DateTimeOffset asOf,
        string? reason = null)
        : base(BuildErrors(currencyCode, asOf, reason))
    {
        CurrencyCode = currencyCode;
        AsOf = asOf;
    }

    public string CurrencyCode { get; }

    public DateTimeOffset AsOf { get; }

    private static Dictionary<string, string[]> BuildErrors(
        string currencyCode,
        DateTimeOffset asOf,
        string? reason)
    {
        var message = $"No exchange rate for {currencyCode} as of {asOf:yyyy-MM-dd HH:mm:ss zzz}.";

        if (!string.IsNullOrEmpty(reason))
            message = $"{message} {reason}";

        return new Dictionary<string, string[]> { ["currency"] = [message] };
    }
}
