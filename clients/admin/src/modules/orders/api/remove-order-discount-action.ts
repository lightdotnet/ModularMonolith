"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { removeOrderDiscount } from "@/modules/orders/api/orders.api";

export interface RemoveOrderDiscountActionState {
  error?: string;
  success?: boolean;
}

export async function removeOrderDiscountAction(
  orderId: string,
): Promise<RemoveOrderDiscountActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await removeOrderDiscount(orderId);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to remove discount." };
  }

  return { success: true };
}
