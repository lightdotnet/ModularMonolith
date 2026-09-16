"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { setOrderLineSalePrice } from "@/modules/orders/api/orders.api";

export interface UpdateOrderLineSalePriceActionState {
  error?: string;
  success?: boolean;
}

/** `salePrice: null` clears a previously-set sale-price override back to the line's regular unit price. */
export async function updateOrderLineSalePriceAction(
  orderId: string,
  lineId: string,
  salePrice: number | null,
): Promise<UpdateOrderLineSalePriceActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await setOrderLineSalePrice(orderId, lineId, { salePrice });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update sale price." };
  }

  return { success: true };
}
