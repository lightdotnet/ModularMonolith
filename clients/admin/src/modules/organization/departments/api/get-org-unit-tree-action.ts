"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getOrgUnitTree } from "@/modules/organization/departments/api/org-units.api";
import type { OrgUnitTreeNodeDto } from "@/modules/organization/departments/types/org-unit";

export interface GetOrgUnitTreeState {
  data: OrgUnitTreeNodeDto[] | null;
  error?: string;
}

/** Used by other features (e.g. the Employees edit dialog's org-unit picker) to self-fetch the tree for a given company on demand. */
export async function getOrgUnitTreeAction(companyId: string): Promise<GetOrgUnitTreeState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await getOrgUnitTree(companyId);

  if (!result.isSuccess) {
    return { data: null, error: result.message || "Failed to load departments/teams." };
  }

  return { data: result.data };
}
