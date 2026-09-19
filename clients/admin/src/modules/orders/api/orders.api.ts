import "server-only";

import { ordersApi } from "@/lib/server/backend-api";
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, PagedResult, Result } from "@/types/api";
import type {
  AddOrderFeeRequest,
  AddOrderLineRequest,
  ApplyOrderDiscountRequest,
  CancelOrderRequest,
  CreateOrderRequest,
  OrderDto,
  OrderSearchParams,
  SetOrderLineSalePriceRequest,
  UpdateOrderLineQuantityRequest,
} from "../types/order";

const { requestJson } = ordersApi;

export function searchOrders(params: OrderSearchParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<OrderDto>>("order", {
      method: "GET",
      query: {
        locationId: params.locationId,
        status: params.status,
        searchValue: params.searchValue,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

export function getOrderById(id: string) {
  return guardCall(() => requestJson<Result<OrderDto>>(`order/${id}`));
}

export function createOrder(request: CreateOrderRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("order", { method: "POST", body: request }),
  );
}

export function addOrderLine(orderId: string, request: AddOrderLineRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`order/${orderId}/line`, {
      method: "POST",
      // ProductId is a bigint on the backend (`long`) with no [JsonNumberHandling] relaxation
      // anywhere in the Lightsoft stack (verified: only JsonStringEnumConverter is registered
      // globally) — sending it as a JSON string like every other opaque id in this app would
      // fail deserialization. This is the only place a bigint id is written into a request body
      // rather than a URL segment, so it needs an explicit numeric coercion here.
      body: { ...request, productId: Number(request.productId) },
    }),
  );
}

export function updateOrderLineQuantity(
  orderId: string,
  lineId: string,
  request: UpdateOrderLineQuantityRequest,
) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`order/${orderId}/line/${lineId}/quantity`, {
      method: "PUT",
      body: request,
    }),
  );
}

export function setOrderLineSalePrice(
  orderId: string,
  lineId: string,
  request: SetOrderLineSalePriceRequest,
) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`order/${orderId}/line/${lineId}/sale_price`, {
      method: "PUT",
      body: request,
    }),
  );
}

export function removeOrderLine(orderId: string, lineId: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`order/${orderId}/line/${lineId}`, { method: "DELETE" }),
  );
}

export function applyOrderDiscount(orderId: string, request: ApplyOrderDiscountRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`order/${orderId}/discount`, { method: "PUT", body: request }),
  );
}

export function removeOrderDiscount(orderId: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`order/${orderId}/discount`, { method: "DELETE" }),
  );
}

export function addOrderFee(orderId: string, request: AddOrderFeeRequest) {
  return guardCall(() =>
    requestJson<Result<string>>(`order/${orderId}/fee`, { method: "POST", body: request }),
  );
}

export function removeOrderFee(orderId: string, feeId: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`order/${orderId}/fee/${feeId}`, { method: "DELETE" }),
  );
}

export function placeOrder(orderId: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`order/${orderId}/place`, { method: "PUT" }),
  );
}

export function cancelOrder(orderId: string, request: CancelOrderRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`order/${orderId}/cancel`, { method: "PUT", body: request }),
  );
}

export function fulfillOrder(orderId: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`order/${orderId}/fulfill`, { method: "PUT" }),
  );
}
