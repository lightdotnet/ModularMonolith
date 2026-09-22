/** Mirrors Currency.Contracts/Common/CurrencyLimits.cs. */
export const CURRENCY_LIMITS = {
  codeLength: 3,
  nameMaxLength: 100,
  symbolMaxLength: 10,
  noteMaxLength: 500,
  maxDecimalPlaces: 4,
  rateScale: 8,
  maxRate: 1_000_000_000,
  /** How far ahead of now a rate may be dated (1 day). */
  maxFutureEffectiveFromMs: 24 * 60 * 60 * 1000,
} as const;

export const CURRENCY_CODE_PATTERN = /^[A-Za-z]{3}$/;

/** Upper bound on currencies loaded into a picker or filter (the backend caps page size at 100). */
export const CURRENCY_OPTIONS_LIMIT = 100;
