"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createProduct } from "@/modules/catalog/api/products.api";
import { DEFAULT_CURRENCY, type CreateProductRequest } from "@/modules/catalog/types/product";

export interface CreateProductFormState {
  error?: string;
  success?: boolean;
}

export async function createProductAction(
  _prevState: CreateProductFormState,
  formData: FormData,
): Promise<CreateProductFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const categoryId = String(formData.get("categoryId") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const sku = String(formData.get("sku") ?? "").trim();
  const price = Number(formData.get("price") ?? 0);
  const vatRate = Number(formData.get("vatRate") ?? 0);

  if (!categoryId || !name || !sku) {
    return { error: "Category, name and SKU are required." };
  }

  const request: CreateProductRequest = {
    categoryId,
    name,
    description: String(formData.get("description") ?? "") || undefined,
    sku,
    price,
    currency: DEFAULT_CURRENCY,
    vatRate,
  };

  const result = await createProduct(request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create product." };
  }

  revalidatePath("/catalog");
  return { success: true };
}
