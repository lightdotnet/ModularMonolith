"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { deactivateProduct } from "@/modules/catalog/api/products.api";

export interface DeactivateProductActionState {
  error?: string;
  success?: boolean;
}

export async function deactivateProductAction(id: string): Promise<DeactivateProductActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await deactivateProduct(id);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to deactivate product." };
  }

  revalidatePath("/catalog");
  return { success: true };
}
