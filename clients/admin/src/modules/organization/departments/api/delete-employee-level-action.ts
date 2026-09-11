"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { deleteEmployeeLevel } from "@/modules/organization/departments/api/employee-levels.api";

export interface DeleteEmployeeLevelActionState {
  error?: string;
  success?: boolean;
}

export async function deleteEmployeeLevelAction(
  id: string,
): Promise<DeleteEmployeeLevelActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await deleteEmployeeLevel(id);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to delete level." };
  }

  revalidatePath("/organization/departments");
  return { success: true };
}
