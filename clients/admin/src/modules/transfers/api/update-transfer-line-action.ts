"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { updateStockTransferLine } from "@/modules/transfers/api/transfers.api";
import { MAX_TRANSFER_LINE_QUANTITY } from "@/modules/transfers/types/transfer";
import { isNumericId } from "@/modules/transfers/utils/params";

export interface UpdateTransferLineActionState {
  error?: string;
  success?: boolean;
}

export async function updateTransferLineAction(
  transferId: string,
  lineId: string,
  quantity: number,
): Promise<UpdateTransferLineActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(transferId) || !isNumericId(lineId)) {
    return { error: "Invalid transfer or line." };
  }

  if (!Number.isInteger(quantity) || quantity < 1 || quantity > MAX_TRANSFER_LINE_QUANTITY) {
    return { error: "Quantity must be a whole number between 1 and 1,000,000." };
  }

  const result = await updateStockTransferLine(transferId, lineId, { quantity });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update quantity." };
  }

  revalidatePath("/transfers");
  revalidatePath(`/transfers/${transferId}`);
  return { success: true };
}
