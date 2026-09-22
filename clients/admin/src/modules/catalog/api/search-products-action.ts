"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { searchProducts } from "@/modules/catalog/api/products.api";
import { ProductStatus, type ProductDto } from "@/modules/catalog/types/product";
import type { Paged } from "@/types/api";

export interface SearchProductsState {
  data: Paged<ProductDto> | null;
  error?: string;
}

/**
 * Client-callable wrapper around `searchProducts`, used by Orders' product picker
 * (`modules/orders/components/product-select.tsx`) to debounce-search active products
 * by name/SKU. Deliberately NOT exported from Catalog's `index.ts` barrel — Orders
 * imports this one file directly, the same "feature owns a small picker, imports one
 * specific action file directly, bypassing the barrel" shape as `UserSelect` importing
 * `searchUsersAction` directly from `modules/identity/users/api/search-users-action.ts`.
 */
export async function searchProductsAction(searchValue: string): Promise<SearchProductsState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await searchProducts({
    searchValue,
    status: ProductStatus.Active,
    pageNumber: 1,
    pageSize: 10,
  });

  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Failed to search products." };
  }

  return { data: result.data };
}
