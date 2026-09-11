"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { removeEmployeeFromOrgUnit } from "@/modules/organization/employees/api/employees.api";

export interface RemoveEmployeeOrgUnitActionState {
  error?: string;
  success?: boolean;
}

export async function removeEmployeeOrgUnitAction(
  employeeId: string,
  orgUnitId: string,
): Promise<RemoveEmployeeOrgUnitActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await removeEmployeeFromOrgUnit(employeeId, orgUnitId);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to remove." };
  }

  revalidatePath("/organization/employees");
  return { success: true };
}
