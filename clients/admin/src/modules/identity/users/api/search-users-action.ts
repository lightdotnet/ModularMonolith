"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { searchUsers } from "@/modules/identity/users/api/users.api";
import type { SearchUsersParams, UserDto } from "@/modules/identity/users/types/user";
import type { Paged } from "@/types/api";

export interface SearchUsersState {
  data: Paged<UserDto> | null;
  error?: string;
}

export async function searchUsersAction(
  params: SearchUsersParams,
): Promise<SearchUsersState> {
  const session = await resolveSession();
  if (!session) {
    return {
      data: null,
      error: "Your session has expired. Please sign in again.",
    };
  }

  const result = await searchUsers(params);

  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Failed to search users." };
  }

  return { data: result.data };
}
