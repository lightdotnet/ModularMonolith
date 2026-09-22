using StarterKit.Currencies.Contracts.Common;

namespace StarterKit.Currencies.Contracts.Currencies;

public record UpdateCurrencyRequest
{
    public string Name { get; set; } = null!;

    public string? Symbol { get; set; }

    public int DecimalPlaces { get; set; }
}

public sealed class UpdateCurrencyRequestValidator : AbstractValidator<UpdateCurrencyRequest>
{
    public UpdateCurrencyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(CurrencyLimits.NameMaxLength);
        RuleFor(x => x.Symbol).MaximumLength(CurrencyLimits.SymbolMaxLength);
        RuleFor(x => x.DecimalPlaces).InclusiveBetween(0, CurrencyLimits.MaxDecimalPlaces);
    }
}
