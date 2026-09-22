namespace StarterKit.Shared.Constants;

public static class CurrencyConstants
{
    /// <summary>
    /// Bootstrap/seed default only — the code the Currency module seeds as the base currency when none
    /// exists yet. It is not an enforced rule: the actual base currency is data owned by the Currency
    /// module, and <c>Money</c> accepts any well-formed ISO 4217 code.
    /// </summary>
    public const string Default = "VND";
}
