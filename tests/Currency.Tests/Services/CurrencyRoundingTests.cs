using StarterKit.Currencies.Contracts.Services;

namespace Currency.Tests.Services;

public class CurrencyRoundingTests
{
    [Theory]
    [InlineData(0.5, 0, 1)]
    [InlineData(1.5, 0, 2)]
    [InlineData(2.5, 0, 3)]
    [InlineData(-0.5, 0, -1)]
    [InlineData(-2.5, 0, -3)]
    [InlineData(1234.4999, 0, 1234)]
    [InlineData(1.005, 2, 1.01)]
    [InlineData(2.675, 2, 2.68)]
    [InlineData(-1.005, 2, -1.01)]
    [InlineData(1.2345, 3, 1.235)]
    [InlineData(1.2344, 3, 1.234)]
    [InlineData(10, 2, 10)]
    public void RoundToMinorUnits_ShouldRoundMidpointsAwayFromZero(
        double amount,
        int decimalPlaces,
        double expected)
    {
        Assert.Equal((decimal)expected, CurrencyRounding.RoundToMinorUnits((decimal)amount, decimalPlaces));
    }

    [Fact]
    public void RoundToMinorUnits_ShouldNotUseBankersRounding()
    {
        Assert.Equal(3m, CurrencyRounding.RoundToMinorUnits(2.5m, 0));
        Assert.Equal(1m, CurrencyRounding.RoundToMinorUnits(0.5m, 0));
    }
}
