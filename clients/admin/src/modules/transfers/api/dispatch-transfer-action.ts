"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { dispatchStockTransfer } from "@/modules/transfers/api/transfers.api";
import { isNumericId } from "@/modules/transfers/utils/params";

export interface DispatchTransferActionState {
  error?: string;
  success?: boolean;
}

/** Issues the stock from the source location. A 409 (e.g. insufficient stock) surfaces as `error`. */
export async function dispatchTransferAction(transferId: string): Promise<DispatchTransferActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(transferId)) {
    return { error: "Invalid transfer." };
  }

  const result = await dispatchStockTransfer(transferId);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to dispatch transfer." };
  }

  revalidatePath("/transfers");
  revalidatePath(`/transfers/${transferId}`);
  return { success: true };
}
