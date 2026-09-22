"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { isNumericId } from "@/modules/purchasing/common";
import { withdrawPurchaseOrder } from "@/modules/purchasing/purchase-orders/api/purchase-orders.api";

export interface WithdrawPurchaseOrderActionState {
  error?: string;
  success?: boolean;
}

/** Withdraws a PendingApproval purchase order back to editable state. */
export async function withdrawPurchaseOrderAction(
  purchaseOrderId: string,
): Promise<WithdrawPurchaseOrderActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(purchaseOrderId)) {
    return { error: "Invalid purchase order." };
  }

  const result = await withdrawPurchaseOrder(purchaseOrderId);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to withdraw purchase order." };
  }

  revalidatePath("/purchasing/orders");
  revalidatePath(`/purchasing/orders/${purchaseOrderId}`);
  return { success: true };
}
