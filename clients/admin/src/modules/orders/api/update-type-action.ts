"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { updateOrderType } from "@/modules/orders/api/order-types.api";
import { OrderTypeCategory, OrderTypeStatus } from "@/modules/orders/types/order-type";

export interface UpdateTypeFormState {
  error?: string;
  success?: boolean;
}

export async function updateTypeAction(
  _prevState: UpdateTypeFormState,
  formData: FormData,
): Promise<UpdateTypeFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const category = String(formData.get("category") ?? "") as OrderTypeCategory;
  const id = String(formData.get("id") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const status = String(formData.get("status") ?? "Active");

  if (category !== OrderTypeCategory.Fee && category !== OrderTypeCategory.Payment) {
    return { error: "Category is required." };
  }

  if (!id || !name) {
    return { error: "Name is required." };
  }

  const result = await updateOrderType(category, id, { name, status: status as OrderTypeStatus });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update type." };
  }

  revalidatePath("/orders");
  return { success: true };
}
