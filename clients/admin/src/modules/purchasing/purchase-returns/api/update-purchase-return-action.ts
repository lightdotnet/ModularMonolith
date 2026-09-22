"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { isNumericId } from "@/modules/purchasing/common";
import { updatePurchaseReturn } from "@/modules/purchasing/purchase-returns/api/purchase-returns.api";
import {
  validateReturnPayload,
  type ReturnPayloadInput,
} from "@/modules/purchasing/purchase-returns/utils/return-payload";

export interface UpdatePurchaseReturnActionState {
  error?: string;
  success?: boolean;
}

/** Replaces a Draft return's reason, note and lines. */
export async function updatePurchaseReturnAction(
  returnId: string,
  input: ReturnPayloadInput,
): Promise<UpdatePurchaseReturnActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(returnId)) {
    return { error: "Invalid purchase return." };
  }

  const payload = validateReturnPayload(input);
  if ("error" in payload) return { error: payload.error };

  const result = await updatePurchaseReturn(returnId, payload);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update purchase return." };
  }

  revalidatePath("/purchasing/returns");
  revalidatePath(`/purchasing/returns/${returnId}`);
  return { success: true };
}
