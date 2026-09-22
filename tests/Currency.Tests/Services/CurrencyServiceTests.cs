using Currency.Tests.TestSupport;
using StarterKit.Currencies.Api.Services;
using StarterKit.Currencies.Contracts.Services;

namespace Currency.Tests.Services;

public class CurrencyServiceTests
{
    private static readonly DateTimeOffset Jan1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Feb1 = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Mar1 = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

    private static async Task<(CurrencyTestHost Host, CurrencyService Service)> CreateAsync()
    {
        var host = new CurrencyTestHost();
        await host.SeedCurrenciesAsync();

        return (host, new CurrencyService(host.Context));
    }

    [Fact]
    public async Task GetBaseCurrencyAsync_ShouldReturnTheBaseCurrency()
    {
        var (host, service) = await CreateAsync();
        using var _ = host;

        var result = await service.GetBaseCurrencyAsync();

        Assert.Equal(new CurrencyInfoDto("VND", 0, true, true), result);
    }

    [Fact]
    public async Task GetBaseCurrencyAsync_ShouldThrow_WhenNoBaseIsConfigured()
    {
        using var host = new CurrencyTestHost();

        await Assert.ThrowsAsync<InvalidOperationException>(() => new CurrencyService(host.Context).GetBaseCurrencyAsync());
    }

    [Fact]
    public async Task GetAsync_ShouldResolveByCaseInsensitiveCode_OrReturnNull()
    {
        var (host, service) = await CreateAsync();
        using var _ = host;

        var usd = await service.GetAsync("usd");

        Assert.Equal(new CurrencyInfoDto("USD", 2, true, false), usd);
        Assert.Null(await service.GetAsync("XYZ"));
    }

    [Fact]
    public async Task GetRateToBaseAsync_ShouldReturnRateOne_ForTheBaseCurrency()
    {
        var (host, service) = await CreateAsync();
        using var _ = host;

        var quote = await service.GetRateToBaseAsync("VND", Jan1);

        Assert.Equal(new ExchangeRateQuote("VND", "VND", 1m, null), quote);
    }

    [Fact]
    public async Task GetRateToBaseAsync_ShouldPickTheLatestRateAtOrBeforeAsOf()
    {
        var (host, service) = await CreateAsync();
        using var _ = host;
        await host.AddRateAsync("USD", 24_000m, Jan1);
        await host.AddRateAsync("USD", 25_000m, Feb1);
        await host.AddRateAsync("USD", 26_000m, Mar1);

        var between = await service.GetRateToBaseAsync("USD", Feb1.AddDays(10));
        var exactlyAtEffectiveFrom = await service.GetRateToBaseAsync("USD", Feb1);
        var justBefore = await service.GetRateToBaseAsync("USD", Feb1.AddSeconds(-1));
        var afterAll = await service.GetRateToBaseAsync("USD", Mar1.AddYears(1));

        Assert.Equal(25_000m, between.Rate);
        Assert.Equal(Feb1, between.EffectiveFrom);
        Assert.Equal("VND", between.BaseCurrencyCode);
        Assert.Equal("USD", between.CurrencyCode);
        Assert.Equal(25_000m, exactlyAtEffectiveFrom.Rate);
        Assert.Equal(24_000m, justBefore.Rate);
        Assert.Equal(26_000m, afterAll.Rate);
    }

    [Fact]
    public async Task GetRateToBaseAsync_ShouldIgnoreARateThatIsNotEffectiveYet()
    {
        var (host, service) = await CreateAsync();
        using var _ = host;
        await host.AddRateAsync("USD", 25_000m, Feb1);

        var ex = await Assert.ThrowsAsync<ExchangeRateNotFoundException>(() => service.GetRateToBaseAsync("USD", Jan1));

        Assert.Equal("USD", ex.CurrencyCode);
        Assert.Equal(Jan1, ex.AsOf);
        Assert.Contains("No exchange rate for USD as of", Assert.Single(ex.ValidationErrors["currency"]));
    }

    [Fact]
    public async Task GetRateToBaseAsync_ShouldThrow_WhenNoRateWasEverRecorded()
    {
        var (host, service) = await CreateAsync();
        using var _ = host;

        await Assert.ThrowsAsync<ExchangeRateNotFoundException>(() => service.GetRateToBaseAsync("JPY", Jan1));
    }

    [Fact]
    public async Task GetRateToBaseAsync_ShouldThrow_ForAnUnknownCurrency()
    {
        var (host, service) = await CreateAsync();
        using var _ = host;

        await Assert.ThrowsAsync<ExchangeRateNotFoundException>(() => service.GetRateToBaseAsync("XYZ", Jan1));
    }

    [Fact]
    public async Task GetRateToBaseAsync_ShouldThrow_ForAnInactiveCurrency_EvenWithARate()
    {
        var (host, service) = await CreateAsync();
        using var _ = host;
        await host.AddRateAsync("USD", 25_000m, Jan1);

        var usd = host.Context.Currencies.Single(x => x.Id == "USD");
        usd.Deactivate();
        await host.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<ExchangeRateNotFoundException>(() => service.GetRateToBaseAsync("USD", Feb1));
    }

    [Fact]
    public async Task GetRatesToBaseAsync_ShouldResolveDistinctCodesInInputOrder()
    {
        var (host, service) = await CreateAsync();
        using var _ = host;
        await host.AddRateAsync("USD", 25_000m, Jan1);
        await host.AddRateAsync("JPY", 170m, Jan1);

        var quotes = await service.GetRatesToBaseAsync(["jpy", "VND", "USD", "usd"], Feb1);

        Assert.Equal(["JPY", "VND", "USD"], quotes.Select(x => x.CurrencyCode));
        Assert.Equal([170m, 1m, 25_000m], quotes.Select(x => x.Rate));
    }

    [Fact]
    public async Task GetRatesToBaseAsync_ShouldThrow_WhenAnyCodeCannotBeResolved()
    {
        var (host, service) = await CreateAsync();
        using var _ = host;
        await host.AddRateAsync("USD", 25_000m, Jan1);

        var ex = await Assert.ThrowsAsync<ExchangeRateNotFoundException>(
            () => service.GetRatesToBaseAsync(["USD", "JPY"], Feb1));

        Assert.Equal("JPY", ex.CurrencyCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetRatesToBaseAsync_ShouldThrowAValidationException_ForANullOrBlankCode(string? code)
    {
        var (host, service) = await CreateAsync();
        using var _ = host;

        await Assert.ThrowsAsync<Light.Exceptions.ValidationException>(
            () => service.GetRatesToBaseAsync(["USD", code!], Jan1));
        await Assert.ThrowsAsync<Light.Exceptions.ValidationException>(
            () => service.GetRateToBaseAsync(code!, Jan1));
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_ForABlankCode()
    {
        var (host, service) = await CreateAsync();
        using var _ = host;

        Assert.Null(await service.GetAsync(" "));
    }

    [Fact]
    public async Task GetRatesToBaseAsync_ShouldReturnEmpty_ForNoCodes()
    {
        var (host, service) = await CreateAsync();
        using var _ = host;

        Assert.Empty(await service.GetRatesToBaseAsync([], Jan1));
    }

    [Fact]
    public void ExchangeRateNotFoundException_ShouldBeAValidationException_SoTheApiReturnsA4xx()
    {
        var ex = new ExchangeRateNotFoundException("USD", Jan1);

        Assert.IsAssignableFrom<Light.Exceptions.ValidationException>(ex);
    }
}
