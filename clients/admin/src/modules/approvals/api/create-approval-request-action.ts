"use server";

import { randomUUID } from "crypto";
import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createApprovalRequest } from "@/modules/approvals/api/user-approvals.api";
import { EMPLOYEE_ID_CLAIM_TYPE } from "@/modules/approvals/constants/claims";
import { getDisplayName } from "@/lib/shared/user-display";
import type { ApproverStepInput } from "@/modules/approvals/types/approval";

export interface CreateApprovalRequestState {
  error?: string;
  success?: boolean;
}

export interface CreateApprovalRequestInput {
  requestType: string;
  title: string;
  content?: string;
  /** Optional catalog document type; also drives `requestType` when set. */
  documentTypeId?: string;
  approvers: { userId: string; employeeId: string; name: string }[];
}

/**
 * Self-service create, available to any authenticated user — creates a
 * request via `POST user_approval`, which overrides `requesterUserId` to the
 * caller server-side regardless of what's sent here (the value below is sent
 * only to satisfy the shared `CreateApprovalRequest` contract shape). The
 * picked approvers become the chain (level = row order). Each approver carries
 * both its Identity `userId` and the real Organization `employeeId` behind it,
 * sourced from the linked-employee picker. For the admin/test harness that can
 * pick an arbitrary requester chain via `POST approval`, see
 * `createTestApprovalRequestAction`.
 */
export async function createApprovalRequestAction(
  input: CreateApprovalRequestInput,
): Promise<CreateApprovalRequestState> {
  const session = await resolveSession();
  if (!session || !session.profile) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!input.title.trim()) {
    return { error: "Title is required." };
  }

  if (input.approvers.length === 0) {
    return { error: "At least one approver level is required." };
  }

  if (input.approvers.some((approver) => !approver.userId || !approver.employeeId)) {
    return { error: "Every approver level needs a linked employee." };
  }

  const employeeIds = input.approvers.map((approver) => approver.employeeId);
  if (new Set(employeeIds).size !== employeeIds.length) {
    return { error: "The same employee appears at more than one level." };
  }

  const approverChain: ApproverStepInput[] = input.approvers.map((approver, index) => ({
    level: index + 1,
    approverUserId: approver.userId,
    approverEmployeeId: approver.employeeId,
    approverName: approver.name || undefined,
  }));

  const requesterId = session.profile.id;
  // `POST user_approval` overrides `requesterUserId` + `requesterEmployeeId` server-side from the
  // caller's identity + `employee_id` claim; `requesterName` is a cosmetic label the server keeps
  // as sent (the JWT has no name claim), so resolve it here from the session profile.
  const requesterEmployeeId = session.claims.find(
    (claim) => claim.type === EMPLOYEE_ID_CLAIM_TYPE,
  )?.value;

  const result = await createApprovalRequest({
    requestType: input.requestType.trim() || "General",
    requestId: randomUUID(),
    requesterUserId: requesterId,
    requesterEmployeeId: requesterEmployeeId || undefined,
    requesterName: getDisplayName(session.profile) || undefined,
    title: input.title.trim(),
    content: input.content?.trim() || undefined,
    documentTypeId: input.documentTypeId || undefined,
    approverChain,
  });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create approval request." };
  }

  revalidatePath("/approvals/requests");
  return { success: true };
}
