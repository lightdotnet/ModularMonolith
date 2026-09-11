"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { moveOrgUnit } from "@/modules/organization/departments/api/org-units.api";

export interface MoveOrgUnitActionState {
  error?: string;
  success?: boolean;
}

export async function moveOrgUnitAction(
  id: string,
  newParentId: string | undefined,
): Promise<MoveOrgUnitActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await moveOrgUnit(id, { newParentId });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to move." };
  }

  revalidatePath("/organization/departments");
  return { success: true };
}
