"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getOrgUnitManagers } from "@/modules/organization/departments/api/org-units.api";
import type { EmployeeDto } from "@/modules/organization/employees";

export interface GetOrgUnitManagersState {
  data: EmployeeDto[] | null;
  error?: string;
}

/** Lets the "View managers" dialog self-fetch a department/team's managers on demand. */
export async function getOrgUnitManagersAction(orgUnitId: string): Promise<GetOrgUnitManagersState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await getOrgUnitManagers(orgUnitId);

  if (!result.isSuccess) {
    return { data: null, error: result.message || "Failed to load managers." };
  }

  return { data: result.data };
}
