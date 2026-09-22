import { MAX_AMOUNT, MAX_AMOUNT_DECIMALS, MAX_QUANTITY } from "../constants/limits";

/** Whole number in 1..1,000,000. */
export function isValidQuantity(value: number): boolean {
  return Number.isInteger(value) && value >= 1 && value <= MAX_QUANTITY;
}

/** Amount in 0..1,000,000,000 with at most 4 decimals. Takes the raw input text so decimals are counted exactly. */
export function isValidAmountText(raw: string): boolean {
  const text = raw.trim();
  if (!/^\d+(\.\d+)?$/.test(text)) return false;
  const decimals = text.split(".")[1]?.length ?? 0;
  if (decimals > MAX_AMOUNT_DECIMALS) return false;
  const value = Number(text);
  return Number.isFinite(value) && value >= 0 && value <= MAX_AMOUNT;
}

/** Same rule for an already-parsed number (server actions receive numbers). */
export function isValidAmount(value: number): boolean {
  if (!Number.isFinite(value)) return false;
  return isValidAmountText(String(value));
}
