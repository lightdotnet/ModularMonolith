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

/** Mirrors Catalog.Contracts/Products/CreateProductRequest.cs */
export interface CreateProductRequest {
  categoryId: string;
  name: string;
  description?: string;
  sku: string;
  price: number;
  currency: string;
  vatRate: number;
}

/** Mirrors Catalog.Contracts/Products/UpdateProductRequest.cs — omits `sku`, immutable post-create. */
export interface UpdateProductRequest {
  categoryId: string;
  name: string;
  description?: string;
  price: number;
  currency: string;
  vatRate: number;
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

/** Mirrors Shared/Constants/CurrencyConstants.cs's `Default` — Contracts can't be referenced from the client, and there is no multi-currency support yet. */
export const DEFAULT_CURRENCY = "VND";
