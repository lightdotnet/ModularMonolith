import { catalogApi } from "@/lib/server/backend-api";
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, PagedResult, Result } from "@/types/api";
import type {
  AddProductImageRequest,
  CreateProductRequest,
  ProductDto,
  ProductSearchParams,
  UpdateProductRequest,
} from "../types/product";

const { requestJson } = catalogApi;

export function searchProducts(params: ProductSearchParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<ProductDto>>("product", {
      method: "GET",
      query: {
        categoryId: params.categoryId,
        status: params.status,
        searchValue: params.searchValue,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

export function getProductById(id: string) {
  return guardCall(() => requestJson<Result<ProductDto>>(`product/${id}`));
}

export function createProduct(request: CreateProductRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("product", { method: "POST", body: request }),
  );
}

export function updateProduct(id: string, request: UpdateProductRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`product/${id}`, { method: "PUT", body: request }),
  );
}

export function activateProduct(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`product/${id}/activate`, { method: "PUT" }),
  );
}

export function deactivateProduct(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`product/${id}/deactivate`, { method: "PUT" }),
  );
}

export function addProductImage(id: string, request: AddProductImageRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`product/${id}/image`, { method: "POST", body: request }),
  );
}

export function removeProductImage(id: string, url: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`product/${id}/image`, {
      method: "DELETE",
      query: { url },
    }),
  );
}
