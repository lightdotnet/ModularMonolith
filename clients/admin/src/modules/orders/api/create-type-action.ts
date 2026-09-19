"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createOrderType } from "@/modules/orders/api/order-types.api";
import { OrderTypeCategory } from "@/modules/orders/types/order-type";

export interface CreateTypeFormState {
  error?: string;
  success?: boolean;
}

export async function createTypeAction(
  _prevState: CreateTypeFormState,
  formData: FormData,
): Promise<CreateTypeFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const category = String(formData.get("category") ?? "") as OrderTypeCategory;
  const id = String(formData.get("id") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();

  if (category !== OrderTypeCategory.Fee && category !== OrderTypeCategory.Payment) {
    return { error: "Category is required." };
  }

  if (!id || !name) {
    return { error: "ID and name are required." };
  }

  const result = await createOrderType({ id, category, name });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create type." };
  }

  revalidatePath("/orders");
  return { success: true };
}
