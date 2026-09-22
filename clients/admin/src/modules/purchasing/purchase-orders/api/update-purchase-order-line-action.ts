"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { isNumericId, isValidAmount, isValidQuantity } from "@/modules/purchasing/common";
import { updatePurchaseOrderLine } from "@/modules/purchasing/purchase-orders/api/purchase-orders.api";

export interface UpdatePurchaseOrderLineActionState {
  error?: string;
  success?: boolean;
}

export async function updatePurchaseOrderLineAction(
  purchaseOrderId: string,
  lineId: string,
  quantity: number,
  unitCost: number,
): Promise<UpdatePurchaseOrderLineActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(purchaseOrderId) || !isNumericId(lineId)) {
    return { error: "Invalid purchase order or line." };
  }

  if (!isValidQuantity(quantity)) {
    return { error: "Quantity must be a whole number between 1 and 1,000,000." };
  }

  if (!isValidAmount(unitCost)) {
    return { error: "Unit cost must be between 0 and 1,000,000,000 with at most 4 decimals." };
  }

  const result = await updatePurchaseOrderLine(purchaseOrderId, lineId, { quantity, unitCost });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update line." };
  }

  revalidatePath("/purchasing/orders");
  revalidatePath(`/purchasing/orders/${purchaseOrderId}`);
  return { success: true };
}
