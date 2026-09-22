namespace StarterKit.Currencies.Contracts.Services;

/// <summary>The slice of a currency other modules need: its code and how amounts in it are rounded.</summary>
public sealed record CurrencyInfoDto(
    string Code,
    int DecimalPlaces,
    bool IsActive,
    bool IsBase);
