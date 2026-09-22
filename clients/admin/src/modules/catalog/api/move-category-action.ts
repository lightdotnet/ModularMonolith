"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { moveCategory } from "@/modules/catalog/api/categories.api";

export interface MoveCategoryActionState {
  error?: string;
  success?: boolean;
}

export async function moveCategoryAction(
  id: string,
  newParentCategoryId: string | undefined,
): Promise<MoveCategoryActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await moveCategory(id, { newParentCategoryId });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to move category." };
  }

  revalidatePath("/catalog");
  return { success: true };
}
