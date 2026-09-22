using Currency.Tests.TestSupport;
using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using StarterKit.Currencies.Api.Application.Currencies.Commands;
using StarterKit.Currencies.Api.Application.Currencies.Queries;
using StarterKit.Currencies.Api.Application.ExchangeRates.Commands;
using StarterKit.Currencies.Api.Application.ExchangeRates.Queries;
using StarterKit.Currencies.Contracts.Currencies;
using StarterKit.Currencies.Contracts.ExchangeRates;

namespace Currency.Tests.Application;

public class CurrencyHandlerTests
{
    private static readonly DateTimeOffset Jan1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Feb1 = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    private static async Task<CurrencyTestHost> SeededAsync()
    {
        var host = new CurrencyTestHost();
        await host.SeedCurrenciesAsync();

        return host;
    }

    [Fact]
    public async Task CreateCurrency_ShouldCreateANonBaseCurrency_AndRejectADuplicate()
    {
        using var host = new CurrencyTestHost();
        var sut = new CreateCurrencyCommandHandler(host.Context);
        var request = new CreateCurrencyRequest { Code = "eur", Name = "Euro", Symbol = "E", DecimalPlaces = 2 };

        var created = await sut.Handle(new CreateCurrencyCommand(request), CancellationToken.None);
        var duplicate = await sut.Handle(new CreateCurrencyCommand(request), CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.Equal("EUR", created.Data);
        Assert.False(duplicate.IsSuccess);
        var stored = await host.Context.Currencies.SingleAsync();
        Assert.False(stored.IsBase);
    }

    [Fact]
    public async Task UpdateCurrency_ShouldChangeNameSymbolAndDecimalPlaces()
    {
        using var host = await SeededAsync();
        var sut = new UpdateCurrencyCommandHandler(host.Context);

        var result = await sut.Handle(
            new UpdateCurrencyCommand("usd", new UpdateCurrencyRequest { Name = "Dollar", Symbol = "US$", DecimalPlaces = 3 }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var usd = await host.Context.Currencies.SingleAsync(x => x.Id == "USD");
        Assert.Equal("Dollar", usd.Name);
        Assert.Equal("US$", usd.Symbol);
        Assert.Equal(3, usd.DecimalPlaces);
    }

    [Fact]
    public async Task UpdateCurrency_ShouldReturnNotFound_ForAnUnknownCode()
    {
        using var host = await SeededAsync();
        var sut = new UpdateCurrencyCommandHandler(host.Context);

        var result = await sut.Handle(
            new UpdateCurrencyCommand("XYZ", new UpdateCurrencyRequest { Name = "x", DecimalPlaces = 0 }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ChangeStatus_ShouldDeactivateAndActivate_ButNeverDeactivateTheBase()
    {
        using var host = await SeededAsync();
        var sut = new ChangeCurrencyStatusCommandHandler(host.Context);

        await sut.Handle(new ChangeCurrencyStatusCommand("USD", Activate: false), CancellationToken.None);
        Assert.False((await host.Context.Currencies.SingleAsync(x => x.Id == "USD")).IsActive);

        await sut.Handle(new ChangeCurrencyStatusCommand("USD", Activate: true), CancellationToken.None);
        Assert.True((await host.Context.Currencies.SingleAsync(x => x.Id == "USD")).IsActive);

        await Assert.ThrowsAsync<ConflictException>(
            () => sut.Handle(new ChangeCurrencyStatusCommand("VND", Activate: false), CancellationToken.None));
    }

    [Fact]
    public async Task SearchCurrencies_ShouldListTheBaseFirst_AndFilterByActive()
    {
        using var host = await SeededAsync();
        var jpy = await host.Context.Currencies.SingleAsync(x => x.Id == "JPY");
        jpy.Deactivate();
        await host.Context.SaveChangesAsync();
        var sut = new SearchCurrenciesQueryHandler(host.Context);

        var all = await sut.Handle(new SearchCurrenciesQuery(new SearchCurrencyRequest()), CancellationToken.None);
        var active = await sut.Handle(new SearchCurrenciesQuery(new SearchCurrencyRequest { IsActive = true }), CancellationToken.None);
        var byName = await sut.Handle(new SearchCurrenciesQuery(new SearchCurrencyRequest { SearchValue = "dollar" }), CancellationToken.None);

        Assert.Equal(["VND", "JPY", "USD"], all.Data.Records.Select(x => x.Code));
        Assert.Equal(["VND", "USD"], active.Data.Records.Select(x => x.Code));
        Assert.Equal(["USD"], byName.Data.Records.Select(x => x.Code));
    }

    [Fact]
    public async Task GetCurrencyByCode_ShouldReturnTheCurrency_OrNotFound()
    {
        using var host = await SeededAsync();
        var sut = new GetCurrencyByCodeQueryHandler(host.Context);

        var found = await sut.Handle(new GetCurrencyByCodeQuery("usd"), CancellationToken.None);
        var missing = await sut.Handle(new GetCurrencyByCodeQuery("XYZ"), CancellationToken.None);

        Assert.True(found.IsSuccess);
        Assert.Equal("USD", found.Data!.Code);
        Assert.False(missing.IsSuccess);
    }

    [Fact]
    public async Task RecordExchangeRate_ShouldAppendARate_AndRejectADuplicateInstant()
    {
        using var host = await SeededAsync();
        var sut = new RecordExchangeRateCommandHandler(host.Context);
        var request = new RecordExchangeRateRequest { CurrencyCode = "usd", Rate = 25_000m, EffectiveFrom = Jan1, Note = "n" };

        var first = await sut.Handle(new RecordExchangeRateCommand(request, "user-1"), CancellationToken.None);
        var duplicate = sut.Handle(new RecordExchangeRateCommand(request with { Rate = 26_000m }, "user-1"), CancellationToken.None);

        Assert.True(first.IsSuccess);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => duplicate);
        Assert.Equal("A newer rate already exists; record a correction as a rate with a later effective date.", ex.Message);
        var stored = await host.Context.ExchangeRates.SingleAsync();
        Assert.Equal("USD", stored.CurrencyCode);
        Assert.Equal(25_000m, stored.Rate);
        Assert.Equal("user-1", stored.RecordedBy);
    }

    [Fact]
    public async Task RecordExchangeRate_ShouldTreatACorrectionAsANewerRate()
    {
        using var host = await SeededAsync();
        var sut = new RecordExchangeRateCommandHandler(host.Context);

        await sut.Handle(new RecordExchangeRateCommand(
            new RecordExchangeRateRequest { CurrencyCode = "USD", Rate = 25_000m, EffectiveFrom = Jan1 }, "u"), CancellationToken.None);
        await sut.Handle(new RecordExchangeRateCommand(
            new RecordExchangeRateRequest { CurrencyCode = "USD", Rate = 25_500m, EffectiveFrom = Feb1 }, "u"), CancellationToken.None);

        Assert.Equal(2, await host.Context.ExchangeRates.CountAsync());
    }

    [Fact]
    public async Task RecordExchangeRate_ShouldRejectABackdatedRate_ButAcceptTheFirstAndALaterOne()
    {
        using var host = await SeededAsync();
        var sut = new RecordExchangeRateCommandHandler(host.Context);

        Task<Light.Contracts.IResult<long>> Record(DateTimeOffset at) => sut.Handle(
            new RecordExchangeRateCommand(new RecordExchangeRateRequest { CurrencyCode = "USD", Rate = 25_000m, EffectiveFrom = at }, "u"),
            CancellationToken.None);

        // First rate for the currency: nothing to be later than.
        Assert.True((await Record(Feb1)).IsSuccess);

        // Earlier than the newest: refused.
        await Assert.ThrowsAsync<ConflictException>(() => Record(Jan1));

        // A rate for another currency is not affected by USD's history.
        var jpy = await sut.Handle(
            new RecordExchangeRateCommand(new RecordExchangeRateRequest { CurrencyCode = "JPY", Rate = 170m, EffectiveFrom = Jan1 }, "u"),
            CancellationToken.None);
        Assert.True(jpy.IsSuccess);

        // Strictly later: accepted.
        Assert.True((await Record(Feb1.AddSeconds(1))).IsSuccess);
        Assert.Equal(3, await host.Context.ExchangeRates.CountAsync());
    }

    [Fact]
    public async Task RecordExchangeRate_ShouldMapAConcurrentInsertOfTheSameInstant_ToConflict()
    {
        CurrencyTestHost? host = null;
        var race = new RaceInterceptor(async () =>
        {
            var other = host!.NewContext();
            var usd = await other.Currencies.SingleAsync(x => x.Id == "USD");
            other.ExchangeRates.Add(StarterKit.Currencies.Api.Domain.ExchangeRates.ExchangeRate.Record(usd, 24_000m, Jan1, "other", null, null));
            await other.SaveChangesAsync();
        });
        host = new CurrencyTestHost(race);
        using var _ = host;
        await host.SeedCurrenciesAsync();
        race.Armed = true;
        var sut = new RecordExchangeRateCommandHandler(host.Context);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(
            new RecordExchangeRateCommand(new RecordExchangeRateRequest { CurrencyCode = "USD", Rate = 25_000m, EffectiveFrom = Jan1 }, "u"),
            CancellationToken.None));

        Assert.Contains("newer rate already exists", ex.Message);
    }

    [Fact]
    public async Task CreateCurrency_ShouldMapAConcurrentInsertOfTheSameCode_ToConflict()
    {
        CurrencyTestHost? host = null;
        var race = new RaceInterceptor(async () =>
        {
            var other = host!.NewContext();
            other.Currencies.Add(StarterKit.Currencies.Api.Domain.Currencies.Currency.Create("EUR", "Euro", null, 2));
            await other.SaveChangesAsync();
        });
        host = new CurrencyTestHost(race);
        using var _ = host;
        race.Armed = true;

        var ex = await Assert.ThrowsAsync<ConflictException>(() => new CreateCurrencyCommandHandler(host.Context).Handle(
            new CreateCurrencyCommand(new CreateCurrencyRequest { Code = "EUR", Name = "Euro", DecimalPlaces = 2 }),
            CancellationToken.None));

        Assert.Equal("Currency 'EUR' already exists.", ex.Message);
    }

    [Fact]
    public async Task UpdateCurrency_ShouldRefuseDecimalPlacesChange_ForTheBase_AndOnceRatesExist()
    {
        using var host = await SeededAsync();
        await host.AddRateAsync("USD", 25_000m, Jan1);
        var sut = new UpdateCurrencyCommandHandler(host.Context);

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(
            new UpdateCurrencyCommand("VND", new UpdateCurrencyRequest { Name = "Dong", DecimalPlaces = 2 }),
            CancellationToken.None));

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(
            new UpdateCurrencyCommand("USD", new UpdateCurrencyRequest { Name = "Dollar", DecimalPlaces = 3 }),
            CancellationToken.None));

        // Unchanged decimal places stay editable (rename/symbol) even with rates recorded, and for the base.
        var renamed = await sut.Handle(
            new UpdateCurrencyCommand("USD", new UpdateCurrencyRequest { Name = "Dollar", Symbol = "US$", DecimalPlaces = 2 }),
            CancellationToken.None);
        var baseRenamed = await sut.Handle(
            new UpdateCurrencyCommand("VND", new UpdateCurrencyRequest { Name = "Dong", DecimalPlaces = 0 }),
            CancellationToken.None);

        Assert.True(renamed.IsSuccess);
        Assert.True(baseRenamed.IsSuccess);
        Assert.Equal("Dollar", (await host.Context.Currencies.SingleAsync(x => x.Id == "USD")).Name);
    }

    [Fact]
    public async Task UpdateCurrency_ShouldAllowChangingDecimalPlaces_WhenNoRateWasRecorded()
    {
        using var host = await SeededAsync();
        var sut = new UpdateCurrencyCommandHandler(host.Context);

        var result = await sut.Handle(
            new UpdateCurrencyCommand("JPY", new UpdateCurrencyRequest { Name = "Yen", DecimalPlaces = 2 }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, (await host.Context.Currencies.SingleAsync(x => x.Id == "JPY")).DecimalPlaces);
    }

    [Fact]
    public async Task SearchCurrencies_ShouldMatchTheCodeCaseInsensitively()
    {
        using var host = await SeededAsync();
        var sut = new SearchCurrenciesQueryHandler(host.Context);

        var result = await sut.Handle(new SearchCurrenciesQuery(new SearchCurrencyRequest { SearchValue = "jp" }), CancellationToken.None);

        Assert.Equal(["JPY"], result.Data.Records.Select(x => x.Code));
    }

    [Fact]
    public async Task RecordExchangeRate_ShouldRejectTheBaseCurrency_AndReturnNotFoundForUnknown()
    {
        using var host = await SeededAsync();
        var sut = new RecordExchangeRateCommandHandler(host.Context);

        await Assert.ThrowsAsync<Light.Exceptions.ValidationException>(() => sut.Handle(
            new RecordExchangeRateCommand(new RecordExchangeRateRequest { CurrencyCode = "VND", Rate = 1m, EffectiveFrom = Jan1 }, "u"),
            CancellationToken.None));

        var unknown = await sut.Handle(
            new RecordExchangeRateCommand(new RecordExchangeRateRequest { CurrencyCode = "XYZ", Rate = 1m, EffectiveFrom = Jan1 }, "u"),
            CancellationToken.None);

        Assert.False(unknown.IsSuccess);
    }

    [Fact]
    public async Task SearchExchangeRates_ShouldReturnNewestFirst_FilteredByCurrencyAndDateRange()
    {
        using var host = await SeededAsync();
        await host.AddRateAsync("USD", 24_000m, Jan1);
        await host.AddRateAsync("USD", 25_000m, Feb1);
        await host.AddRateAsync("USD", 26_000m, Feb1.AddMonths(1));
        await host.AddRateAsync("JPY", 170m, Feb1);
        var sut = new SearchExchangeRatesQueryHandler(host.Context);

        var usd = await sut.Handle(new SearchExchangeRatesQuery(new SearchExchangeRateRequest { CurrencyCode = "usd" }), CancellationToken.None);
        var ranged = await sut.Handle(
            new SearchExchangeRatesQuery(new SearchExchangeRateRequest { CurrencyCode = "USD", From = Feb1, To = Feb1.AddDays(5) }),
            CancellationToken.None);

        Assert.Equal([26_000m, 25_000m, 24_000m], usd.Data.Records.Select(x => x.Rate));
        Assert.Equal([25_000m], ranged.Data.Records.Select(x => x.Rate));
    }

    [Fact]
    public async Task GetLatestExchangeRates_ShouldReturnTheRateInEffectPerActiveForeignCurrency()
    {
        using var host = await SeededAsync();
        await host.AddRateAsync("USD", 24_000m, Jan1);
        await host.AddRateAsync("USD", 25_000m, Feb1);
        await host.AddRateAsync("JPY", 170m, Feb1.AddYears(1));
        host.DateTime.UtcNow = Feb1.AddDays(3);
        var sut = new GetLatestExchangeRatesQueryHandler(host.Context, host.DateTime);

        var now = await sut.Handle(new GetLatestExchangeRatesQuery(null), CancellationToken.None);
        var earlier = await sut.Handle(new GetLatestExchangeRatesQuery(Jan1.AddDays(1)), CancellationToken.None);

        // JPY's only rate is future-dated, so it is not "latest" yet; the base currency never appears.
        Assert.Equal(["USD"], now.Select(x => x.CurrencyCode));
        Assert.Equal(25_000m, now[0].Rate);
        Assert.Equal(24_000m, Assert.Single(earlier).Rate);
    }
}
