"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createEmployeeLevel } from "@/modules/organization/departments/api/employee-levels.api";
import type { CreateEmployeeLevelRequest } from "@/modules/organization/departments/types/employee-level";

export interface CreateEmployeeLevelFormState {
  error?: string;
  success?: boolean;
}

export async function createEmployeeLevelAction(
  _prevState: CreateEmployeeLevelFormState,
  formData: FormData,
): Promise<CreateEmployeeLevelFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const companyId = String(formData.get("companyId") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const code = String(formData.get("code") ?? "").trim();
  const rank = Number(formData.get("rank") ?? 0);

  if (!companyId || !name || !code) {
    return { error: "Name and code are required." };
  }

  const request: CreateEmployeeLevelRequest = {
    companyId,
    name,
    code,
    rank,
    description: String(formData.get("description") ?? "") || undefined,
  };

  const result = await createEmployeeLevel(request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create level." };
  }

  revalidatePath("/organization/departments");
  return { success: true };
}
