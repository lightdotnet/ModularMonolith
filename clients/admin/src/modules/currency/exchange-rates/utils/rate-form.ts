import { CURRENCY_CODE_PATTERN, CURRENCY_LIMITS } from "@/modules/currency/common/constants/limits";
import type { RecordExchangeRateRequest } from "../types/exchange-rate";

/** Number of fraction digits once trailing zeros are ignored (so "1.50000000000" counts as 1). */
function significantDecimals(text: string): number {
  const fraction = text.split(".")[1] ?? "";
  return fraction.replace(/0+$/, "").length;
}

/**
 * Validates a "record exchange rate" input against the backend limits and returns the request, or an error.
 * Pure (no Node/browser APIs), so the dialog and the Server Action share the exact same rules.
 * `rateText` is the raw input so the decimal places are counted exactly; `effectiveFrom` is an ISO instant.
 */
export function buildRecordRateRequest(
  input: { currencyCode: string; rateText: string; effectiveFrom: string; note: string },
  nowMs: number = Date.now(),
): { request: RecordExchangeRateRequest } | { error: string } {
  const currencyCode = input.currencyCode.trim().toUpperCase();
  const rateText = input.rateText.trim();
  const note = input.note.trim();

  if (!CURRENCY_CODE_PATTERN.test(currencyCode)) {
    return { error: `Currency code must be a ${CURRENCY_LIMITS.codeLength}-letter ISO 4217 code.` };
  }

  if (rateText.includes(",")) return { error: "Use a dot as the decimal separator." };
  if (!/^\d+(\.\d+)?$/.test(rateText)) return { error: "Rate must be a positive number." };
  const rate = Number(rateText);
  if (!Number.isFinite(rate) || rate <= 0) return { error: "Rate must be greater than 0." };
  if (rate > CURRENCY_LIMITS.maxRate) {
    return { error: `Rate must not exceed ${CURRENCY_LIMITS.maxRate.toLocaleString("en-US")}.` };
  }
  if (significantDecimals(rateText) > CURRENCY_LIMITS.rateScale) {
    return { error: `Rate can have at most ${CURRENCY_LIMITS.rateScale} decimal places.` };
  }

  const effective = new Date(input.effectiveFrom);
  if (Number.isNaN(effective.getTime())) return { error: "Effective from must be a valid date and time." };
  if (effective.getTime() > nowMs + CURRENCY_LIMITS.maxFutureEffectiveFromMs) {
    return { error: "Effective from can be at most 1 day in the future." };
  }

  if (note.length > CURRENCY_LIMITS.noteMaxLength) {
    return { error: `Note must not exceed ${CURRENCY_LIMITS.noteMaxLength} characters.` };
  }

  return {
    request: {
      currencyCode,
      rate,
      effectiveFrom: effective.toISOString(),
      note: note || undefined,
    },
  };
}
