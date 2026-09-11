"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createRole } from "@/modules/identity/roles/api/roles.api";
import type { CreateRoleRequest } from "@/modules/identity/roles/types/role";

export interface CreateRoleFormState {
  error?: string;
  success?: boolean;
}

export async function createRoleAction(
  _prevState: CreateRoleFormState,
  formData: FormData,
): Promise<CreateRoleFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const name = String(formData.get("name") ?? "").trim();

  if (!name) {
    return { error: "Name is required." };
  }

  const request: CreateRoleRequest = {
    name,
    description: String(formData.get("description") ?? "") || undefined,
  };

  const result = await createRole(request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create role." };
  }

  revalidatePath("/identity/roles");
  return { success: true };
}
