/** Mirrors Currency.Contracts/Authorization/CurrencyPermissions.cs (currency.*). */
export const CURRENCY_PERMISSIONS = {
  Currencies: {
    View: "currency.currencies.view",
    /** Create a currency, edit its name/symbol/decimal places, and activate or deactivate it. */
    Manage: "currency.currencies.manage",
  },
  Rates: {
    View: "currency.rates.view",
    /** Record a new exchange rate (history is append-only; a correction is a newer rate). */
    Manage: "currency.rates.manage",
  },
} as const;
