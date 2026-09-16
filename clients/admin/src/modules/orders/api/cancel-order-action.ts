"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { cancelOrder } from "@/modules/orders/api/orders.api";

export interface CancelOrderActionState {
  error?: string;
  success?: boolean;
}

export async function cancelOrderAction(
  orderId: string,
  reason: string,
): Promise<CancelOrderActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await cancelOrder(orderId, { reason });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to cancel order." };
  }

  revalidatePath("/orders");
  return { success: true };
}
