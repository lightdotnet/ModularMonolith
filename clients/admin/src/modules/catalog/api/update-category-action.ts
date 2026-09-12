"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { updateCategory } from "@/modules/catalog/api/categories.api";

export interface UpdateCategoryFormState {
  error?: string;
  success?: boolean;
}

export async function updateCategoryAction(
  _prevState: UpdateCategoryFormState,
  formData: FormData,
): Promise<UpdateCategoryFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();

  if (!id || !name) {
    return { error: "Name is required." };
  }

  const result = await updateCategory(id, { name });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update category." };
  }

  revalidatePath("/catalog");
  return { success: true };
}
