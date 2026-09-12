"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { moveLocation } from "@/modules/location/api/locations.api";

export interface MoveLocationActionState {
  error?: string;
  success?: boolean;
}

export async function moveLocationAction(
  id: string,
  newParentLocationId: string | undefined,
): Promise<MoveLocationActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await moveLocation(id, { newParentLocationId });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to move location." };
  }

  revalidatePath("/location");
  return { success: true };
}
