"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { createOrder } from "@/modules/orders/api/orders.api";
import type { CreateOrderRequest } from "@/modules/orders/types/order";

export interface CreateOrderFormState {
  error?: string;
  success?: boolean;
  orderId?: string;
}

/**
 * Creates a Draft order. No `revalidatePath` — the order is invisible in the
 * default `/orders` list (Draft is hidden unless explicitly filtered for) until
 * it's placed, so there's nothing on that page for this action to refresh yet.
 */
export async function createOrderAction(
  _prevState: CreateOrderFormState,
  formData: FormData,
): Promise<CreateOrderFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const locationId = String(formData.get("locationId") ?? "").trim();
  if (!locationId) {
    return { error: "Location is required." };
  }

  const request: CreateOrderRequest = {
    locationId,
    memberId: String(formData.get("memberId") ?? "").trim() || undefined,
    orderCode: String(formData.get("orderCode") ?? "").trim() || undefined,
    externalReferenceCode: String(formData.get("externalReferenceCode") ?? "").trim() || undefined,
  };

  const result = await createOrder(request);

  if (!result.isSuccess || !result.data) {
    return { error: result.message || "Failed to create order." };
  }

  return { success: true, orderId: result.data };
}
