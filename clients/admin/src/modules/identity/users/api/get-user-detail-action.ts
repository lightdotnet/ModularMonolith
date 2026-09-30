"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getUserById } from "@/modules/identity/users/api/users.api";
import type { UserDto } from "@/modules/identity/users/types/user";

export interface GetUserDetailState {
  data: UserDto | null;
  error?: string;
}

export async function getUserDetailAction(id: string): Promise<GetUserDetailState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await getUserById(id);

  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Failed to load user." };
  }

  return { data: result.data };
}
