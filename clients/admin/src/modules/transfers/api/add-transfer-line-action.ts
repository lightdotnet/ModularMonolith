"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { addStockTransferLine } from "@/modules/transfers/api/transfers.api";
import { MAX_TRANSFER_LINE_QUANTITY } from "@/modules/transfers/types/transfer";
import { isNumericId } from "@/modules/transfers/utils/params";

export interface AddTransferLineActionState {
  error?: string;
  success?: boolean;
}

export async function addTransferLineAction(
  transferId: string,
  productId: string,
  quantity: number,
): Promise<AddTransferLineActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(transferId) || !isNumericId(productId)) {
    return { error: "Invalid transfer or product." };
  }

  if (!Number.isInteger(quantity) || quantity < 1 || quantity > MAX_TRANSFER_LINE_QUANTITY) {
    return { error: "Quantity must be a whole number between 1 and 1,000,000." };
  }

  const result = await addStockTransferLine(transferId, { productId, quantity });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to add product." };
  }

  revalidatePath("/transfers");
  revalidatePath(`/transfers/${transferId}`);
  return { success: true };
}
