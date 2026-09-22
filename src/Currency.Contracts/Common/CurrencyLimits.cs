namespace StarterKit.Currencies.Contracts.Common;

/// <summary>Shared bounds for currency and exchange-rate input, used by validators and the EF mapping.</summary>
public static class CurrencyLimits
{
    /// <summary>ISO 4217 alphabetic codes are exactly three letters.</summary>
    public const int CodeLength = 3;

    public const int NameMaxLength = 100;

    public const int SymbolMaxLength = 10;

    public const int NoteMaxLength = 500;

    /// <summary>Minor-unit digits supported per currency (0 for VND/JPY, 2 for USD, 3 for KWD, ...).</summary>
    public const int MaxDecimalPlaces = 4;

    public const int RatePrecision = 18;

    public const int RateScale = 8;

    /// <summary>Upper sanity bound for a rate; kept below what the decimal(18,8) column can hold (under 1e10).</summary>
    public const decimal MaxRate = 1_000_000_000m;

    /// <summary>How far ahead of now a rate may be dated (covers timezone skew and pre-announced rates).</summary>
    public static readonly TimeSpan MaxFutureEffectiveFrom = TimeSpan.FromDays(1);
}
