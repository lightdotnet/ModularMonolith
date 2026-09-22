/** Mirrors Catalog.Contracts/Common/ProductStatus.cs — serialized by name. */
export enum ProductStatus {
  Active = "Active",
  Inactive = "Inactive",
}

/** Mirrors Catalog.Contracts/Products/ProductImageDto.cs */
export interface ProductImageDto {
  url: string;
  sortOrder?: number | null;
}

/**
 * Mirrors Catalog.Contracts/Products/ProductDto.cs — `price`/`currency`/`vatRate` flatten the
 * backend's `Money`/`VatPercentage` value objects to scalar fields.
 */
export interface ProductDto {
  id: string;
  categoryId: string;
  name: string;
  description?: string | null;
  sku: string;
  price: number;
  currency: string;
  vatRate: number;
  status: ProductStatus;
  images: ProductImageDto[];
}

/** Mirrors Catalog.Contracts/Products/UpsertProductRequest.cs */
export interface UpsertProductRequest {
  categoryId: string;
  name: string;
  description?: string;
  sku?: string;
  price: number;
  currency: string;
  vatRate: number;
  images: ProductImageDto[];
}

/** Mirrors Catalog.Contracts/Products/AddProductImageRequest.cs */
export interface AddProductImageRequest {
  url: string;
  sortOrder?: number;
}

export interface ProductSearchParams {
  categoryId?: string;
  status?: ProductStatus;
  searchValue?: string;
  pageNumber?: number;
  pageSize?: number;
}

/**
 * Mirrors Shared/Constants/CurrencyConstants.cs's `Default` (the seeded base currency) — Contracts can't be
 * referenced from the client. Used as the pre-filled product currency when the active-currency list is unavailable.
 */
export const DEFAULT_CURRENCY = "VND";
