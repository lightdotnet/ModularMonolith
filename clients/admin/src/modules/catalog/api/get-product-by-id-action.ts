"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getProductById } from "@/modules/catalog/api/products.api";
import type { ProductDto } from "@/modules/catalog/types/product";

export interface GetProductByIdState {
  data: ProductDto | null;
  error?: string;
}

/** Used by the product panel to fetch the authoritative current product (incl. its image list) when opened in edit mode. */
export async function getProductByIdAction(id: string): Promise<GetProductByIdState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await getProductById(id);

  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Failed to load product." };
  }

  return { data: result.data };
}
