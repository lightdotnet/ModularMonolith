namespace StarterKit.Currencies.Contracts.Currencies;

/// <summary><see cref="SearchQuery.SearchValue"/> matches the currency code or name.</summary>
public record SearchCurrencyRequest : SearchQuery
{
    public bool? IsActive { get; set; }
}
