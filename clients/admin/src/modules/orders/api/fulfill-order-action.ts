"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { fulfillOrder } from "@/modules/orders/api/orders.api";

export interface FulfillOrderActionState {
  error?: string;
  success?: boolean;
}

export async function fulfillOrderAction(orderId: string): Promise<FulfillOrderActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await fulfillOrder(orderId);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to fulfill order." };
  }

  revalidatePath("/orders");
  return { success: true };
}
