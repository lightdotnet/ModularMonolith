"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { updateLocation } from "@/modules/location/api/locations.api";
import { LocationStatus } from "@/modules/location/types/location";

export interface UpdateLocationFormState {
  error?: string;
  success?: boolean;
}

export async function updateLocationAction(
  _prevState: UpdateLocationFormState,
  formData: FormData,
): Promise<UpdateLocationFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const code = String(formData.get("code") ?? "").trim();
  const status = String(formData.get("status") ?? LocationStatus.Active) as LocationStatus;

  if (!id || !name || !code) {
    return { error: "Name and code are required." };
  }

  const result = await updateLocation(id, { name, code, status });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update location." };
  }

  revalidatePath("/location");
  return { success: true };
}
