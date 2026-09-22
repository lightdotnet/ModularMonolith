"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { isNumericId } from "@/modules/purchasing/common";
import { submitPurchaseOrder } from "@/modules/purchasing/purchase-orders/api/purchase-orders.api";

export interface SubmitPurchaseOrderActionState {
  error?: string;
  success?: boolean;
}

/** Submits (or resubmits after a rejection) a purchase order for approval by the chosen approver. */
export async function submitPurchaseOrderAction(
  purchaseOrderId: string,
  approverEmployeeId: string,
): Promise<SubmitPurchaseOrderActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(purchaseOrderId)) {
    return { error: "Invalid purchase order." };
  }

  const approver = approverEmployeeId.trim();
  if (!approver) {
    return { error: "Select an approver." };
  }

  if (approver.length > 450) {
    return { error: "Invalid approver." };
  }

  const result = await submitPurchaseOrder(purchaseOrderId, { approverEmployeeId: approver });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to submit purchase order." };
  }

  revalidatePath("/purchasing/orders");
  revalidatePath(`/purchasing/orders/${purchaseOrderId}`);
  return { success: true };
}
