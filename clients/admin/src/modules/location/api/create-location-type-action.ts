"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createLocationType } from "@/modules/location/api/location-types.api";
import type { CreateLocationTypeRequest } from "@/modules/location/types/location-type";

export interface CreateLocationTypeFormState {
  error?: string;
  success?: boolean;
}

export async function createLocationTypeAction(
  _prevState: CreateLocationTypeFormState,
  formData: FormData,
): Promise<CreateLocationTypeFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const canHaveChildren = formData.get("canHaveChildren") === "true";

  if (!id || !name) {
    return { error: "ID and name are required." };
  }

  const request: CreateLocationTypeRequest = {
    id,
    name,
    allowedParentTypeId: String(formData.get("allowedParentTypeId") ?? "") || undefined,
    canHaveChildren,
  };

  const result = await createLocationType(request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create location type." };
  }

  revalidatePath("/location");
  return { success: true };
}
