"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createLeaveRequest } from "@/modules/leave-requests/api/leave-requests.api";
import { LeaveType } from "@/modules/leave-requests/types/leave-request";
import type { CreateLeaveRequestPayload } from "@/modules/leave-requests/types/leave-request";

export interface CreateLeaveRequestFormState {
  error?: string;
  success?: boolean;
}

export async function createLeaveRequestAction(
  _prevState: CreateLeaveRequestFormState,
  formData: FormData,
): Promise<CreateLeaveRequestFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const leaveType = String(formData.get("leaveType") ?? "") as LeaveType;
  const startDate = String(formData.get("startDate") ?? "");
  const endDate = String(formData.get("endDate") ?? "");
  const approverEmployeeId = String(formData.get("approverEmployeeId") ?? "").trim();

  if (!leaveType || !Object.values(LeaveType).includes(leaveType)) {
    return { error: "Leave type is required." };
  }

  if (!startDate || !endDate) {
    return { error: "Start date and end date are required." };
  }

  if (endDate < startDate) {
    return { error: "End date cannot be before start date." };
  }

  if (!approverEmployeeId) {
    return { error: "Please select an approver." };
  }

  const request: CreateLeaveRequestPayload = {
    leaveType,
    startDate,
    endDate,
    reason: String(formData.get("reason") ?? "").trim() || undefined,
    approverEmployeeId,
  };

  const result = await createLeaveRequest(request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to submit leave request." };
  }

  revalidatePath("/leave-requests");
  return { success: true };
}
