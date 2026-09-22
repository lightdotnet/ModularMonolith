"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { isNumericId } from "@/modules/purchasing/common";
import { closePurchaseOrder } from "@/modules/purchasing/purchase-orders/api/purchase-orders.api";

export interface ClosePurchaseOrderActionState {
  error?: string;
  success?: boolean;
}

/** Closes a PartiallyReceived purchase order, abandoning the outstanding quantity. */
export async function closePurchaseOrderAction(
  purchaseOrderId: string,
  reason: string,
): Promise<ClosePurchaseOrderActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(purchaseOrderId)) {
    return { error: "Invalid purchase order." };
  }

  const trimmed = reason.trim();
  if (!trimmed) {
    return { error: "A reason is required." };
  }

  if (trimmed.length > 1000) {
    return { error: "Reason must not exceed 1000 characters." };
  }

  const result = await closePurchaseOrder(purchaseOrderId, { reason: trimmed });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to close purchase order." };
  }

  revalidatePath("/purchasing/orders");
  revalidatePath(`/purchasing/orders/${purchaseOrderId}`);
  return { success: true };
}
