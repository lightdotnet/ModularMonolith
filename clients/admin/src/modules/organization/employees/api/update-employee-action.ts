"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { getEmployeeById, updateEmployee } from "@/modules/organization/employees/api/employees.api";
import { EmploymentStatus, type EmployeeDto } from "@/modules/organization/employees/types/employee";

export interface UpdateEmployeeFormState {
  error?: string;
  success?: boolean;
}

export async function updateEmployeeAction(
  _prevState: UpdateEmployeeFormState,
  formData: FormData,
): Promise<UpdateEmployeeFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  const employeeCode = String(formData.get("employeeCode") ?? "").trim();
  const firstName = String(formData.get("firstName") ?? "").trim();
  const lastName = String(formData.get("lastName") ?? "").trim();

  if (!id || !employeeCode || !firstName || !lastName) {
    return { error: "Employee code, first name, and last name are required." };
  }

  // userId/memberships are managed through dedicated actions, not this form —
  // load the current record so we submit them back unchanged.
  const current = await getEmployeeById(id);
  if (!current.isSuccess || !current.data) {
    return { error: current.message || "Failed to load the current employee." };
  }

  const employee: EmployeeDto = {
    ...current.data,
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
    terminationDate: String(formData.get("terminationDate") ?? "") || undefined,
    employmentStatus: (String(
      formData.get("employmentStatus") ?? EmploymentStatus.Active,
    ) as EmploymentStatus),
  };

  const result = await updateEmployee(employee);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update employee." };
  }

  revalidatePath("/organization/employees");
  return { success: true };
}
