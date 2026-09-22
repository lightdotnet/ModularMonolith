"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { revalueStock } from "@/modules/inventory/api/stock-adjustments.api";
import type { RevalueStockRequest } from "@/modules/inventory/types/stock";
import { parseUnitCost } from "@/modules/inventory/utils/unit-cost";

export interface RevalueStockFormState {
  error?: string;
  success?: boolean;
}

export async function revalueStockAction(
  _prevState: RevalueStockFormState,
  formData: FormData,
): Promise<RevalueStockFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const productId = String(formData.get("productId") ?? "").trim();
  const locationId = String(formData.get("locationId") ?? "").trim();
  const unitCostRaw = String(formData.get("unitCost") ?? "").trim();
  const note = String(formData.get("note") ?? "").trim();

  if (!productId || !locationId) {
    return { error: "Product and location are required." };
  }

  const parsedUnitCost = parseUnitCost(unitCostRaw, { allowZero: false });
  if ("error" in parsedUnitCost) {
    return { error: parsedUnitCost.error };
  }

  const request: RevalueStockRequest = {
    productId,
    locationId,
    unitCost: parsedUnitCost.value,
    note: note || undefined,
  };

  const result = await revalueStock(request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to revalue stock." };
  }

  revalidatePath("/inventory");
  revalidatePath("/inventory/valuation");
  return { success: true };
}
