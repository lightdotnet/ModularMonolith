"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { unlinkEmployeeLogin } from "@/modules/organization/employees/api/employees.api";

export interface UnlinkEmployeeLoginActionState {
  error?: string;
  success?: boolean;
}

export async function unlinkEmployeeLoginAction(
  employeeId: string,
): Promise<UnlinkEmployeeLoginActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await unlinkEmployeeLogin(employeeId);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to unlink login." };
  }

  revalidatePath("/organization/employees");
  return { success: true };
}
