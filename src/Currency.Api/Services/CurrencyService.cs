using ValidationException = Light.Exceptions.ValidationException;
using StarterKit.Currencies.Api.Data;
using StarterKit.Currencies.Api.Domain.Currencies;
using StarterKit.Currencies.Api.Domain.ExchangeRates;
using StarterKit.Currencies.Contracts.Services;

namespace StarterKit.Currencies.Api.Services;

/// <summary>
/// Read-only seam over the Currency module. A missing rate is never papered over: an unknown/inactive
/// currency or one with no rate effective yet at the requested time throws
/// <see cref="ExchangeRateNotFoundException"/>.
/// </summary>
internal class CurrencyService(CurrencyDbContext context) : ICurrencyService
{
    public async Task<CurrencyInfoDto> GetBaseCurrencyAsync(CancellationToken cancellationToken = default)
    {
        var baseCurrency = await context.Currencies
            .AsNoTracking()
            .Where(x => x.IsBase)
            .FirstOrDefaultAsync(cancellationToken);

        return baseCurrency is null
            ? throw new InvalidOperationException("No base currency is configured.")
            : ToInfo(baseCurrency);
    }

    public async Task<CurrencyInfoDto?> GetAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        // A blank code cannot name a currency; treat it like any other unknown code.
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var normalized = Currency.NormalizeCode(code);

        var currency = await context.Currencies
            .AsNoTracking()
            .Where(x => x.Id == normalized)
            .FirstOrDefaultAsync(cancellationToken);

        return currency is null ? null : ToInfo(currency);
    }

    public async Task<ExchangeRateQuote> GetRateToBaseAsync(
        string currencyCode,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default)
    {
        var quotes = await GetRatesToBaseAsync([currencyCode], asOf, cancellationToken);

        return quotes[0];
    }

    public async Task<IReadOnlyList<ExchangeRateQuote>> GetRatesToBaseAsync(
        IReadOnlyCollection<string> currencyCodes,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currencyCodes);

        if (currencyCodes.Any(string.IsNullOrWhiteSpace))
        {
            throw new ValidationException(
                new Dictionary<string, string[]> { ["currencyCodes"] = ["Currency codes cannot be null or blank."] });
        }

        var codes = currencyCodes
            .Select(x => Currency.NormalizeCode(x))
            .Distinct()
            .ToList();

        if (codes.Count == 0)
            return [];

        var baseCurrency = await GetBaseCurrencyAsync(cancellationToken);

        var currencies = await context.Currencies
            .AsNoTracking()
            .Where(x => codes.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var quotes = new List<ExchangeRateQuote>(codes.Count);

        foreach (var code in codes)
        {
            if (code == baseCurrency.Code)
            {
                quotes.Add(new ExchangeRateQuote(code, baseCurrency.Code, 1m, EffectiveFrom: null));
                continue;
            }

            if (!currencies.TryGetValue(code, out var currency))
                throw new ExchangeRateNotFoundException(code, asOf, "The currency is not defined.");

            if (!currency.IsActive)
                throw new ExchangeRateNotFoundException(code, asOf, "The currency is inactive.");

            // One small indexed lookup per requested currency (a handful at most).
            var rate = await context.ExchangeRates
                .AsNoTracking()
                .EffectiveAt(code, asOf)
                .FirstOrDefaultAsync(cancellationToken);

            if (rate is null)
                throw new ExchangeRateNotFoundException(code, asOf);

            quotes.Add(new ExchangeRateQuote(code, baseCurrency.Code, rate.Rate, rate.EffectiveFrom));
        }

        return quotes;
    }

    private static CurrencyInfoDto ToInfo(Currency currency) =>
        new(
            currency.Code,
            currency.DecimalPlaces,
            currency.IsActive,
            currency.IsBase);
}
