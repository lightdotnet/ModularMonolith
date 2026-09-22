using StarterKit.Currencies.Contracts.Common;

namespace StarterKit.Currencies.Contracts.Currencies;

public record CreateCurrencyRequest
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Symbol { get; set; }

    public int DecimalPlaces { get; set; }
}

public sealed class CreateCurrencyRequestValidator : AbstractValidator<CreateCurrencyRequest>
{
    public CreateCurrencyRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$")
            .WithMessage($"Code must be a {CurrencyLimits.CodeLength}-letter ISO 4217 code.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(CurrencyLimits.NameMaxLength);
        RuleFor(x => x.Symbol).MaximumLength(CurrencyLimits.SymbolMaxLength);
        RuleFor(x => x.DecimalPlaces).InclusiveBetween(0, CurrencyLimits.MaxDecimalPlaces);
    }
}
