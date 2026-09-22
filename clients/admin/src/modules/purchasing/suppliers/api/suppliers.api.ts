import "server-only";

import { purchasingApi } from "@/lib/server/backend-api";
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import { SupplierStatus } from "@/modules/purchasing/common";
import type { ApiResponse, PagedResult, Result } from "@/types/api";
import type {
  CreateSupplierRequest,
  SupplierDto,
  SupplierSearchParams,
  UpdateSupplierRequest,
} from "../types/supplier";

const { requestJson } = purchasingApi;

export function searchSuppliers(params: SupplierSearchParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<SupplierDto>>("supplier", {
      method: "GET",
      query: {
        status: params.status,
        searchValue: params.searchValue,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

export function getSupplierById(id: string) {
  return guardCall(() => requestJson<Result<SupplierDto>>(`supplier/${id}`));
}

export function createSupplier(request: CreateSupplierRequest) {
  return guardCall(() => requestJson<Result<string>>("supplier", { method: "POST", body: request }));
}

export function updateSupplier(id: string, request: UpdateSupplierRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`supplier/${id}`, { method: "PUT", body: request }),
  );
}

export function activateSupplier(id: string) {
  return guardResponseCall(() => requestJson<ApiResponse>(`supplier/${id}/activate`, { method: "PUT" }));
}

export function deactivateSupplier(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`supplier/${id}/deactivate`, { method: "PUT" }),
  );
}

/** Upper bound on suppliers loaded into a picker or filter; pages show a notice when there are more. */
export const SUPPLIER_OPTIONS_LIMIT = 200;

export interface SupplierOptionsResult {
  options: { id: string; name: string; code: string; active: boolean }[];
  /** More suppliers exist than were loaded. */
  truncated: boolean;
  /** The lookup failed (for example the viewer lacks purchasing.suppliers.view). */
  failed: boolean;
}

/** Loads a bounded supplier list for pickers/filters; `activeOnly` requests only Active suppliers from the backend. */
export async function getSupplierOptions(activeOnly = false): Promise<SupplierOptionsResult> {
  const result = await searchSuppliers({
    status: activeOnly ? SupplierStatus.Active : undefined,
    pageNumber: 1,
    pageSize: SUPPLIER_OPTIONS_LIMIT,
  });

  if (!result.isSuccess || !result.data) return { options: [], truncated: false, failed: true };

  return {
    options: result.data.records.map((supplier) => ({
      id: String(supplier.id),
      name: supplier.name,
      code: supplier.code,
      active: supplier.status === SupplierStatus.Active,
    })),
    truncated: result.data.totalRecords > SUPPLIER_OPTIONS_LIMIT,
    failed: false,
  };
}
