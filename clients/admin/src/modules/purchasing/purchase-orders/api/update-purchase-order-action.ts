"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { dateInputToIso, isNumericId } from "@/modules/purchasing/common";
import { updatePurchaseOrder } from "@/modules/purchasing/purchase-orders/api/purchase-orders.api";
import { PURCHASE_ORDER_NOTE_MAX_LENGTH } from "@/modules/purchasing/purchase-orders/types/purchase-order";

export interface UpdatePurchaseOrderFormState {
  error?: string;
  success?: boolean;
}

/** Updates a Draft/Rejected purchase order's header (supplier, receiving location, expected date, note). */
export async function updatePurchaseOrderAction(
  _prevState: UpdatePurchaseOrderFormState,
  formData: FormData,
): Promise<UpdatePurchaseOrderFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  const supplierId = String(formData.get("supplierId") ?? "").trim();
  const locationId = String(formData.get("locationId") ?? "").trim();
  const expectedAtInput = String(formData.get("expectedAt") ?? "").trim();
  const note = String(formData.get("note") ?? "").trim();

  if (!isNumericId(id)) {
    return { error: "Invalid purchase order." };
  }

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

  const result = await updatePurchaseOrder(id, {
    supplierId,
    locationId,
    expectedAt,
    note: note || undefined,
  });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update purchase order." };
  }

  revalidatePath("/purchasing/orders");
  revalidatePath(`/purchasing/orders/${id}`);
  return { success: true };
}
