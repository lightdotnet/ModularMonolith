namespace StarterKit.Currencies.Contracts.Services;

/// <summary>Pure rounding helper so every module rounds converted amounts the same way.</summary>
public static class CurrencyRounding
{
    /// <summary>
    /// Rounds <paramref name="amount"/> to a currency's minor units, midpoints away from zero
    /// (0.5 becomes 1, -0.5 becomes -1) — the usual commercial rounding, not banker's rounding.
    /// </summary>
    public static decimal RoundToMinorUnits(
        decimal amount,
        int decimalPlaces) =>
        Math.Round(amount, decimalPlaces, MidpointRounding.AwayFromZero);
}
