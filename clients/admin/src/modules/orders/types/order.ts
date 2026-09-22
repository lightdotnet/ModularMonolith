/** Mirrors Orders.Contracts/Common/OrderStatus.cs — serialized by name. */
export enum OrderStatus {
  Draft = "Draft",
  Placed = "Placed",
  PartiallyPaid = "PartiallyPaid",
  Paid = "Paid",
  Fulfilled = "Fulfilled",
  Cancelled = "Cancelled",
}

/** Mirrors Orders.Contracts/Common/OrderDiscountKind.cs — serialized by name. */
export enum OrderDiscountKind {
  FixedAmount = "FixedAmount",
  Percentage = "Percentage",
}

/**
 * Mirrors Orders.Contracts/Orders/OrderLineDto.cs — `unitPrice`/`vatRate` flatten the domain's
 * `Money`/`VatPercentage` value objects to scalar fields, same convention as `ProductDto`.
 */
export interface OrderLineDto {
  id: string;
  orderCode: string;
  productId: string;
  productName: string;
  sku: string;
  unitPrice: number;
  vatRate: number;
  quantity: number;
  requestedSalePrice?: number | null;
  discountAmountPerUnit: number;
  discountPercentage: number;
  /**
   * Only populated when the catalog price was in a different currency than the order and was converted
   * at add time: the original catalog price/currency, the rate applied (1 catalog unit = rate order-currency
   * units) and when that rate took effect.
   */
  catalogUnitPrice?: number | null;
  catalogCurrency?: string | null;
  appliedRate?: number | null;
  rateEffectiveFrom?: string | null;
}

/** Mirrors Orders.Contracts/Orders/OrderFeeDto.cs — `feeTypeName` is a snapshot of the fee type's name taken when the fee was added, not a live lookup. */
export interface OrderFeeDto {
  id: string;
  orderCode: string;
  name: string;
  amount: number;
  feeTypeId: string;
  feeTypeName: string;
}

/**
 * Mirrors Orders.Contracts/Payments/PaymentDto.cs — `id`/`orderId` are backend `long`, typed
 * `string` here for consistency with every other opaque id in this app (e.g. `OrderLineDto.productId`).
 * `paymentTypeName` is a snapshot of the payment type's name taken when the payment was recorded,
 * not a live lookup.
 */
export interface PaymentDto {
  id: string;
  orderId: string;
  orderCode: string;
  amount: number;
  currency: string;
  paymentTypeId: string;
  paymentTypeName: string;
  paidAt: string;
  reference?: string | null;
  recordedByUserId: string;
  isVoided: boolean;
  voidedAt?: string | null;
  voidReason?: string | null;
}

/**
 * Mirrors Orders.Contracts/Orders/OrderDto.cs — `subtotal`/`discountAmount`/`feesTotal`/`total`
 * mirror the domain's own computed roll-ups, plain decimals same as `OrderLineDto`'s `unitPrice`.
 */
export interface OrderDto {
  id: string;
  locationId: string;
  memberId?: string | null;
  orderCode: string;
  externalReferenceCode?: string | null;
  status: OrderStatus;
  discountKind?: OrderDiscountKind | null;
  discountValue?: number | null;
  subtotal: number;
  discountAmount: number;
  feesTotal: number;
  total: number;
  amountPaid: number;
  currency: string;
  placedAt?: string | null;
  cancelledAt?: string | null;
  fulfilledAt?: string | null;
  cancelledReason?: string | null;
  lines: OrderLineDto[];
  fees: OrderFeeDto[];
}

/** Mirrors Orders.Contracts/Orders/CreateOrderRequest.cs */
export interface CreateOrderRequest {
  locationId: string;
  memberId?: string;
  orderCode?: string;
  externalReferenceCode?: string;
}

/** Mirrors Orders.Contracts/Orders/AddOrderLineRequest.cs */
export interface AddOrderLineRequest {
  productId: string;
  quantity: number;
  requestedSalePrice?: number;
}

/** Mirrors Orders.Contracts/Orders/UpdateOrderLineQuantityRequest.cs */
export interface UpdateOrderLineQuantityRequest {
  quantity: number;
}

/** Mirrors Orders.Contracts/Orders/SetOrderLineSalePriceRequest.cs */
export interface SetOrderLineSalePriceRequest {
  salePrice?: number | null;
}

/** Mirrors Orders.Contracts/Orders/ApplyOrderDiscountRequest.cs */
export interface ApplyOrderDiscountRequest {
  kind: OrderDiscountKind;
  value: number;
}

/** Mirrors Orders.Contracts/Orders/AddOrderFeeRequest.cs */
export interface AddOrderFeeRequest {
  name: string;
  amount: number;
  feeTypeId: string;
}

/** Mirrors Orders.Contracts/Orders/CancelOrderRequest.cs */
export interface CancelOrderRequest {
  reason: string;
}

/**
 * Mirrors Orders.Contracts/Payments/RecordPaymentRequest.cs — `currency` must equal the order's
 * currency (the base currency; the server hard-rejects any other value), so the client only ever
 * sends `order.currency` here rather than exposing a currency picker.
 */
export interface RecordPaymentRequest {
  amount: number;
  currency: string;
  paymentTypeId: string;
  paidAt: string;
  reference?: string;
}

/** Mirrors Orders.Contracts/Payments/VoidPaymentRequest.cs */
export interface VoidPaymentRequest {
  reason: string;
}

/** Mirrors Orders.Contracts/Orders/SearchOrderRequest.cs */
export interface OrderSearchParams {
  locationId?: string;
  status?: OrderStatus;
  searchValue?: string;
  pageNumber?: number;
  pageSize?: number;
}
