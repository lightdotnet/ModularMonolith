"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { recordStockMovement } from "@/modules/inventory/api/stock-adjustments.api";
import type { RecordStockMovementRequest } from "@/modules/inventory/types/stock";
import { MAX_QUANTITY_DELTA, parseUnitCost } from "@/modules/inventory/utils/unit-cost";

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
  // Only submitted when the form rendered the revalue-gated field; the API rejects it without the permission.
  const unitCostRaw = String(formData.get("unitCost") ?? "").trim();

  if (!productId || !locationId) {
    return { error: "Product and location are required." };
  }

  if (!Number.isFinite(quantityDelta)) {
    return { error: "Enter a valid number." };
  }

  if (!quantityDelta) {
    return { error: "Quantity change must not be zero." };
  }

  if (!Number.isInteger(quantityDelta)) {
    return { error: "Quantity change must be a whole number." };
  }

  if (Math.abs(quantityDelta) > MAX_QUANTITY_DELTA) {
    return { error: "Quantity change must not exceed 1,000,000,000." };
  }

  // Empty stays omitted (never 0): the backend then defaults to the current average cost.
  let unitCost: number | undefined;
  if (unitCostRaw !== "") {
    const parsedUnitCost = parseUnitCost(unitCostRaw, { allowZero: true });
    if ("error" in parsedUnitCost) {
      return { error: parsedUnitCost.error };
    }
    unitCost = parsedUnitCost.value;
  }

  const request: RecordStockMovementRequest = {
    productId,
    locationId,
    quantityDelta,
    unitCost,
    note: note || undefined,
  };

  const result = await recordStockMovement(request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to record stock adjustment." };
  }

  revalidatePath("/inventory");
  revalidatePath("/inventory/valuation");
  return { success: true };
}
