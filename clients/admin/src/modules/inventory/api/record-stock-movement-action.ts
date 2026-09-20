"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { recordStockMovement } from "@/modules/inventory/api/stock-adjustments.api";
import type { RecordStockMovementRequest } from "@/modules/inventory/types/stock";

export interface RecordStockMovementFormState {
  error?: string;
  success?: boolean;
}

export async function recordStockMovementAction(
  _prevState: RecordStockMovementFormState,
  formData: FormData,
): Promise<RecordStockMovementFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const productId = String(formData.get("productId") ?? "").trim();
  const locationId = String(formData.get("locationId") ?? "").trim();
  const quantityDelta = Number(formData.get("quantityDelta") ?? 0);
  const note = String(formData.get("note") ?? "").trim();

  if (!productId || !locationId) {
    return { error: "Product and location are required." };
  }

  if (!quantityDelta) {
    return { error: "Quantity change must not be zero." };
  }

  const request: RecordStockMovementRequest = {
    productId,
    locationId,
    quantityDelta,
    note: note || undefined,
  };

  const result = await recordStockMovement(request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to record stock adjustment." };
  }

  revalidatePath("/inventory");
  return { success: true };
}
