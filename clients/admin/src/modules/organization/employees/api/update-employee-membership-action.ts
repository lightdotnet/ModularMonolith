"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { updateEmployeeMembership } from "@/modules/organization/employees/api/employees.api";
import type { UpdateEmployeeMembershipRequest } from "@/modules/organization/employees/types/employee";

export interface UpdateEmployeeMembershipActionState {
  error?: string;
  success?: boolean;
}

export async function updateEmployeeMembershipAction(
  employeeId: string,
  orgUnitId: string,
  request: UpdateEmployeeMembershipRequest,
): Promise<UpdateEmployeeMembershipActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await updateEmployeeMembership(employeeId, orgUnitId, request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update." };
  }

  revalidatePath("/organization/employees");
  return { success: true };
}
