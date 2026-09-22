"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { dateInputToIso, isNumericId } from "@/modules/purchasing/common";
import { createPurchaseOrder } from "@/modules/purchasing/purchase-orders/api/purchase-orders.api";
import { PURCHASE_ORDER_NOTE_MAX_LENGTH } from "@/modules/purchasing/purchase-orders/types/purchase-order";

export interface CreatePurchaseOrderFormState {
  error?: string;
  success?: boolean;
  purchaseOrderId?: string;
}

/** Creates a Draft purchase order; the client then navigates to its detail page to build the lines. */
export async function createPurchaseOrderAction(
  _prevState: CreatePurchaseOrderFormState,
  formData: FormData,
): Promise<CreatePurchaseOrderFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const supplierId = String(formData.get("supplierId") ?? "").trim();
  const locationId = String(formData.get("locationId") ?? "").trim();
  const expectedAtInput = String(formData.get("expectedAt") ?? "").trim();
  const note = String(formData.get("note") ?? "").trim();

  if (!isNumericId(supplierId)) {
    return { error: "Select a supplier." };
  }

  if (!locationId || locationId.length > 450) {
    return { error: "Select a receiving location." };
  }

  const expectedAt = expectedAtInput ? dateInputToIso(expectedAtInput) : undefined;
  if (expectedAtInput && !expectedAt) {
    return { error: "Expected date is invalid." };
  }

  if (note.length > PURCHASE_ORDER_NOTE_MAX_LENGTH) {
    return { error: `Note must not exceed ${PURCHASE_ORDER_NOTE_MAX_LENGTH} characters.` };
  }

  const result = await createPurchaseOrder({
    supplierId,
    locationId,
    expectedAt,
    note: note || undefined,
  });

  if (!result.isSuccess || !result.data) {
    return { error: result.message || "Failed to create purchase order." };
  }

  revalidatePath("/purchasing/orders");
  return { success: true, purchaseOrderId: String(result.data) };
}
