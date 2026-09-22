"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { removeStockTransferLine } from "@/modules/transfers/api/transfers.api";
import { isNumericId } from "@/modules/transfers/utils/params";

export interface RemoveTransferLineActionState {
  error?: string;
  success?: boolean;
}

export async function removeTransferLineAction(
  transferId: string,
  lineId: string,
): Promise<RemoveTransferLineActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(transferId) || !isNumericId(lineId)) {
    return { error: "Invalid transfer or line." };
  }

  const result = await removeStockTransferLine(transferId, lineId);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to remove line." };
  }

  revalidatePath("/transfers");
  revalidatePath(`/transfers/${transferId}`);
  return { success: true };
}
