using Currency.Tests.TestSupport;
using StarterKit.Currencies.Api.Application.ExchangeRates.Commands;
using StarterKit.Currencies.Contracts.Common;
using StarterKit.Currencies.Contracts.Currencies;
using StarterKit.Currencies.Contracts.ExchangeRates;

namespace Currency.Tests.Validators;

public class CurrencyRequestValidatorTests
{
    private static RecordExchangeRateRequest Rate(
        string code = "USD",
        decimal rate = 25_000m,
        DateTimeOffset? effectiveFrom = null,
        string? note = null) =>
        new()
        {
            CurrencyCode = code,
            Rate = rate,
            EffectiveFrom = effectiveFrom ?? DateTimeOffset.UtcNow.AddDays(-1),
            Note = note,
        };

    private static bool IsValid(RecordExchangeRateRequest request) =>
        new RecordExchangeRateRequestValidator().Validate(request).IsValid;

    [Fact]
    public void RecordRate_ShouldAcceptAValidRequest() => Assert.True(IsValid(Rate()));

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("U5D")]
    public void RecordRate_ShouldRejectAMalformedCode(string code) => Assert.False(IsValid(Rate(code: code)));

    [Fact]
    public void RecordRate_ShouldAcceptALowerCaseCode_TheHandlerNormalizesIt() => Assert.True(IsValid(Rate(code: "usd")));

    [Theory]
    [InlineData("0", false)]
    [InlineData("-1", false)]
    [InlineData("0.00000001", true)]
    [InlineData("1000000000", true)]
    [InlineData("1000000000.00000001", false)]
    [InlineData("1000000001", false)]
    [InlineData("1.123456789", false)]
    [InlineData("1.12345678", true)]
    [InlineData("1.1234567800", true)]
    public void RecordRate_ShouldBoundTheRateAndItsScale(
        string value,
        bool expected) =>
        Assert.Equal(expected, IsValid(Rate(rate: decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture))));

    [Fact]
    public void MaxRate_ShouldFitTheDecimal18_8Column()
    {
        // decimal(18,8) holds 10 integer digits, i.e. values below 1e10.
        Assert.True(CurrencyLimits.MaxRate < 10_000_000_000m);
        Assert.True(CurrencyLimits.RatePrecision - CurrencyLimits.RateScale >= CurrencyLimits.MaxRate.ToString("0").Length);
    }

    [Fact]
    public void RecordRate_ShouldRejectAZeroEffectiveFrom() =>
        Assert.False(IsValid(Rate(effectiveFrom: default(DateTimeOffset))));

    [Fact]
    public void RecordCommand_ShouldRejectAnEffectiveFromMoreThanADayAhead_UsingTheInjectedClock()
    {
        var now = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var validator = new RecordExchangeRateCommandValidator(new FakeDateTime { UtcNow = now });

        bool IsValidAt(DateTimeOffset effectiveFrom) =>
            validator.Validate(new RecordExchangeRateCommand(Rate(effectiveFrom: effectiveFrom), "user-1")).IsValid;

        Assert.True(IsValidAt(now.AddDays(-30)));
        Assert.True(IsValidAt(now.AddHours(23)));
        Assert.True(IsValidAt(now.AddDays(1)));
        Assert.False(IsValidAt(now.AddDays(1).AddSeconds(1)));
        Assert.False(IsValidAt(now.AddDays(2)));
    }

    [Fact]
    public void RecordCommand_ShouldRequireTheRecordingUser()
    {
        var validator = new RecordExchangeRateCommandValidator(new FakeDateTime { UtcNow = DateTimeOffset.UtcNow });

        Assert.False(validator.Validate(new RecordExchangeRateCommand(Rate(), "")).IsValid);
    }

    [Fact]
    public void RecordRate_ShouldCapTheNoteAt500Characters()
    {
        Assert.True(IsValid(Rate(note: new string('x', 500))));
        Assert.False(IsValid(Rate(note: new string('x', 501))));
    }

    [Theory]
    [InlineData("USD", "US dollar", "$", 2, true)]
    [InlineData("usd", "US dollar", null, 0, true)]
    [InlineData("KWD", "Kuwaiti dinar", null, 4, true)]
    [InlineData("US", "US dollar", null, 2, false)]
    [InlineData("USD", "", null, 2, false)]
    [InlineData("USD", "US dollar", "12345678901", 2, false)]
    [InlineData("USD", "US dollar", null, -1, false)]
    [InlineData("USD", "US dollar", null, 5, false)]
    public void CreateCurrency_ShouldValidateShapeAndBounds(
        string code,
        string name,
        string? symbol,
        int decimalPlaces,
        bool expected)
    {
        var request = new CreateCurrencyRequest { Code = code, Name = name, Symbol = symbol, DecimalPlaces = decimalPlaces };

        Assert.Equal(expected, new CreateCurrencyRequestValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData("US dollar", 2, true)]
    [InlineData("", 2, false)]
    [InlineData("US dollar", 5, false)]
    public void UpdateCurrency_ShouldValidateNameAndDecimalPlaces(
        string name,
        int decimalPlaces,
        bool expected)
    {
        var request = new UpdateCurrencyRequest { Name = name, DecimalPlaces = decimalPlaces };

        Assert.Equal(expected, new UpdateCurrencyRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void SearchRates_ShouldRejectAnInvertedDateRange_AndAMalformedCode()
    {
        var validator = new SearchExchangeRateRequestValidator();
        var now = DateTimeOffset.UtcNow;

        Assert.True(validator.Validate(new SearchExchangeRateRequest { From = now.AddDays(-2), To = now }).IsValid);
        Assert.False(validator.Validate(new SearchExchangeRateRequest { From = now, To = now.AddDays(-2) }).IsValid);
        Assert.False(validator.Validate(new SearchExchangeRateRequest { CurrencyCode = "US" }).IsValid);
        Assert.True(validator.Validate(new SearchExchangeRateRequest()).IsValid);
    }
}
