"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { deleteEmployee } from "@/modules/organization/employees/api/employees.api";

export interface DeleteEmployeeActionState {
  error?: string;
  success?: boolean;
}

export async function deleteEmployeeAction(id: string): Promise<DeleteEmployeeActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await deleteEmployee(id);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to delete employee." };
  }

  revalidatePath("/organization/employees");
  return { success: true };
}
