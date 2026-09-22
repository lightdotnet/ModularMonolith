"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { deleteLocation } from "@/modules/location/api/locations.api";

export interface DeleteLocationActionState {
  error?: string;
  success?: boolean;
}

export async function deleteLocationAction(id: string): Promise<DeleteLocationActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await deleteLocation(id);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to delete location." };
  }

  revalidatePath("/location");
  return { success: true };
}
