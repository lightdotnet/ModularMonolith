namespace StarterKit.Shared.Constants;

public static class CurrencyConstants
{
    /// <summary>
    /// Bootstrap/seed default only — the code seeded as the base currency when none exists yet.
    /// It is not an enforced rule: the actual base currency is data owned by whichever module manages
    /// currencies, and <c>Money</c> accepts any well-formed ISO 4217 code.
    /// </summary>
    public const string Default = "VND";
}
