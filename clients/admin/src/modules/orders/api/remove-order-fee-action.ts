"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { removeOrderFee } from "@/modules/orders/api/orders.api";

export interface RemoveOrderFeeActionState {
  error?: string;
  success?: boolean;
}

export async function removeOrderFeeAction(
  orderId: string,
  feeId: string,
): Promise<RemoveOrderFeeActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await removeOrderFee(orderId, feeId);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to remove fee." };
  }

  return { success: true };
}
