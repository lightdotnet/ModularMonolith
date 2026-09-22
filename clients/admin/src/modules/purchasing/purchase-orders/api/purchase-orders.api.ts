import "server-only";

import { purchasingApi } from "@/lib/server/backend-api";
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import { withStatusCode } from "@/modules/purchasing/common/server/with-status-code";
import type { ApiResponse, PagedResult, Result } from "@/types/api";
import type {
  AddPurchaseOrderLineRequest,
  CancelPurchaseOrderRequest,
  ClosePurchaseOrderRequest,
  CreatePurchaseOrderRequest,
  PurchaseOrderApproverDto,
  PurchaseOrderDto,
  PurchaseOrderSearchParams,
  ReceivePurchaseOrderRequest,
  SubmitPurchaseOrderRequest,
  UpdatePurchaseOrderLineRequest,
  UpdatePurchaseOrderRequest,
} from "../types/purchase-order";

const { requestJson } = purchasingApi;

export function searchPurchaseOrders(params: PurchaseOrderSearchParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<PurchaseOrderDto>>("purchase_order", {
      method: "GET",
      query: {
        status: params.status,
        supplierId: params.supplierId,
        locationId: params.locationId,
        searchValue: params.searchValue,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

export function getPurchaseOrderById(id: string) {
  return guardCall(() =>
    withStatusCode(() => requestJson<Result<PurchaseOrderDto>>(`purchase_order/${id}`)),
  );
}

/** Candidate approvers for the caller (requires purchasing.orders.submit). */
export function getPurchaseOrderApprovers() {
  return guardCall(() => requestJson<Result<PurchaseOrderApproverDto[]>>("purchase_order/approvers"));
}

export function createPurchaseOrder(request: CreatePurchaseOrderRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("purchase_order", {
      method: "POST",
      // SupplierId is a backend `long` and no JSON string-to-number relaxation is registered, so
      // the opaque string id is coerced back to a number here (same as Orders/Transfers).
      body: { ...request, supplierId: Number(request.supplierId) },
    }),
  );
}

export function addPurchaseOrderLine(id: string, request: AddPurchaseOrderLineRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`purchase_order/${id}/line`, {
      method: "POST",
      body: { ...request, productId: Number(request.productId) },
    }),
  );
}

/** Returns the new goods receipt's id. */
export function receivePurchaseOrder(id: string, request: ReceivePurchaseOrderRequest) {
  return guardCall(() =>
    withStatusCode(() =>
      requestJson<Result<string>>(`purchase_order/${id}/receipt`, {
        method: "POST",
        body: {
          deliveryNoteRef: request.deliveryNoteRef,
          receivedAt: request.receivedAt,
          lines: request.lines.map((line) => ({
            purchaseOrderLineId: Number(line.purchaseOrderLineId),
            quantity: line.quantity,
          })),
        },
      }),
    ),
  );
}

export function updatePurchaseOrder(id: string, request: UpdatePurchaseOrderRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`purchase_order/${id}`, {
      method: "PUT",
      body: { ...request, supplierId: Number(request.supplierId) },
    }),
  );
}

export function updatePurchaseOrderLine(
  id: string,
  lineId: string,
  request: UpdatePurchaseOrderLineRequest,
) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`purchase_order/${id}/line/${lineId}`, { method: "PUT", body: request }),
  );
}

export function submitPurchaseOrder(id: string, request: SubmitPurchaseOrderRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`purchase_order/${id}/submit`, { method: "PUT", body: request }),
  );
}

export function withdrawPurchaseOrder(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`purchase_order/${id}/withdraw`, { method: "PUT" }),
  );
}

export function closePurchaseOrder(id: string, request: ClosePurchaseOrderRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`purchase_order/${id}/close`, { method: "PUT", body: request }),
  );
}

export function cancelPurchaseOrder(id: string, request: CancelPurchaseOrderRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`purchase_order/${id}/cancel`, { method: "PUT", body: request }),
  );
}

export function removePurchaseOrderLine(id: string, lineId: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`purchase_order/${id}/line/${lineId}`, { method: "DELETE" }),
  );
}
