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

/** Mirrors Orders.Contracts/Common/OrderFeeType.cs — serialized by name. */
export enum OrderFeeType {
  Shipping = "Shipping",
  Other = "Other",
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
}

/** Mirrors Orders.Contracts/Orders/OrderFeeDto.cs */
export interface OrderFeeDto {
  id: string;
  orderCode: string;
  name: string;
  amount: number;
  type: OrderFeeType;
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
  type: OrderFeeType;
}

/** Mirrors Orders.Contracts/Orders/CancelOrderRequest.cs */
export interface CancelOrderRequest {
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
