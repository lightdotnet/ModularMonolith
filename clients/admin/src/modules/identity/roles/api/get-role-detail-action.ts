"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getRoleById } from "@/modules/identity/roles/api/roles.api";
import type { RoleDto } from "@/modules/identity/roles/types/role";

export interface GetRoleDetailState {
  data: RoleDto | null;
  error?: string;
}

export async function getRoleDetailAction(
  id: string,
): Promise<GetRoleDetailState> {
  const session = await resolveSession();
  if (!session) {
    return {
      data: null,
      error: "Your session has expired. Please sign in again.",
    };
  }

  const result = await getRoleById(id);

  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Failed to load role." };
  }

  return { data: result.data };
}
