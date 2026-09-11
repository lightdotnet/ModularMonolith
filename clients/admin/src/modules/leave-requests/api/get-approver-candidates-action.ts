"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getApproverCandidates } from "@/modules/leave-requests/api/leave-requests.api";
import type { ApproverCandidateDto } from "@/modules/leave-requests/types/leave-request";

export interface GetApproverCandidatesState {
  data: ApproverCandidateDto[] | null;
  error?: string;
}

export async function getApproverCandidatesAction(): Promise<GetApproverCandidatesState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await getApproverCandidates();

  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Failed to load approvers." };
  }

  return { data: result.data };
}
