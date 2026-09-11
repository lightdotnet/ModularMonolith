"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getMyApprovals } from "@/modules/approvals/api/user-approvals.api";
import { ApprovalRelation, type ApprovalRequestDto } from "@/modules/approvals/types/approval";

export interface GetAwaitingApprovalsState {
  data: ApprovalRequestDto[] | null;
  error?: string;
}

/** Client-callable, lazy fetch for the "Waiting on your decision" tab — called once when that
 * tab is first activated, instead of eagerly on every page navigation. */
export async function getAwaitingApprovalsAction(): Promise<GetAwaitingApprovalsState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await getMyApprovals({
    relation: ApprovalRelation.AwaitingMyDecision,
    pageSize: 50,
  });
  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Unable to load your approvals." };
  }

  return { data: result.data.records };
}
