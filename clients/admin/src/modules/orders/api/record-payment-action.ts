"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { recordPayment } from "@/modules/orders/api/payments.api";
import type { RecordPaymentRequest } from "@/modules/orders/types/order";

export interface RecordPaymentActionState {
  error?: string;
  success?: boolean;
}

export async function recordPaymentAction(
  orderId: string,
  request: RecordPaymentRequest,
): Promise<RecordPaymentActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await recordPayment(orderId, request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to record payment." };
  }

  // Recording a payment can move the order's status (Placed -> PartiallyPaid/Paid),
  // which the outer orders list shows — same reasoning as place/cancel/fulfill.
  revalidatePath("/orders");
  return { success: true };
}
