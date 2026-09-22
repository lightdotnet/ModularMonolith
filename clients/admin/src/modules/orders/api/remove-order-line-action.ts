"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { removeOrderLine } from "@/modules/orders/api/orders.api";

export interface RemoveOrderLineActionState {
  error?: string;
  success?: boolean;
}

export async function removeOrderLineAction(
  orderId: string,
  lineId: string,
): Promise<RemoveOrderLineActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await removeOrderLine(orderId, lineId);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to remove line." };
  }

  return { success: true };
}
