"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { addOrderLine } from "@/modules/orders/api/orders.api";
import type { AddOrderLineRequest } from "@/modules/orders/types/order";

export interface AddOrderLineActionState {
  error?: string;
  success?: boolean;
}

export async function addOrderLineAction(
  orderId: string,
  request: AddOrderLineRequest,
): Promise<AddOrderLineActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await addOrderLine(orderId, request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to add product." };
  }

  return { success: true };
}
