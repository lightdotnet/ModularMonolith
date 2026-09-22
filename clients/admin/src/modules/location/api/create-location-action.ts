"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createLocation } from "@/modules/location/api/locations.api";
import type { CreateLocationRequest } from "@/modules/location/types/location";

export interface CreateLocationFormState {
  error?: string;
  success?: boolean;
}

export async function createLocationAction(
  _prevState: CreateLocationFormState,
  formData: FormData,
): Promise<CreateLocationFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const name = String(formData.get("name") ?? "").trim();
  const code = String(formData.get("code") ?? "").trim();
  const locationTypeId = String(formData.get("locationTypeId") ?? "").trim();

  if (!name || !code || !locationTypeId) {
    return { error: "Name, code and location type are required." };
  }

  const request: CreateLocationRequest = {
    parentLocationId: String(formData.get("parentLocationId") ?? "") || undefined,
    locationTypeId,
    name,
    code,
  };

  const result = await createLocation(request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create location." };
  }

  revalidatePath("/location");
  return { success: true };
}
