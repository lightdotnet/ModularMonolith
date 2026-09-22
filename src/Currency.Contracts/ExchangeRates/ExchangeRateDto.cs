namespace StarterKit.Currencies.Contracts.ExchangeRates;

/// <summary>One recorded rate: 1 unit of <see cref="CurrencyCode"/> = <see cref="Rate"/> units of the base currency.</summary>
public class ExchangeRateDto : BaseDto<long>
{
    public string CurrencyCode { get; set; } = null!;

    public decimal Rate { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; }

    public string RecordedBy { get; set; } = null!;

    public string? Note { get; set; }
}
