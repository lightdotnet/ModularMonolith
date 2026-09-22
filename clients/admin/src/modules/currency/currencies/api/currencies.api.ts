import "server-only";

import { currencyApi } from "@/lib/server/backend-api";
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import { CURRENCY_OPTIONS_LIMIT } from "@/modules/currency/common";
import type { ApiResponse, PagedResult, Result } from "@/types/api";
import type {
  CreateCurrencyRequest,
  CurrencyDto,
  CurrencySearchParams,
  UpdateCurrencyRequest,
} from "../types/currency";

const { requestJson } = currencyApi;

export function searchCurrencies(params: CurrencySearchParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<CurrencyDto>>("currency", {
      method: "GET",
      query: {
        searchValue: params.searchValue,
        isActive: params.isActive === undefined ? undefined : String(params.isActive),
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

export function getCurrencyByCode(code: string) {
  return guardCall(() => requestJson<Result<CurrencyDto>>(`currency/${encodeURIComponent(code)}`));
}

export function createCurrency(request: CreateCurrencyRequest) {
  return guardCall(() => requestJson<Result<string>>("currency", { method: "POST", body: request }));
}

export function updateCurrency(code: string, request: UpdateCurrencyRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`currency/${encodeURIComponent(code)}`, { method: "PUT", body: request }),
  );
}

export function activateCurrency(code: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`currency/${encodeURIComponent(code)}/activate`, { method: "PUT" }),
  );
}

export function deactivateCurrency(code: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`currency/${encodeURIComponent(code)}/deactivate`, { method: "PUT" }),
  );
}

export interface CurrencyOptionsResult {
  /** The base currency is listed first (backend order). */
  options: CurrencyDto[];
  /** More currencies exist than were loaded. */
  truncated: boolean;
  /** The lookup failed (for example the viewer lacks currency.currencies.view). */
  failed: boolean;
}

/** Loads a bounded currency list for pickers/filters; `activeOnly` requests only active currencies from the backend. */
export async function getCurrencyOptions(activeOnly = false): Promise<CurrencyOptionsResult> {
  const result = await searchCurrencies({
    isActive: activeOnly ? true : undefined,
    pageNumber: 1,
    pageSize: CURRENCY_OPTIONS_LIMIT,
  });

  if (!result.isSuccess || !result.data) return { options: [], truncated: false, failed: true };

  return {
    options: result.data.records,
    truncated: result.data.totalRecords > result.data.records.length,
    failed: false,
  };
}
