"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { isNumericId } from "@/modules/purchasing/common";
import { cancelPurchaseReturn } from "@/modules/purchasing/purchase-returns/api/purchase-returns.api";

export interface CancelPurchaseReturnActionState {
  error?: string;
  success?: boolean;
}

/** Cancels a Draft return. */
export async function cancelPurchaseReturnAction(
  returnId: string,
  reason: string,
): Promise<CancelPurchaseReturnActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(returnId)) {
    return { error: "Invalid purchase return." };
  }

  const trimmed = reason.trim();
  if (!trimmed) {
    return { error: "A reason is required." };
  }

  if (trimmed.length > 1000) {
    return { error: "Reason must not exceed 1000 characters." };
  }

  const result = await cancelPurchaseReturn(returnId, { reason: trimmed });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to cancel purchase return." };
  }

  revalidatePath("/purchasing/returns");
  revalidatePath(`/purchasing/returns/${returnId}`);
  return { success: true };
}
