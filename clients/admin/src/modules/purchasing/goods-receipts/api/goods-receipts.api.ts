import "server-only";

import { purchasingApi } from "@/lib/server/backend-api";
import { guardCall } from "@/lib/server/call-guard";
import { withStatusCode } from "@/modules/purchasing/common/server/with-status-code";
import type { PagedResult, Result } from "@/types/api";
import type { GoodsReceiptDto, GoodsReceiptSearchParams } from "../types/goods-receipt";

const { requestJson } = purchasingApi;

export function searchGoodsReceipts(params: GoodsReceiptSearchParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<GoodsReceiptDto>>("goods_receipt", {
      method: "GET",
      query: {
        status: params.status,
        purchaseOrderId: params.purchaseOrderId,
        supplierId: params.supplierId,
        locationId: params.locationId,
        searchValue: params.searchValue,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

export function getGoodsReceiptById(id: string) {
  return guardCall(() =>
    withStatusCode(() => requestJson<Result<GoodsReceiptDto>>(`goods_receipt/${id}`)),
  );
}
