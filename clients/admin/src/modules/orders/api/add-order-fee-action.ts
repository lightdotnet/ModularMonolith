"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { addOrderFee } from "@/modules/orders/api/orders.api";
import type { AddOrderFeeRequest } from "@/modules/orders/types/order";

export interface AddOrderFeeActionState {
  error?: string;
  success?: boolean;
}

export async function addOrderFeeAction(
  orderId: string,
  request: AddOrderFeeRequest,
): Promise<AddOrderFeeActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await addOrderFee(orderId, request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to add fee." };
  }

  return { success: true };
}
