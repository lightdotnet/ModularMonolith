"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { recordExchangeRate } from "@/modules/currency/exchange-rates/api/exchange-rates.api";
import { buildRecordRateRequest } from "@/modules/currency/exchange-rates/utils/rate-form";

export interface RecordExchangeRateFormState {
  error?: string;
  success?: boolean;
}

export async function recordExchangeRateAction(
  _prevState: RecordExchangeRateFormState,
  formData: FormData,
): Promise<RecordExchangeRateFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const parsed = buildRecordRateRequest({
    currencyCode: String(formData.get("currencyCode") ?? ""),
    rateText: String(formData.get("rate") ?? ""),
    effectiveFrom: String(formData.get("effectiveFrom") ?? ""),
    note: String(formData.get("note") ?? ""),
  });
  if ("error" in parsed) return { error: parsed.error };

  const result = await recordExchangeRate(parsed.request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to record the exchange rate." };
  }

  revalidatePath("/currency/exchange-rates");
  return { success: true };
}
