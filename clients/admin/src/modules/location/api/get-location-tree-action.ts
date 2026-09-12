"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getLocationTree } from "@/modules/location/api/locations.api";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

export interface GetLocationTreeState {
  data: LocationTreeNodeDto[] | null;
  error?: string;
}

/** Used by the move-location dialog to re-fetch the tree on demand. */
export async function getLocationTreeAction(): Promise<GetLocationTreeState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await getLocationTree();

  if (!result.isSuccess) {
    return { data: null, error: result.message || "Failed to load locations." };
  }

  return { data: result.data };
}
