"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createCurrency } from "@/modules/currency/currencies/api/currencies.api";
import { readCreateCurrencyForm } from "@/modules/currency/currencies/utils/currency-form";

export interface CreateCurrencyFormState {
  error?: string;
  success?: boolean;
}

export async function createCurrencyAction(
  _prevState: CreateCurrencyFormState,
  formData: FormData,
): Promise<CreateCurrencyFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const parsed = readCreateCurrencyForm(formData);
  if ("error" in parsed) return { error: parsed.error };

  const result = await createCurrency(parsed.request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create currency." };
  }

  revalidatePath("/currency/currencies");
  revalidatePath("/catalog");
  return { success: true };
}
