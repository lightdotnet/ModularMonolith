"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { CURRENCY_CODE_PATTERN } from "@/modules/currency/common/constants/limits";
import { activateCurrency, deactivateCurrency } from "@/modules/currency/currencies/api/currencies.api";

export interface SetCurrencyStatusActionState {
  error?: string;
  success?: boolean;
}

/** Activates or deactivates a currency (the base currency cannot be deactivated — the backend refuses). */
export async function setCurrencyStatusAction(
  code: string,
  activate: boolean,
): Promise<SetCurrencyStatusActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!CURRENCY_CODE_PATTERN.test(code)) {
    return { error: "Invalid currency." };
  }

  const result = activate ? await activateCurrency(code) : await deactivateCurrency(code);

  if (!result.isSuccess) {
    return { error: result.message || `Failed to ${activate ? "activate" : "deactivate"} currency.` };
  }

  revalidatePath("/currency/currencies");
  revalidatePath("/currency/exchange-rates");
  revalidatePath("/catalog");
  return { success: true };
}
