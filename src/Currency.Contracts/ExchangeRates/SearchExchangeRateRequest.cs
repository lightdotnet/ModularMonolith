namespace StarterKit.Currencies.Contracts.ExchangeRates;

/// <summary>Filters the rate history; <see cref="From"/>/<see cref="To"/> bound <c>EffectiveFrom</c> (both inclusive).</summary>
public record SearchExchangeRateRequest : PageQuery
{
    public string? CurrencyCode { get; set; }

    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }
}

public sealed class SearchExchangeRateRequestValidator : AbstractValidator<SearchExchangeRateRequest>
{
    public SearchExchangeRateRequestValidator()
    {
        RuleFor(x => x.CurrencyCode)
            .Matches("^[A-Za-z]{3}$")
            .When(x => !string.IsNullOrEmpty(x.CurrencyCode));

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From.HasValue && x.To.HasValue);
    }
}
