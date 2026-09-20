import "server-only";

import { inventoryApi } from "@/lib/server/backend-api";
import { guardCall } from "@/lib/server/call-guard";
import type { PagedResult, Result } from "@/types/api";
import type { ProductStockTotalDto, StockLevelDto, StockLevelSearchParams } from "../types/stock";

const { requestJson } = inventoryApi;

export function searchStockLevels(params: StockLevelSearchParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<StockLevelDto>>("stock_level", {
      method: "GET",
      query: {
        productId: params.productId,
        locationId: params.locationId,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

export function getProductStockTotal(productId: string) {
  return guardCall(() =>
    requestJson<Result<ProductStockTotalDto>>(`stock_level/total/${productId}`),
  );
}
