import "server-only";

import { purchasingApi } from "@/lib/server/backend-api";
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import { PurchaseReturnStatus } from "@/modules/purchasing/common";
import { withStatusCode } from "@/modules/purchasing/common/server/with-status-code";
import type { ApiResponse, PagedResult, Result } from "@/types/api";
import type {
  CancelPurchaseReturnRequest,
  CreatePurchaseReturnRequest,
  MarkPurchaseReturnCreditedRequest,
  PurchaseReturnDto,
  PurchaseReturnLineRequest,
  PurchaseReturnSearchParams,
  UpdatePurchaseReturnRequest,
} from "../types/purchase-return";

const { requestJson } = purchasingApi;

// GoodsReceiptLineId is a backend `long` and no JSON string-to-number relaxation is registered, so the
// opaque string id is coerced back to a number here (same as Orders/Transfers).
function toLineBody(lines: PurchaseReturnLineRequest[]) {
  return lines.map((line) => ({
    goodsReceiptLineId: Number(line.goodsReceiptLineId),
    quantity: line.quantity,
    reason: line.reason,
  }));
}

export function searchPurchaseReturns(params: PurchaseReturnSearchParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<PurchaseReturnDto>>("purchase_return", {
      method: "GET",
      query: {
        status: params.status,
        supplierId: params.supplierId,
        goodsReceiptId: params.goodsReceiptId,
        locationId: params.locationId,
        searchValue: params.searchValue,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

export function getPurchaseReturnById(id: string) {
  return guardCall(() =>
    withStatusCode(() => requestJson<Result<PurchaseReturnDto>>(`purchase_return/${id}`)),
  );
}

/** Creates a Draft return from a Posted receipt; returns the new return's id. */
export function createPurchaseReturn(request: CreatePurchaseReturnRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("purchase_return", {
      method: "POST",
      body: {
        goodsReceiptId: Number(request.goodsReceiptId),
        reason: request.reason,
        note: request.note,
        lines: toLineBody(request.lines),
      },
    }),
  );
}

export function updatePurchaseReturn(id: string, request: UpdatePurchaseReturnRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`purchase_return/${id}`, {
      method: "PUT",
      body: { reason: request.reason, note: request.note, lines: toLineBody(request.lines) },
    }),
  );
}

export function postPurchaseReturn(id: string) {
  return guardResponseCall(() => requestJson<ApiResponse>(`purchase_return/${id}/post`, { method: "PUT" }));
}

export function cancelPurchaseReturn(id: string, request: CancelPurchaseReturnRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`purchase_return/${id}/cancel`, { method: "PUT", body: request }),
  );
}

export function markPurchaseReturnCredited(id: string, request: MarkPurchaseReturnCreditedRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`purchase_return/${id}/credit`, { method: "PUT", body: request }),
  );
}

const CLAIM_PAGE_SIZE = 100;
const CLAIM_MAX_PAGES = 20;

/**
 * Sums, per goods-receipt line, the quantity already claimed by the non-cancelled returns of one receipt
 * (drafts included), optionally excluding the return being edited. Requires purchasing.returns.view;
 * `claimed` is null when the lookup failed so callers can fall back to the received quantity.
 */
export async function getClaimedReturnQuantities(
  goodsReceiptId: string,
  excludeReturnId?: string,
): Promise<{ claimed: Record<string, number> | null }> {
  const claimed: Record<string, number> = {};

  for (let pageNumber = 1; pageNumber <= CLAIM_MAX_PAGES; pageNumber += 1) {
    const result = await searchPurchaseReturns({ goodsReceiptId, pageNumber, pageSize: CLAIM_PAGE_SIZE });
    if (!result.isSuccess || !result.data) return { claimed: null };

    for (const item of result.data.records) {
      if (item.status === PurchaseReturnStatus.Cancelled) continue;
      if (excludeReturnId && String(item.id) === excludeReturnId) continue;
      for (const line of item.lines ?? []) {
        const key = String(line.goodsReceiptLineId);
        claimed[key] = (claimed[key] ?? 0) + line.quantity;
      }
    }

    if (pageNumber >= result.data.totalPages) return { claimed };
  }

  // More pages than we are willing to walk: the sum would be incomplete, so report it as unknown.
  return { claimed: null };
}
