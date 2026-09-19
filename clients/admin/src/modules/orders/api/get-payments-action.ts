"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getPayments } from "@/modules/orders/api/payments.api";
import type { PaymentDto } from "@/modules/orders/types/order";

export interface GetPaymentsState {
  data?: PaymentDto[];
  error?: string;
}

/** Used by the payments section to (re-)fetch an order's payment history — payments aren't nested on `OrderDto`. */
export async function getPaymentsAction(orderId: string): Promise<GetPaymentsState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await getPayments(orderId);

  if (!result.isSuccess || !result.data) {
    return { error: result.message || "Failed to load payments." };
  }

  return { data: result.data };
}
