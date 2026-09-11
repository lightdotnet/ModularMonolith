"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { getOrgUnitById, updateOrgUnit } from "@/modules/organization/departments/api/org-units.api";
import type { OrgUnitDto } from "@/modules/organization/departments/types/org-unit";

export interface UpdateOrgUnitFormState {
  error?: string;
  success?: boolean;
}

export async function updateOrgUnitAction(
  _prevState: UpdateOrgUnitFormState,
  formData: FormData,
): Promise<UpdateOrgUnitFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const code = String(formData.get("code") ?? "").trim();

  if (!id || !name || !code) {
    return { error: "Name and code are required." };
  }

  // companyId/parentId are not editable here (parent changes go through the
  // dedicated Move action) — load the current record so we submit its
  // existing values back unchanged.
  const current = await getOrgUnitById(id);
  if (!current.isSuccess || !current.data) {
    return { error: current.message || "Failed to load the current record." };
  }

  const orgUnit: OrgUnitDto = {
    ...current.data,
    name,
    code,
    managerEmployeeId: String(formData.get("managerEmployeeId") ?? "") || undefined,
    description: String(formData.get("description") ?? "") || undefined,
  };

  const result = await updateOrgUnit(orgUnit);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update." };
  }

  revalidatePath("/organization/departments");
  return { success: true };
}
