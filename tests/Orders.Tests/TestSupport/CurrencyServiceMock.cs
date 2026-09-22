using Moq;
using StarterKit.Currencies.Contracts.Services;

namespace Orders.Tests.TestSupport;

/// <summary>
/// Builds a Moq double for the Currency module's seam: one base currency (VND, 0 decimals unless
/// overridden) that resolves to rate 1, plus whatever foreign rates the test registers. Any other code
/// throws <see cref="ExchangeRateNotFoundException"/> exactly like the real service, so a missing rate
/// is never silently treated as 1.
/// </summary>
internal static class CurrencyServiceMock
{
    public static readonly DateTimeOffset RateEffectiveFrom = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static Mock<ICurrencyService> Create(
        string baseCode = "VND",
        int baseDecimals = 0,
        IReadOnlyDictionary<string, decimal>? rates = null)
    {
        rates ??= new Dictionary<string, decimal>();

        var mock = new Mock<ICurrencyService>();

        mock.Setup(x => x.GetBaseCurrencyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurrencyInfoDto(baseCode, baseDecimals, true, true));

        mock.Setup(x => x.GetRateToBaseAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                string code,
                DateTimeOffset asOf,
                CancellationToken _) =>
            {
                if (code == baseCode)
                    return new ExchangeRateQuote(code, baseCode, 1m, null);

                if (rates.TryGetValue(code, out var rate))
                    return new ExchangeRateQuote(code, baseCode, rate, RateEffectiveFrom);

                throw new ExchangeRateNotFoundException(code, asOf);
            });

        return mock;
    }
}
