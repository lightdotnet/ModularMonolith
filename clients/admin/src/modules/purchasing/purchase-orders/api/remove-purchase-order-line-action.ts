"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { isNumericId } from "@/modules/purchasing/common";
import { removePurchaseOrderLine } from "@/modules/purchasing/purchase-orders/api/purchase-orders.api";

export interface RemovePurchaseOrderLineActionState {
  error?: string;
  success?: boolean;
}

export async function removePurchaseOrderLineAction(
  purchaseOrderId: string,
  lineId: string,
): Promise<RemovePurchaseOrderLineActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(purchaseOrderId) || !isNumericId(lineId)) {
    return { error: "Invalid purchase order or line." };
  }

  const result = await removePurchaseOrderLine(purchaseOrderId, lineId);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to remove line." };
  }

  revalidatePath("/purchasing/orders");
  revalidatePath(`/purchasing/orders/${purchaseOrderId}`);
  return { success: true };
}
