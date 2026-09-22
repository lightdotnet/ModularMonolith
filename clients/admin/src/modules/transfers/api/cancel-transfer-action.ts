"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { cancelStockTransfer } from "@/modules/transfers/api/transfers.api";
import { isNumericId } from "@/modules/transfers/utils/params";

export interface CancelTransferActionState {
  error?: string;
  success?: boolean;
}

/** Cancels a Draft transfer. */
export async function cancelTransferAction(
  transferId: string,
  reason: string,
): Promise<CancelTransferActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(transferId)) {
    return { error: "Invalid transfer." };
  }

  const trimmed = reason.trim();
  if (!trimmed) {
    return { error: "A reason is required." };
  }

  if (trimmed.length > 1000) {
    return { error: "Reason must not exceed 1000 characters." };
  }

  const result = await cancelStockTransfer(transferId, { reason: trimmed });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to cancel transfer." };
  }

  revalidatePath("/transfers");
  revalidatePath(`/transfers/${transferId}`);
  return { success: true };
}
