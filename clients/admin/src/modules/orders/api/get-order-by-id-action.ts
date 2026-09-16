"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getOrderById } from "@/modules/orders/api/orders.api";
import type { OrderDto } from "@/modules/orders/types/order";

export interface GetOrderByIdState {
  data: OrderDto | null;
  error?: string;
}

/** Used by the order panel to (re-)fetch the authoritative current order — every builder mutation calls this afterward. */
export async function getOrderByIdAction(id: string): Promise<GetOrderByIdState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await getOrderById(id);

  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Failed to load order." };
  }

  return { data: result.data };
}
