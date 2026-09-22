"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { closeStockTransfer } from "@/modules/transfers/api/transfers.api";
import { isNumericId } from "@/modules/transfers/utils/params";

export interface CloseTransferActionState {
  error?: string;
  success?: boolean;
}

/** Closes a transfer, writing off the undelivered in-transit remainder. */
export async function closeTransferAction(
  transferId: string,
  reason: string,
): Promise<CloseTransferActionState> {
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

  const result = await closeStockTransfer(transferId, { reason: trimmed });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to close transfer." };
  }

  revalidatePath("/transfers");
  revalidatePath(`/transfers/${transferId}`);
  return { success: true };
}
