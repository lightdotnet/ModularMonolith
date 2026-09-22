import { CURRENCY_CODE_PATTERN, CURRENCY_LIMITS } from "@/modules/currency/common/constants/limits";
import type { CreateCurrencyRequest, UpdateCurrencyRequest } from "../types/currency";

function text(formData: FormData, key: string): string {
  return String(formData.get(key) ?? "").trim();
}

function readBody(formData: FormData): UpdateCurrencyRequest | { error: string } {
  const name = text(formData, "name");
  const symbol = text(formData, "symbol");
  const decimalsText = text(formData, "decimalPlaces");
  const decimalPlaces = Number(decimalsText);

  if (!name) return { error: "Name is required." };
  if (name.length > CURRENCY_LIMITS.nameMaxLength) {
    return { error: `Name must not exceed ${CURRENCY_LIMITS.nameMaxLength} characters.` };
  }
  if (symbol.length > CURRENCY_LIMITS.symbolMaxLength) {
    return { error: `Symbol must not exceed ${CURRENCY_LIMITS.symbolMaxLength} characters.` };
  }
  if (
    !/^\d+$/.test(decimalsText) ||
    !Number.isInteger(decimalPlaces) ||
    decimalPlaces < 0 ||
    decimalPlaces > CURRENCY_LIMITS.maxDecimalPlaces
  ) {
    return { error: `Decimal places must be a whole number from 0 to ${CURRENCY_LIMITS.maxDecimalPlaces}.` };
  }

  return { name, symbol: symbol || undefined, decimalPlaces };
}

/** Not a Server Action file — plain helper. Limits mirror the backend validators. */
export function readCreateCurrencyForm(
  formData: FormData,
): { request: CreateCurrencyRequest } | { error: string } {
  const code = text(formData, "code").toUpperCase();
  if (!CURRENCY_CODE_PATTERN.test(code)) {
    return { error: `Code must be a ${CURRENCY_LIMITS.codeLength}-letter ISO 4217 code.` };
  }

  const body = readBody(formData);
  if ("error" in body) return body;

  return { request: { code, ...body } };
}

export function readUpdateCurrencyForm(
  formData: FormData,
): { request: UpdateCurrencyRequest } | { error: string } {
  const body = readBody(formData);
  if ("error" in body) return body;
  return { request: body };
}
