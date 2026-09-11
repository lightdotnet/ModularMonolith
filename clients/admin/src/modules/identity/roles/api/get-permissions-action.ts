"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getPermissions } from "@/modules/identity/roles/api/roles.api";
import type { PermissionDefinition } from "@/modules/identity/roles/types/permission-definition";

export interface GetPermissionsState {
  data: PermissionDefinition[] | null;
  error?: string;
}

export async function getPermissionsAction(): Promise<GetPermissionsState> {
  const session = await resolveSession();
  if (!session) {
    return {
      data: null,
      error: "Your session has expired. Please sign in again.",
    };
  }

  const result = await getPermissions();

  if (!result.isSuccess || !result.data) {
    return {
      data: null,
      error: result.message || "Failed to load permissions.",
    };
  }

  return { data: result.data };
}
