import "server-only";

import { ordersApi } from "@/lib/server/backend-api";
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, Result } from "@/types/api";
import type {
  CreateOrderTypeRequest,
  OrderTypeCategory,
  OrderTypeDto,
  UpdateOrderTypeRequest,
} from "../types/order-type";

const { requestJson } = ordersApi;

export function getOrderTypes(category?: OrderTypeCategory) {
  const path = category ? `order_type?category=${encodeURIComponent(category)}` : "order_type";
  return guardCall(() => requestJson<Result<OrderTypeDto[]>>(path));
}

export function getOrderTypeById(category: OrderTypeCategory, id: string) {
  return guardCall(() =>
    requestJson<Result<OrderTypeDto>>(`order_type/${category}/${encodeURIComponent(id)}`),
  );
}

export function createOrderType(request: CreateOrderTypeRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("order_type", { method: "POST", body: request }),
  );
}

export function updateOrderType(
  category: OrderTypeCategory,
  id: string,
  request: UpdateOrderTypeRequest,
) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`order_type/${category}/${encodeURIComponent(id)}`, {
      method: "PUT",
      body: request,
    }),
  );
}

export function deleteOrderType(category: OrderTypeCategory, id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`order_type/${category}/${encodeURIComponent(id)}`, {
      method: "DELETE",
    }),
  );
}
