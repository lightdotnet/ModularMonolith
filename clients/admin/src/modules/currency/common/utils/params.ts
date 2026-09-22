import { CURRENCY_CODE_PATTERN } from "../constants/limits";

/** Safe page-number parse: a positive integer, else 1. */
export function parsePageNumber(value: string | undefined): number {
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) && parsed >= 1 ? parsed : 1;
}

/** Returns the upper-cased currency code only when it is exactly three letters, else undefined. */
export function parseCurrencyCodeParam(value: string | undefined): string | undefined {
  const text = value?.trim();
  return text && CURRENCY_CODE_PATTERN.test(text) ? text.toUpperCase() : undefined;
}

/** Returns the `yyyy-MM-dd` value only when it is a real calendar date, else undefined. */
export function parseDateParam(value: string | undefined): string | undefined {
  if (!value || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return undefined;
  const parsed = new Date(`${value}T00:00:00.000Z`);
  return Number.isNaN(parsed.getTime()) || parsed.toISOString().slice(0, 10) !== value ? undefined : value;
}

/** Returns the boolean for "true"/"false", else undefined. */
export function parseBooleanParam(value: string | undefined): boolean | undefined {
  if (value === "true") return true;
  if (value === "false") return false;
  return undefined;
}
