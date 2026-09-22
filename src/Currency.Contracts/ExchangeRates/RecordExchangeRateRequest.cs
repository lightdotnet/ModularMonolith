using StarterKit.Currencies.Contracts.Common;

namespace StarterKit.Currencies.Contracts.ExchangeRates;

/// <summary>"1 unit of <see cref="CurrencyCode"/> = <see cref="Rate"/> units of the base currency", effective from <see cref="EffectiveFrom"/>.</summary>
public record RecordExchangeRateRequest
{
    public string CurrencyCode { get; set; } = null!;

    public decimal Rate { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; }

    public string? Note { get; set; }
}

public sealed class RecordExchangeRateRequestValidator : AbstractValidator<RecordExchangeRateRequest>
{
    public RecordExchangeRateRequestValidator()
    {
        RuleFor(x => x.CurrencyCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$")
            .WithMessage($"Currency code must be a {CurrencyLimits.CodeLength}-letter ISO 4217 code.");

        RuleFor(x => x.Rate)
            .GreaterThan(0)
            .LessThanOrEqualTo(CurrencyLimits.MaxRate)
            .Must(HasAtMostAllowedScale)
            .WithMessage($"Rate can have at most {CurrencyLimits.RateScale} decimal places.");

        // The "not too far in the future" rule needs the clock, so it lives in the command validator
        // (which gets IDateTime injected), like Purchasing's clock-dependent rules.
        RuleFor(x => x.EffectiveFrom).NotEqual(default(DateTimeOffset));

        RuleFor(x => x.Note).MaximumLength(CurrencyLimits.NoteMaxLength);
    }

    private static bool HasAtMostAllowedScale(decimal rate)
    {
        // Dividing by 1.000...0 (28 zeros) strips trailing zeros, so 1.50000000000 counts as scale 1.
        var normalized = rate / 1.0000000000000000000000000000m;

        return ((decimal.GetBits(normalized)[3] >> 16) & 0xFF) <= CurrencyLimits.RateScale;
    }
}
