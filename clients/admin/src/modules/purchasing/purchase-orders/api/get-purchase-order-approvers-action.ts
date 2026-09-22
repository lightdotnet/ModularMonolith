"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getPurchaseOrderApprovers } from "@/modules/purchasing/purchase-orders/api/purchase-orders.api";
import type { PurchaseOrderApproverDto } from "@/modules/purchasing/purchase-orders/types/purchase-order";

export interface GetPurchaseOrderApproversState {
  data: PurchaseOrderApproverDto[] | null;
  error?: string;
}

/** On-demand read for the submit dialog's approver picker. */
export async function getPurchaseOrderApproversAction(): Promise<GetPurchaseOrderApproversState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await getPurchaseOrderApprovers();

  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Failed to load approvers." };
  }

  return { data: result.data };
}
