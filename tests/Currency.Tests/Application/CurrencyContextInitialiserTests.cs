using Currency.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StarterKit.Currencies.Api.Data;
using CurrencyEntity = StarterKit.Currencies.Api.Domain.Currencies.Currency;

namespace Currency.Tests.Application;

public class CurrencyContextInitialiserTests
{
    private static CurrencyContextInitialiser CreateSut(CurrencyTestHost host) =>
        new(NullLogger<CurrencyContextInitialiser>.Instance, host.Context);

    [Fact]
    public async Task SeedAsync_ShouldCreateTheVndBaseCurrency_WhenNoneExists()
    {
        using var host = new CurrencyTestHost();

        await CreateSut(host).SeedAsync();

        var seeded = await host.Context.Currencies.SingleAsync();
        Assert.Equal("VND", seeded.Code);
        Assert.Equal("Vietnamese dong", seeded.Name);
        Assert.Equal(0, seeded.DecimalPlaces);
        Assert.True(seeded.IsBase);
        Assert.True(seeded.IsActive);
    }

    [Fact]
    public async Task SeedAsync_ShouldBeIdempotent()
    {
        using var host = new CurrencyTestHost();
        var sut = CreateSut(host);

        await sut.SeedAsync();
        await sut.SeedAsync();
        await sut.SeedAsync();

        Assert.Equal(1, await host.Context.Currencies.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_ShouldLeaveAnExistingBaseCurrencyUntouched()
    {
        using var host = new CurrencyTestHost();
        host.Context.Currencies.Add(CurrencyEntity.CreateBase("USD", "US dollar", "$", 2));
        await host.Context.SaveChangesAsync();

        await CreateSut(host).SeedAsync();

        var only = await host.Context.Currencies.SingleAsync();
        Assert.Equal("USD", only.Code);
        Assert.True(only.IsBase);
    }

    [Fact]
    public async Task SeedAsync_ShouldFailLoudly_WhenANonBaseVndExistsAndThereIsNoBase()
    {
        using var host = new CurrencyTestHost();
        host.Context.Currencies.Add(CurrencyEntity.Create("VND", "Dong", null, 0));
        await host.Context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut(host).SeedAsync());

        Assert.Contains("not the base currency", ex.Message);
        Assert.DoesNotContain(host.Context.Currencies, x => x.IsBase);
    }

    [Fact]
    public async Task SeedAsync_ShouldTreatAParallelSeederRunAsSuccess()
    {
        CurrencyTestHost? host = null;
        var race = new RaceInterceptor(async () =>
        {
            var other = host!.NewContext();
            other.Currencies.Add(CurrencyEntity.CreateBase("VND", "Vietnamese dong", null, 0));
            await other.SaveChangesAsync();
        });
        host = new CurrencyTestHost(race);
        using var _ = host;
        race.Armed = true;

        // The competing run wins the insert; this run hits the unique violation and must not fail.
        await CreateSut(host).SeedAsync();

        var only = await host.NewContext().Currencies.SingleAsync();
        Assert.True(only.IsBase);
    }
}
