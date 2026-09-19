/** Mirrors Orders.Contracts/Common/OrderTypeCategory.cs */
export enum OrderTypeCategory {
  Fee = "Fee",
  Payment = "Payment",
}

/** Mirrors Orders.Contracts/Common/OrderTypeStatus.cs */
export enum OrderTypeStatus {
  Active = "Active",
  Inactive = "Inactive",
}

/** Mirrors Orders.Contracts/OrderTypes/OrderTypeDto.cs. Identity is the composite (id, category). */
export interface OrderTypeDto {
  id: string;
  category: OrderTypeCategory;
  name: string;
  status: OrderTypeStatus;
}

/** Mirrors Orders.Contracts/OrderTypes/CreateOrderTypeRequest.cs */
export interface CreateOrderTypeRequest {
  /** User-defined ID — not auto-generated. Unique within its category. */
  id: string;
  category: OrderTypeCategory;
  name: string;
}

/** Mirrors Orders.Contracts/OrderTypes/UpdateOrderTypeRequest.cs (category/id are immutable). */
export interface UpdateOrderTypeRequest {
  name: string;
  status: OrderTypeStatus;
}
