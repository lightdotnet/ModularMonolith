import { catalogApi } from "@/lib/server/backend-api";
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, Result } from "@/types/api";
import type {
  CategoryDto,
  CategoryTreeNodeDto,
  CreateCategoryRequest,
  MoveCategoryRequest,
  UpdateCategoryRequest,
} from "../types/category";

const { requestJson } = catalogApi;

export function getCategoryTree() {
  return guardCall(() => requestJson<Result<CategoryTreeNodeDto[]>>("category/tree"));
}

export function getCategoryById(id: string) {
  return guardCall(() => requestJson<Result<CategoryDto>>(`category/${id}`));
}

export function getCategoryChildren(id: string) {
  return guardCall(() => requestJson<Result<CategoryTreeNodeDto[]>>(`category/${id}/children`));
}

export function createCategory(request: CreateCategoryRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("category", { method: "POST", body: request }),
  );
}

export function updateCategory(id: string, request: UpdateCategoryRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`category/${id}`, { method: "PUT", body: request }),
  );
}

export function moveCategory(id: string, request: MoveCategoryRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`category/${id}/move`, { method: "PUT", body: request }),
  );
}

export function deleteCategory(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`category/${id}`, { method: "DELETE" }),
  );
}
