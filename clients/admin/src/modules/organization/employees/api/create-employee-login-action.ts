"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createEmployeeLogin } from "@/modules/organization/employees/api/employees.api";
import type { CreateEmployeeLoginRequest } from "@/modules/organization/employees/types/employee";

export interface CreateEmployeeLoginFormState {
  error?: string;
  success?: boolean;
}

export async function createEmployeeLoginAction(
  employeeId: string,
  _prevState: CreateEmployeeLoginFormState,
  formData: FormData,
): Promise<CreateEmployeeLoginFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const userName = String(formData.get("userName") ?? "").trim();
  const password = String(formData.get("password") ?? "");

  if (!userName || !password) {
    return { error: "Username and password are required." };
  }

  const request: CreateEmployeeLoginRequest = {
    userName,
    password,
    email: String(formData.get("email") ?? "") || undefined,
    phoneNumber: String(formData.get("phoneNumber") ?? "") || undefined,
  };

  const result = await createEmployeeLogin(employeeId, request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create login." };
  }

  revalidatePath("/organization/employees");
  return { success: true };
}
