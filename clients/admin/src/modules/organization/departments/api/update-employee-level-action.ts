"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { updateEmployeeLevel } from "@/modules/organization/departments/api/employee-levels.api";
import type { EmployeeLevelDto } from "@/modules/organization/departments/types/employee-level";

export interface UpdateEmployeeLevelFormState {
  error?: string;
  success?: boolean;
}

export async function updateEmployeeLevelAction(
  _prevState: UpdateEmployeeLevelFormState,
  formData: FormData,
): Promise<UpdateEmployeeLevelFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  const companyId = String(formData.get("companyId") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const code = String(formData.get("code") ?? "").trim();
  const rank = Number(formData.get("rank") ?? 0);

  if (!id || !companyId || !name || !code) {
    return { error: "Name and code are required." };
  }

  const level: EmployeeLevelDto = {
    id,
    companyId,
    name,
    code,
    rank,
    description: String(formData.get("description") ?? "") || undefined,
  };

  const result = await updateEmployeeLevel(level);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update level." };
  }

  revalidatePath("/organization/departments");
  return { success: true };
}
