"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { decideApproval } from "@/modules/approvals/api/user-approvals.api";

export interface DecideApprovalState {
  error?: string;
  success?: boolean;
}

export async function decideApprovalAction(
  approvalRequestId: string,
  approved: boolean,
  comment: string,
): Promise<DecideApprovalState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!approved && !comment.trim()) {
    return { error: "A reason is required when rejecting a request." };
  }

  const result = await decideApproval(approvalRequestId, {
    approved,
    comment: comment.trim() || undefined,
  });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to record your decision." };
  }

  revalidatePath("/approvals/requests");
  revalidatePath("/approvals/requests/[id]", "page");
  return { success: true };
}
