"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { updateOrderLineQuantity } from "@/modules/orders/api/orders.api";

export interface UpdateOrderLineQuantityActionState {
  error?: string;
  success?: boolean;
}

export async function updateOrderLineQuantityAction(
  orderId: string,
  lineId: string,
  quantity: number,
): Promise<UpdateOrderLineQuantityActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await updateOrderLineQuantity(orderId, lineId, { quantity });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update quantity." };
  }

  return { success: true };
}
