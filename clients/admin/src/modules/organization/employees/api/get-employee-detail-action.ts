"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getEmployeeById } from "@/modules/organization/employees/api/employees.api";
import type { EmployeeDto } from "@/modules/organization/employees/types/employee";

export interface GetEmployeeDetailState {
  data: EmployeeDto | null;
  error?: string;
}

export async function getEmployeeDetailAction(id: string): Promise<GetEmployeeDetailState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await getEmployeeById(id);

  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Failed to load employee." };
  }

  return { data: result.data };
}
