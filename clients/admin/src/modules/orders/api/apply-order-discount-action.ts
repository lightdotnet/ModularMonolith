"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { applyOrderDiscount } from "@/modules/orders/api/orders.api";
import type { ApplyOrderDiscountRequest } from "@/modules/orders/types/order";

export interface ApplyOrderDiscountActionState {
  error?: string;
  success?: boolean;
}

export async function applyOrderDiscountAction(
  orderId: string,
  request: ApplyOrderDiscountRequest,
): Promise<ApplyOrderDiscountActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await applyOrderDiscount(orderId, request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to apply discount." };
  }

  return { success: true };
}
