"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { updateLeaveRequest } from "@/modules/leave-requests/api/leave-requests.api";
import { LEAVE_REQUESTS_PERMISSIONS } from "@/modules/leave-requests/constants/permissions";
import { LeaveType } from "@/modules/leave-requests/types/leave-request";
import type { UpdateLeaveRequestPayload } from "@/modules/leave-requests/types/leave-request";
import { hasPermission } from "@/lib/server/authorization";

export interface UpdateLeaveRequestFormState {
  error?: string;
  success?: boolean;
}

export async function updateLeaveRequestAction(
  _prevState: UpdateLeaveRequestFormState,
  formData: FormData,
): Promise<UpdateLeaveRequestFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  const leaveType = String(formData.get("leaveType") ?? "") as LeaveType;
  const startDate = String(formData.get("startDate") ?? "");
  const endDate = String(formData.get("endDate") ?? "");

  if (!id || !leaveType || !Object.values(LeaveType).includes(leaveType)) {
    return { error: "Leave type is required." };
  }

  if (!startDate || !endDate) {
    return { error: "Start date and end date are required." };
  }

  if (endDate < startDate) {
    return { error: "End date cannot be before start date." };
  }

  // Server-side source of truth for whether an approver selection is required — a manage-scoped
  // edit is metadata-only and never touches Approval, regardless of what the client sent.
  const canManage = hasPermission(session, LEAVE_REQUESTS_PERMISSIONS.Manage);
  const approverEmployeeId = String(formData.get("approverEmployeeId") ?? "").trim();

  if (!canManage && !approverEmployeeId) {
    return { error: "Please select an approver." };
  }

  const request: UpdateLeaveRequestPayload = {
    leaveType,
    startDate,
    endDate,
    reason: String(formData.get("reason") ?? "").trim() || undefined,
    approverEmployeeId: canManage ? undefined : approverEmployeeId,
  };

  const result = await updateLeaveRequest(id, request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update leave request." };
  }

  revalidatePath("/leave-requests");
  revalidatePath("/leave-requests/[id]", "page");
  return { success: true };
}
