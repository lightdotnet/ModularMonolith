"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { deleteOrderType } from "@/modules/orders/api/order-types.api";
import type { OrderTypeCategory } from "@/modules/orders/types/order-type";

export interface DeleteTypeActionState {
  error?: string;
  success?: boolean;
}

export async function deleteTypeAction(
  id: string,
  category: OrderTypeCategory,
): Promise<DeleteTypeActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await deleteOrderType(category, id);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to delete type." };
  }

  revalidatePath("/orders");
  return { success: true };
}
