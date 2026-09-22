import "server-only";

import { inventoryApi } from "@/lib/server/backend-api";
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, PagedResult } from "@/types/api";
import type {
  RecordStockMovementRequest,
  RevalueStockRequest,
  StockAdjustmentDto,
  StockAdjustmentSearchParams,
} from "../types/stock";

const { requestJson } = inventoryApi;

export function searchStockAdjustments(params: StockAdjustmentSearchParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<StockAdjustmentDto>>("stock_adjustment", {
      method: "GET",
      query: {
        productId: params.productId,
        locationId: params.locationId,
        sourceOrderId: params.sourceOrderId,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

export function recordStockMovement(request: RecordStockMovementRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>("stock_adjustment", {
      method: "POST",
      // ProductId is a bigint on the backend (`long`) with no [JsonNumberHandling] relaxation
      // anywhere in the Lightsoft stack — sending it as a JSON string like every other opaque id
      // in this app would fail deserialization. Same documented exception as Orders' addOrderLine
      // (see modules/orders/api/orders.api.ts).
      body: { ...request, productId: Number(request.productId) },
    }),
  );
}

export function revalueStock(request: RevalueStockRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>("stock_adjustment/revaluation", {
      method: "POST",
      // Same bigint `productId` exception as recordStockMovement above.
      body: { ...request, productId: Number(request.productId) },
    }),
  );
}
