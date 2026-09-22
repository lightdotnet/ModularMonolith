"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { isNumericId } from "@/modules/purchasing/common";
import { createPurchaseReturn } from "@/modules/purchasing/purchase-returns/api/purchase-returns.api";
import {
  validateReturnPayload,
  type ReturnPayloadInput,
} from "@/modules/purchasing/purchase-returns/utils/return-payload";

export interface CreatePurchaseReturnActionState {
  error?: string;
  success?: boolean;
  returnId?: string;
}

/** Creates a Draft return from a Posted goods receipt; the client then opens its detail page. */
export async function createPurchaseReturnAction(
  goodsReceiptId: string,
  input: ReturnPayloadInput,
): Promise<CreatePurchaseReturnActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(goodsReceiptId)) {
    return { error: "Invalid goods receipt." };
  }

  const payload = validateReturnPayload(input);
  if ("error" in payload) return { error: payload.error };

  const result = await createPurchaseReturn({ goodsReceiptId, ...payload });

  if (!result.isSuccess || !result.data) {
    return { error: result.message || "Failed to create purchase return." };
  }

  revalidatePath("/purchasing/returns");
  revalidatePath(`/purchasing/receipts/${goodsReceiptId}`);
  return { success: true, returnId: String(result.data) };
}
