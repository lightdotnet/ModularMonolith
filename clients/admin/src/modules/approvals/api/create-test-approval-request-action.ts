"use server";

import { randomUUID } from "crypto";
import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createTestApprovalRequest } from "@/modules/approvals/api/approvals.api";
import { getDisplayName } from "@/lib/shared/user-display";
import type { ApproverStepInput } from "@/modules/approvals/types/approval";
import { EMPLOYEE_ID_CLAIM_TYPE } from "@/modules/approvals/constants/claims";
import type { CreateApprovalRequestInput, CreateApprovalRequestState } from "@/modules/approvals/api/create-approval-request-action";

/**
 * Admin/test harness for the generic Approval engine — creates a request via
 * `POST approval` (requires `approval.requests.view_all`), with the picked
 * approvers as the chain (level = row order). A real request type (e.g.
 * Leave) resolves its chain server-side via `IApprovalService` instead of
 * going through this HTTP endpoint; a regular user creating a request for
 * themselves uses `createApprovalRequestAction` instead.
 *
 * Each approver carries both its Identity `userId` and the real Organization
 * `employeeId` behind it, sourced from the linked-employee picker.
 */
export async function createTestApprovalRequestAction(
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
  // `POST approval` does not override the requester server-side, so resolve the real
  // linked employee id from the caller's `employee_id` claim (omitted when not linked).
  const requesterEmployeeId = session.claims.find(
    (claim) => claim.type === EMPLOYEE_ID_CLAIM_TYPE,
  )?.value;

  const result = await createTestApprovalRequest({
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
