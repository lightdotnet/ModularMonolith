"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { deleteLeaveRequest } from "@/modules/leave-requests/api/leave-requests.api";

export interface DeleteLeaveRequestActionState {
  error?: string;
  success?: boolean;
}

export async function deleteLeaveRequestAction(id: string): Promise<DeleteLeaveRequestActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await deleteLeaveRequest(id);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to delete leave request." };
  }

  revalidatePath("/leave-requests");
  return { success: true };
}
