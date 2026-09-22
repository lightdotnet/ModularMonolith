"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { upsertProduct } from "@/modules/catalog/api/products.api";
import {
  DEFAULT_CURRENCY,
  type ProductImageDto,
  type UpsertProductRequest,
} from "@/modules/catalog/types/product";

export interface UpsertProductFormState {
  error?: string;
  success?: boolean;
}

export async function upsertProductAction(
  _prevState: UpsertProductFormState,
  formData: FormData,
): Promise<UpsertProductFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim() || undefined;
  const categoryId = String(formData.get("categoryId") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const sku = String(formData.get("sku") ?? "").trim();
  const price = Number(formData.get("price") ?? 0);
  const vatRate = Number(formData.get("vatRate") ?? 0);
  const currency = (String(formData.get("currency") ?? "").trim() || DEFAULT_CURRENCY).toUpperCase();

  if (!categoryId || !name || (!id && !sku)) {
    return { error: "Category, name and SKU are required." };
  }

  // Whether the currency exists and is active is the backend's call; only the shape is checked here.
  if (!/^[A-Z]{3}$/.test(currency)) {
    return { error: "Currency must be a three-letter code." };
  }

  let images: ProductImageDto[] = [];
  try {
    const imagesJson = String(formData.get("imagesJson") ?? "");
    images = imagesJson ? JSON.parse(imagesJson) : [];
  } catch {
    images = [];
  }

  const request: UpsertProductRequest = {
    categoryId,
    name,
    description: String(formData.get("description") ?? "") || undefined,
    sku: sku || undefined,
    price,
    currency,
    vatRate,
    images,
  };

  const result = await upsertProduct(id, request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to save product." };
  }

  revalidatePath("/catalog");
  return { success: true };
}
