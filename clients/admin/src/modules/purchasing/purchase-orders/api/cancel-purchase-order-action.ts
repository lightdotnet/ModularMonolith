"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { isNumericId } from "@/modules/purchasing/common";
import { cancelPurchaseOrder } from "@/modules/purchasing/purchase-orders/api/purchase-orders.api";

export interface CancelPurchaseOrderActionState {
  error?: string;
  success?: boolean;
}

/** Cancels a Draft/Rejected purchase order, or an Approved one with nothing received. */
export async function cancelPurchaseOrderAction(
  purchaseOrderId: string,
  reason: string,
): Promise<CancelPurchaseOrderActionState> {
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

  const result = await cancelPurchaseOrder(purchaseOrderId, { reason: trimmed });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to cancel purchase order." };
  }

  revalidatePath("/purchasing/orders");
  revalidatePath(`/purchasing/orders/${purchaseOrderId}`);
  return { success: true };
}
