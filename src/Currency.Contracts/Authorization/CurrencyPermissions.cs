namespace StarterKit.Currencies.Contracts.Authorization;

public static class CurrencyPermissions
{
    public const string Group = "currency";

    public static class Currencies
    {
        public const string View = $"{Group}.currencies.view";

        /// <summary>Create a currency, edit its name/symbol/decimal places, and activate or deactivate it.</summary>
        public const string Manage = $"{Group}.currencies.manage";
    }

    public static class Rates
    {
        public const string View = $"{Group}.rates.view";

        /// <summary>Record a new exchange rate (history is append-only; a correction is a newer rate).</summary>
        public const string Manage = $"{Group}.rates.manage";
    }
}
