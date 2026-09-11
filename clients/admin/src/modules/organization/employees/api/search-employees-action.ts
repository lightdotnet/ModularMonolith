"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { searchEmployees } from "@/modules/organization/employees/api/employees.api";
import type {
  EmployeeDto,
  EmployeeSearchParams,
} from "@/modules/organization/employees/types/employee";
import type { Paged } from "@/types/api";

export interface SearchEmployeesState {
  data: Paged<EmployeeDto> | null;
  error?: string;
}

export async function searchEmployeesAction(
  params: EmployeeSearchParams,
): Promise<SearchEmployeesState> {
  const session = await resolveSession();
  if (!session) {
    return {
      data: null,
      error: "Your session has expired. Please sign in again.",
    };
  }

  const result = await searchEmployees(params);

  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Failed to search employees." };
  }

  return { data: result.data };
}
