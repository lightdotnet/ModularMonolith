"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getMyApprovals } from "@/modules/approvals/api/user-approvals.api";
import { ApprovalRelation, type ApprovalRequestDto } from "@/modules/approvals/types/approval";
import type { ApprovalOwnerRole } from "@/modules/approvals/components/approval-history-table";

export interface GetMyApprovalRequestsState {
  records: ApprovalRequestDto[] | null;
  /** requestId -> the current user's role(s) on that request, as tuples — a plain array survives
   * the server action boundary more predictably than a `Map`. */
  roles: [string, ApprovalOwnerRole[]][];
  error?: string;
}

/**
 * Client-callable, lazy fetch for the "My requests" tab — called once when that tab is first
 * activated. `relation=All` also includes requests where the current user is an approver at a
 * future step they haven't reached yet, so this narrows down to just "I created it" or "I decided
 * a step on it" (the two lists being merged here), tagging each row with which of those apply.
 */
export async function getMyApprovalRequestsAction(): Promise<GetMyApprovalRequestsState> {
  const session = await resolveSession();
  if (!session || !session.profile) {
    return { records: null, roles: [], error: "Your session has expired. Please sign in again." };
  }

  const result = await getMyApprovals({ relation: ApprovalRelation.All, pageSize: 50 });
  if (!result.isSuccess || !result.data) {
    return { records: null, roles: [], error: result.message || "Unable to load your requests." };
  }

  const myId = session.profile.id;
  const roles: [string, ApprovalOwnerRole[]][] = [];
  const records = result.data.records.filter((request) => {
    const requestRoles: ApprovalOwnerRole[] = [];
    if (request.requesterUserId === myId) requestRoles.push("requester");
    if (request.steps.some((step) => step.approverUserId === myId && step.decidedAt)) {
      requestRoles.push("decided");
    }
    if (requestRoles.length > 0) roles.push([request.id, requestRoles]);
    return requestRoles.length > 0;
  });

  return { records, roles };
}
