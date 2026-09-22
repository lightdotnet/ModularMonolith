"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { isNumericId } from "@/modules/purchasing/common";
import { postPurchaseReturn } from "@/modules/purchasing/purchase-returns/api/purchase-returns.api";

export interface PostPurchaseReturnActionState {
  error?: string;
  success?: boolean;
}

/** Posts a Draft return: the returned stock leaves the location (insufficient stock is a backend 409). */
export async function postPurchaseReturnAction(
  returnId: string,
  purchaseOrderId?: string,
): Promise<PostPurchaseReturnActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(returnId)) {
    return { error: "Invalid purchase return." };
  }

  const result = await postPurchaseReturn(returnId);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to post purchase return." };
  }

  revalidatePath("/purchasing/returns");
  revalidatePath(`/purchasing/returns/${returnId}`);
  // Posting changes the order's returned quantities.
  revalidatePath("/purchasing/orders");
  if (isNumericId(purchaseOrderId)) revalidatePath(`/purchasing/orders/${purchaseOrderId}`);
  return { success: true };
}
