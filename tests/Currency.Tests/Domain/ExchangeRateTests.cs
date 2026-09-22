using System.Reflection;
using Currency.Tests.TestSupport;
using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using StarterKit.Currencies.Api.Domain.ExchangeRates;
using CurrencyEntity = StarterKit.Currencies.Api.Domain.Currencies.Currency;
using ValidationException = Light.Exceptions.ValidationException;

namespace Currency.Tests.Domain;

public class ExchangeRateTests
{
    private static readonly DateTimeOffset At = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static CurrencyEntity Usd() => CurrencyEntity.Create("USD", "US dollar", "$", 2);

    [Fact]
    public void Record_ShouldCaptureTheRate()
    {
        var rate = ExchangeRate.Record(Usd(), 25_000.5m, At, "user-1", "  weekly  ", null);

        Assert.Equal("USD", rate.CurrencyCode);
        Assert.Equal(25_000.5m, rate.Rate);
        Assert.Equal(At, rate.EffectiveFrom);
        Assert.Equal("user-1", rate.RecordedBy);
        Assert.Equal("weekly", rate.Note);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Record_ShouldRejectANonPositiveRate(decimal value)
    {
        Assert.Throws<ValidationException>(() => ExchangeRate.Record(Usd(), value, At, "user-1", null, null));
    }

    [Fact]
    public void Record_ShouldRejectARateForTheBaseCurrency()
    {
        var baseCurrency = CurrencyEntity.CreateBase("VND", "Vietnamese dong", null, 0);

        Assert.Throws<ValidationException>(() => ExchangeRate.Record(baseCurrency, 1m, At, "user-1", null, null));
    }

    [Fact]
    public void Record_ShouldRejectARateForAnInactiveCurrency()
    {
        var usd = Usd();
        usd.Deactivate();

        Assert.Throws<ConflictException>(() => ExchangeRate.Record(usd, 25_000m, At, "user-1", null, null));
    }

    [Fact]
    public void Record_ShouldAcceptTheFirstRateForACurrency()
    {
        var rate = ExchangeRate.Record(Usd(), 25_000m, At, "user-1", null, latestEffectiveFrom: null);

        Assert.Equal(At, rate.EffectiveFrom);
    }

    [Fact]
    public void Record_ShouldAcceptAStrictlyLaterEffectiveDate()
    {
        var rate = ExchangeRate.Record(Usd(), 25_000m, At.AddSeconds(1), "user-1", null, latestEffectiveFrom: At);

        Assert.Equal(At.AddSeconds(1), rate.EffectiveFrom);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-86_400)]
    public void Record_ShouldRejectAnEqualOrEarlierEffectiveDate(int offsetSeconds)
    {
        var ex = Assert.Throws<ConflictException>(
            () => ExchangeRate.Record(Usd(), 25_000m, At.AddSeconds(offsetSeconds), "user-1", null, latestEffectiveFrom: At));

        Assert.Equal(ExchangeRate.NewerRateExistsMessage, ex.Message);
        Assert.Equal(
            "A newer rate already exists; record a correction as a rate with a later effective date.",
            ExchangeRate.NewerRateExistsMessage);
    }

    [Fact]
    public void TheAggregate_ShouldExposeNoWayToChangeARecordedRate()
    {
        var type = typeof(ExchangeRate);

        Assert.All(
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
            p => Assert.False(p.SetMethod?.IsPublic ?? false, p.Name));

        var publicMethods = type
            .GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name);

        Assert.Equal([nameof(ExchangeRate.Record)], publicMethods);
    }

    [Fact]
    public async Task TheDatabase_ShouldRefuseASecondRateForTheSameCurrencyAndInstant()
    {
        using var host = new CurrencyTestHost();
        await host.SeedCurrenciesAsync();
        await host.AddRateAsync("USD", 25_000m, At);

        var usd = await host.Context.Currencies.SingleAsync(x => x.Id == "USD");
        host.Context.ExchangeRates.Add(ExchangeRate.Record(usd, 26_000m, At, "user-1", null, null));

        await Assert.ThrowsAsync<DbUpdateException>(() => host.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task TheDatabase_ShouldAllowTheSameInstantForDifferentCurrencies()
    {
        using var host = new CurrencyTestHost();
        await host.SeedCurrenciesAsync();

        await host.AddRateAsync("USD", 25_000m, At);
        await host.AddRateAsync("JPY", 170m, At);

        Assert.Equal(2, await host.Context.ExchangeRates.CountAsync());
    }

    [Fact]
    public async Task SaveChanges_ShouldRefuseToModifyARecordedRate()
    {
        using var host = new CurrencyTestHost();
        await host.SeedCurrenciesAsync();
        await host.AddRateAsync("USD", 25_000m, At);

        var stored = await host.Context.ExchangeRates.SingleAsync();
        host.Context.Entry(stored).Property(x => x.Rate).CurrentValue = 1m;

        await Assert.ThrowsAsync<ConflictException>(() => host.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_ShouldRefuseToDeleteARecordedRate()
    {
        using var host = new CurrencyTestHost();
        await host.SeedCurrenciesAsync();
        await host.AddRateAsync("USD", 25_000m, At);

        host.Context.ExchangeRates.Remove(await host.Context.ExchangeRates.SingleAsync());

        await Assert.ThrowsAsync<ConflictException>(() => host.Context.SaveChangesAsync());
    }
}
