"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { activateProduct } from "@/modules/catalog/api/products.api";

export interface ActivateProductActionState {
  error?: string;
  success?: boolean;
}

export async function activateProductAction(id: string): Promise<ActivateProductActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await activateProduct(id);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to activate product." };
  }

  revalidatePath("/catalog");
  return { success: true };
}
