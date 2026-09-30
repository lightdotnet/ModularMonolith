"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { deleteUser } from "@/modules/identity/users/api/users.api";

export interface DeleteUserActionState {
  error?: string;
  success?: boolean;
}

export async function deleteUserAction(id: string): Promise<DeleteUserActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await deleteUser(id);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to delete user." };
  }

  revalidatePath("/identity/users");
  return { success: true };
}
