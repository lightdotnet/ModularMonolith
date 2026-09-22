"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getCategoryTree } from "@/modules/catalog/api/categories.api";
import type { CategoryTreeNodeDto } from "@/modules/catalog/types/category";

export interface GetCategoryTreeState {
  data: CategoryTreeNodeDto[] | null;
  error?: string;
}

/** Used by the move-category dialog's parent picker to re-fetch the tree on demand. */
export async function getCategoryTreeAction(): Promise<GetCategoryTreeState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await getCategoryTree();

  if (!result.isSuccess) {
    return { data: null, error: result.message || "Failed to load categories." };
  }

  return { data: result.data };
}
