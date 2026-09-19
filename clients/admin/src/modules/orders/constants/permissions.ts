/** Mirrors Orders.Contracts/Authorization/OrdersPermissions.cs (Orders.*). */
export const ORDERS_PERMISSIONS = {
  View: "orders.orders.view",
  Manage: "orders.orders.manage",
  Payments: {
    View: "orders.payments.view",
    Manage: "orders.payments.manage",
  },
} as const;

/** Mirrors Orders.Contracts/Authorization/OrdersPermissions.cs (OrderTypes.*). */
export const ORDERS_ORDER_TYPES_PERMISSIONS = {
  View: "orders.order_types.view",
  Manage: "orders.order_types.manage",
} as const;
