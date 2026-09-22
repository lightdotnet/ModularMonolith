"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { placeOrder } from "@/modules/orders/api/orders.api";

export interface PlaceOrderActionState {
  error?: string;
  success?: boolean;
}

export async function placeOrderAction(orderId: string): Promise<PlaceOrderActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await placeOrder(orderId);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to place order." };
  }

  revalidatePath("/orders");
  return { success: true };
}
