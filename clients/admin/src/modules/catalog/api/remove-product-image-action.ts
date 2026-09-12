"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { removeProductImage } from "@/modules/catalog/api/products.api";

export interface RemoveProductImageActionState {
  error?: string;
  success?: boolean;
}

export async function removeProductImageAction(
  id: string,
  url: string,
): Promise<RemoveProductImageActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await removeProductImage(id, url);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to remove image." };
  }

  revalidatePath("/catalog");
  return { success: true };
}
