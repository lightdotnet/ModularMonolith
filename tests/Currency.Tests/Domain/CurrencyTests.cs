using Currency.Tests.TestSupport;
using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using CurrencyEntity = StarterKit.Currencies.Api.Domain.Currencies.Currency;

namespace Currency.Tests.Domain;

public class CurrencyTests
{
    [Fact]
    public void Create_ShouldBuildAnActiveNonBaseCurrency_WithANormalizedCode()
    {
        var currency = CurrencyEntity.Create(" usd ", "  US dollar ", " $ ", 2);

        Assert.Equal("USD", currency.Code);
        Assert.Equal("USD", currency.Id);
        Assert.Equal("US dollar", currency.Name);
        Assert.Equal("$", currency.Symbol);
        Assert.Equal(2, currency.DecimalPlaces);
        Assert.True(currency.IsActive);
        Assert.False(currency.IsBase);
    }

    [Fact]
    public void CreateBase_ShouldFlagTheCurrencyAsBase()
    {
        var currency = CurrencyEntity.CreateBase("VND", "Vietnamese dong", null, 0);

        Assert.True(currency.IsBase);
        Assert.True(currency.IsActive);
        Assert.Null(currency.Symbol);
    }

    [Fact]
    public void Deactivate_ShouldRefuseTheBaseCurrency()
    {
        var currency = CurrencyEntity.CreateBase("VND", "Vietnamese dong", null, 0);

        Assert.Throws<ConflictException>(currency.Deactivate);
        Assert.True(currency.IsActive);
    }

    [Fact]
    public void Deactivate_And_Activate_ShouldToggleANonBaseCurrency()
    {
        var currency = CurrencyEntity.Create("USD", "US dollar", null, 2);

        currency.Deactivate();
        Assert.False(currency.IsActive);

        currency.Activate();
        Assert.True(currency.IsActive);
    }

    [Fact]
    public void Rename_SetSymbol_And_SetDecimalPlaces_ShouldUpdateTheCurrency()
    {
        var currency = CurrencyEntity.Create("KWD", "Kuwaiti", null, 2);

        currency.Rename(" Kuwaiti dinar ");
        currency.SetSymbol("KD");
        currency.SetDecimalPlaces(3, hasRecordedRates: false);

        Assert.Equal("Kuwaiti dinar", currency.Name);
        Assert.Equal("KD", currency.Symbol);
        Assert.Equal(3, currency.DecimalPlaces);

        currency.SetSymbol("  ");
        Assert.Null(currency.Symbol);
    }

    [Fact]
    public void SetDecimalPlaces_ShouldRefuseTheBaseCurrency()
    {
        var currency = CurrencyEntity.CreateBase("VND", "Vietnamese dong", null, 0);

        Assert.Throws<ConflictException>(() => currency.SetDecimalPlaces(2, hasRecordedRates: false));
        Assert.Equal(0, currency.DecimalPlaces);
    }

    [Fact]
    public void SetDecimalPlaces_ShouldRefuseAChangeOnceRatesAreRecorded()
    {
        var currency = CurrencyEntity.Create("USD", "US dollar", null, 2);

        Assert.Throws<ConflictException>(() => currency.SetDecimalPlaces(3, hasRecordedRates: true));
        Assert.Equal(2, currency.DecimalPlaces);
    }

    [Fact]
    public void SetDecimalPlaces_ShouldBeANoOp_WhenTheValueIsUnchanged()
    {
        var baseCurrency = CurrencyEntity.CreateBase("VND", "Vietnamese dong", null, 0);
        var usd = CurrencyEntity.Create("USD", "US dollar", null, 2);

        baseCurrency.SetDecimalPlaces(0, hasRecordedRates: false);
        usd.SetDecimalPlaces(2, hasRecordedRates: true);

        Assert.Equal(0, baseCurrency.DecimalPlaces);
        Assert.Equal(2, usd.DecimalPlaces);
    }

    [Fact]
    public void TheBaseFlag_ShouldNotBeChangeableAfterCreation()
    {
        // Changing the base currency is out of scope for v1: no public operation may flip the flag.
        var setter = typeof(CurrencyEntity).GetProperty(nameof(CurrencyEntity.IsBase))!.SetMethod!;

        Assert.False(setter.IsPublic);
    }

    [Fact]
    public async Task TheDatabase_ShouldRefuseASecondBaseCurrency()
    {
        using var host = new CurrencyTestHost();
        host.Context.Currencies.Add(CurrencyEntity.CreateBase("VND", "Vietnamese dong", null, 0));
        await host.Context.SaveChangesAsync();

        host.Context.Currencies.Add(CurrencyEntity.CreateBase("USD", "US dollar", "$", 2));

        await Assert.ThrowsAsync<DbUpdateException>(() => host.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task TheDatabase_ShouldAllowManyNonBaseCurrencies_AndRefuseADuplicateCode()
    {
        using var host = new CurrencyTestHost();
        await host.SeedCurrenciesAsync();

        Assert.Equal(3, await host.Context.Currencies.CountAsync());

        // USD is already tracked by the host's context, so use a fresh context to hit the database's primary key.
        var other = host.NewContext();
        other.Currencies.Add(CurrencyEntity.Create("USD", "Duplicate", null, 2));

        await Assert.ThrowsAsync<DbUpdateException>(() => other.SaveChangesAsync());
    }
}
