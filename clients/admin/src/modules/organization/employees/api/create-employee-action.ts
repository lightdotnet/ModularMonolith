"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createEmployee } from "@/modules/organization/employees/api/employees.api";
import type { CreateEmployeeRequest } from "@/modules/organization/employees/types/employee";

export interface CreateEmployeeFormState {
  error?: string;
  success?: boolean;
}

export async function createEmployeeAction(
  _prevState: CreateEmployeeFormState,
  formData: FormData,
): Promise<CreateEmployeeFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const companyId = String(formData.get("companyId") ?? "").trim();
  const employeeCode = String(formData.get("employeeCode") ?? "").trim();
  const firstName = String(formData.get("firstName") ?? "").trim();
  const lastName = String(formData.get("lastName") ?? "").trim();

  if (!companyId || !employeeCode || !firstName || !lastName) {
    return { error: "Employee code, first name, and last name are required." };
  }

  const request: CreateEmployeeRequest = {
    companyId,
    employeeCode,
    firstName,
    lastName,
    dateOfBirth: String(formData.get("dateOfBirth") ?? "") || undefined,
    gender: String(formData.get("gender") ?? "") || undefined,
    nationalId: String(formData.get("nationalId") ?? "") || undefined,
    email: String(formData.get("email") ?? "") || undefined,
    phoneNumber: String(formData.get("phoneNumber") ?? "") || undefined,
    address: String(formData.get("address") ?? "") || undefined,
    hireDate: String(formData.get("hireDate") ?? "") || undefined,
  };

  const result = await createEmployee(request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create employee." };
  }

  revalidatePath("/organization/employees");
  return { success: true };
}
