"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { deleteRole } from "@/modules/identity/roles/api/roles.api";

export interface DeleteRoleActionState {
  error?: string;
  success?: boolean;
}

export async function deleteRoleAction(
  id: string,
): Promise<DeleteRoleActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await deleteRole(id);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to delete role." };
  }

  revalidatePath("/identity/roles");
  return { success: true };
}
