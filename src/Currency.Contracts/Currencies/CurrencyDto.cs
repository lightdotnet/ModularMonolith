namespace StarterKit.Currencies.Contracts.Currencies;

/// <summary>A currency; <see cref="Code"/> (ISO 4217) is its identity.</summary>
public class CurrencyDto
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Symbol { get; set; }

    /// <summary>Minor-unit digits amounts in this currency are rounded to.</summary>
    public int DecimalPlaces { get; set; }

    public bool IsActive { get; set; }

    /// <summary>True for the single base currency all order calculations are done in.</summary>
    public bool IsBase { get; set; }
}
