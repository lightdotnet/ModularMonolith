"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { CURRENCY_CODE_PATTERN } from "@/modules/currency/common/constants/limits";
import { updateCurrency } from "@/modules/currency/currencies/api/currencies.api";
import { readUpdateCurrencyForm } from "@/modules/currency/currencies/utils/currency-form";

export interface UpdateCurrencyFormState {
  error?: string;
  success?: boolean;
}

export async function updateCurrencyAction(
  _prevState: UpdateCurrencyFormState,
  formData: FormData,
): Promise<UpdateCurrencyFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const code = String(formData.get("code") ?? "").trim().toUpperCase();
  if (!CURRENCY_CODE_PATTERN.test(code)) {
    return { error: "Invalid currency." };
  }

  const parsed = readUpdateCurrencyForm(formData);
  if ("error" in parsed) return { error: parsed.error };

  const result = await updateCurrency(code, parsed.request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update currency." };
  }

  revalidatePath("/currency/currencies");
  revalidatePath("/catalog");
  return { success: true };
}
