"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { deleteLocationType } from "@/modules/location/api/location-types.api";

export interface DeleteLocationTypeActionState {
  error?: string;
  success?: boolean;
}

export async function deleteLocationTypeAction(id: string): Promise<DeleteLocationTypeActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await deleteLocationType(id);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to delete location type." };
  }

  revalidatePath("/location");
  return { success: true };
}
