"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { addProductImage } from "@/modules/catalog/api/products.api";
import type { AddProductImageRequest } from "@/modules/catalog/types/product";

export interface AddProductImageActionState {
  error?: string;
  success?: boolean;
}

export async function addProductImageAction(
  id: string,
  request: AddProductImageRequest,
): Promise<AddProductImageActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await addProductImage(id, request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to add image." };
  }

  revalidatePath("/catalog");
  return { success: true };
}
