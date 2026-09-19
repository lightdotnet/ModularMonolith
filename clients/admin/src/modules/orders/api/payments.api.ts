import "server-only";

import { ordersApi } from "@/lib/server/backend-api";
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, Result } from "@/types/api";
import type { PaymentDto, RecordPaymentRequest, VoidPaymentRequest } from "../types/order";

const { requestJson } = ordersApi;

export function getPayments(orderId: string) {
  return guardCall(() => requestJson<Result<PaymentDto[]>>(`payment/order/${orderId}`));
}

export function recordPayment(orderId: string, request: RecordPaymentRequest) {
  return guardCall(() =>
    requestJson<Result<string>>(`payment/order/${orderId}`, { method: "POST", body: request }),
  );
}

export function voidPayment(paymentId: string, request: VoidPaymentRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`payment/${paymentId}/void`, { method: "PUT", body: request }),
  );
}
