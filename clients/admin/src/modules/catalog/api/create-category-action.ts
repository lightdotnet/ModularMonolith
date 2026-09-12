"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createCategory } from "@/modules/catalog/api/categories.api";
import type { CreateCategoryRequest } from "@/modules/catalog/types/category";

export interface CreateCategoryFormState {
  error?: string;
  success?: boolean;
}

export async function createCategoryAction(
  _prevState: CreateCategoryFormState,
  formData: FormData,
): Promise<CreateCategoryFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const name = String(formData.get("name") ?? "").trim();

  if (!name) {
    return { error: "Name is required." };
  }

  const request: CreateCategoryRequest = {
    parentCategoryId: String(formData.get("parentCategoryId") ?? "") || undefined,
    name,
  };

  const result = await createCategory(request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create category." };
  }

  revalidatePath("/catalog");
  return { success: true };
}
