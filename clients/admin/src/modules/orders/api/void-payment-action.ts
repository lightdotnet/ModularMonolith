"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { voidPayment } from "@/modules/orders/api/payments.api";

export interface VoidPaymentActionState {
  error?: string;
  success?: boolean;
}

export async function voidPaymentAction(
  paymentId: string,
  reason: string,
): Promise<VoidPaymentActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await voidPayment(paymentId, { reason });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to void payment." };
  }

  // Voiding a payment can move the order's status back (e.g. Paid -> PartiallyPaid),
  // which the outer orders list shows — same reasoning as place/cancel/fulfill.
  revalidatePath("/orders");
  return { success: true };
}
