"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getAllRoles } from "@/modules/identity/roles/api/roles.api";
import type { RoleDto } from "@/modules/identity/roles/types/role";

export interface GetAllRolesState {
  data: RoleDto[] | null;
  error?: string;
}

export async function getAllRolesAction(): Promise<GetAllRolesState> {
  const session = await resolveSession();
  if (!session) {
    return {
      data: null,
      error: "Your session has expired. Please sign in again.",
    };
  }

  const result = await getAllRoles();

  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Failed to load roles." };
  }

  return { data: result.data };
}
