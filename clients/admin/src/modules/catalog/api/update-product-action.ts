"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { updateProduct } from "@/modules/catalog/api/products.api";
import { DEFAULT_CURRENCY, type UpdateProductRequest } from "@/modules/catalog/types/product";

export interface UpdateProductFormState {
  error?: string;
  success?: boolean;
}

export async function updateProductAction(
  _prevState: UpdateProductFormState,
  formData: FormData,
): Promise<UpdateProductFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  const categoryId = String(formData.get("categoryId") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const price = Number(formData.get("price") ?? 0);
  const vatRate = Number(formData.get("vatRate") ?? 0);

  if (!id || !categoryId || !name) {
    return { error: "Category and name are required." };
  }

  const request: UpdateProductRequest = {
    categoryId,
    name,
    description: String(formData.get("description") ?? "") || undefined,
    price,
    currency: DEFAULT_CURRENCY,
    vatRate,
  };

  const result = await updateProduct(id, request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update product." };
  }

  revalidatePath("/catalog");
  return { success: true };
}
