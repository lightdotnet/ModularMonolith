"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { deleteCategory } from "@/modules/catalog/api/categories.api";

export interface DeleteCategoryActionState {
  error?: string;
  success?: boolean;
}

export async function deleteCategoryAction(id: string): Promise<DeleteCategoryActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await deleteCategory(id);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to delete category." };
  }

  revalidatePath("/catalog");
  return { success: true };
}
