"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { updateLocationType } from "@/modules/location/api/location-types.api";
import { LocationTypeStatus } from "@/modules/location/types/location-type";

export interface UpdateLocationTypeFormState {
  error?: string;
  success?: boolean;
}

export async function updateLocationTypeAction(
  _prevState: UpdateLocationTypeFormState,
  formData: FormData,
): Promise<UpdateLocationTypeFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const canHaveChildren = formData.get("canHaveChildren") === "true";
  const status = String(formData.get("status") ?? LocationTypeStatus.Active) as LocationTypeStatus;

  if (!id || !name) {
    return { error: "Name is required." };
  }

  const result = await updateLocationType(id, {
    name,
    allowedParentTypeId: String(formData.get("allowedParentTypeId") ?? "") || undefined,
    canHaveChildren,
    status,
  });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update location type." };
  }

  revalidatePath("/location");
  return { success: true };
}
