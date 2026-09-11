"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { hasPermission } from "@/lib/server/authorization";
import { searchApprovals } from "@/modules/approvals/api/approvals.api";
import { APPROVALS_PERMISSIONS } from "@/modules/approvals/constants/permissions";
import type { ApprovalRequestDto } from "@/modules/approvals/types/approval";

export interface SearchAllApprovalsState {
  data: ApprovalRequestDto[] | null;
  error?: string;
}

/** Client-callable, lazy fetch for the admin "All requests" tab — called once when that tab is
 * first activated. Requires `approval.requests.view_all`, re-checked here since a server action
 * is directly callable from the client regardless of what's rendered. */
export async function searchAllApprovalsAction(): Promise<SearchAllApprovalsState> {
  const session = await resolveSession();
  if (!session || !hasPermission(session, APPROVALS_PERMISSIONS.ViewAll)) {
    return { data: null, error: "You do not have permission to view all requests." };
  }

  const result = await searchApprovals({ pageSize: 50 });
  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Unable to load approval requests." };
  }

  return { data: result.data.records };
}
