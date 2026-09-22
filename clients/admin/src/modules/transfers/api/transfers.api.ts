import "server-only";

import { transfersApi } from "@/lib/server/backend-api";
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import { HttpError } from "@/lib/server/http";
import type { ApiResponse, PagedResult, Result, ResultCode } from "@/types/api";
import type {
  AddStockTransferLineRequest,
  CancelStockTransferRequest,
  CloseStockTransferRequest,
  CreateStockTransferRequest,
  ReceiveStockTransferRequest,
  StockTransferDto,
  StockTransferSearchParams,
  UpdateStockTransferLineRequest,
  UpdateStockTransferRequest,
} from "../types/transfer";

const { requestJson } = transfersApi;

/**
 * `guardCall` collapses every non-400/401 HTTP failure into code "error", which loses the
 * distinction the callers below need (404 not found; 409 deterministic refusal vs. an ambiguous
 * network failure). A definitive 4xx status is mapped back to the backend's own result code here;
 * anything else (network error, timeout, 5xx, non-JSON) is rethrown so `guardCall` reports "error".
 */
const CODE_BY_STATUS: Record<number, ResultCode> = {
  400: "bad_request",
  401: "unauthorized",
  403: "forbidden",
  404: "not_found",
  409: "conflict",
};

async function withStatusCode<T>(call: () => Promise<Result<T>>): Promise<Result<T>> {
  try {
    return await call();
  } catch (error) {
    const code = error instanceof HttpError ? CODE_BY_STATUS[error.status] : undefined;
    if (!code) throw error;
    return {
      requestId: "",
      code,
      isSuccess: false,
      message: (error as HttpError).message,
      data: null as T,
    };
  }
}

export function searchStockTransfers(params: StockTransferSearchParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<StockTransferDto>>("stock_transfer", {
      method: "GET",
      query: {
        status: params.status,
        sourceLocationId: params.sourceLocationId,
        destinationLocationId: params.destinationLocationId,
        searchValue: params.searchValue,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

export function getStockTransferById(id: string) {
  return guardCall(() =>
    withStatusCode(() => requestJson<Result<StockTransferDto>>(`stock_transfer/${id}`)),
  );
}

export function createStockTransfer(request: CreateStockTransferRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("stock_transfer", { method: "POST", body: request }),
  );
}

export function addStockTransferLine(id: string, request: AddStockTransferLineRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`stock_transfer/${id}/line`, {
      method: "POST",
      // ProductId is a backend `long` and no JSON string-to-number relaxation is registered, so
      // the opaque string id must be coerced back to a number here (same as Orders/Inventory).
      body: { ...request, productId: Number(request.productId) },
    }),
  );
}

/** Returns the new receipt's id. */
export function receiveStockTransfer(id: string, request: ReceiveStockTransferRequest) {
  return guardCall(() =>
    withStatusCode(() =>
      requestJson<Result<string>>(`stock_transfer/${id}/receipt`, {
        method: "POST",
        // TransferLineId is a backend `long` — same numeric coercion as `productId` above.
        body: {
          clientRequestId: request.clientRequestId,
          lines: request.lines.map((line) => ({
            transferLineId: Number(line.transferLineId),
            quantity: line.quantity,
          })),
        },
      }),
    ),
  );
}

export function updateStockTransfer(id: string, request: UpdateStockTransferRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`stock_transfer/${id}`, { method: "PUT", body: request }),
  );
}

export function updateStockTransferLine(
  id: string,
  lineId: string,
  request: UpdateStockTransferLineRequest,
) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`stock_transfer/${id}/line/${lineId}`, {
      method: "PUT",
      body: request,
    }),
  );
}

export function dispatchStockTransfer(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`stock_transfer/${id}/dispatch`, { method: "PUT" }),
  );
}

export function closeStockTransfer(id: string, request: CloseStockTransferRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`stock_transfer/${id}/close`, { method: "PUT", body: request }),
  );
}

export function cancelStockTransfer(id: string, request: CancelStockTransferRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`stock_transfer/${id}/cancel`, { method: "PUT", body: request }),
  );
}

export function removeStockTransferLine(id: string, lineId: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`stock_transfer/${id}/line/${lineId}`, { method: "DELETE" }),
  );
}
