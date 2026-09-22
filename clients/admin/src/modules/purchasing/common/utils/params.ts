const LONG_MAX = "9223372036854775807";

/** Backend ids are `long` — only digit strings within the long range are ever forwarded into a URL segment or query. */
export function isNumericId(value: string | undefined | null): value is string {
  if (!value || !/^\d{1,19}$/.test(value)) return false;
  // Same-length digit strings compare correctly lexicographically; shorter ones are always smaller.
  return value.length < LONG_MAX.length || value <= LONG_MAX;
}

/** Safe page-number parse: a positive integer, else 1. */
export function parsePageNumber(value: string | undefined): number {
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) && parsed >= 1 ? parsed : 1;
}

/** Returns the matching enum member when `value` is one of its values, else undefined. */
export function parseEnumValue<T extends string>(
  values: Record<string, T>,
  value: string | undefined,
): T | undefined {
  return Object.values(values).find((candidate) => candidate === value);
}

/** Returns `value` only when it is a digit-only id, else undefined. */
export function parseNumericIdParam(value: string | undefined): string | undefined {
  return isNumericId(value) ? value : undefined;
}
