import { Badge } from "@/components/ui/badge";
import { OrderStatus } from "@/modules/orders/types/order";

const VARIANT_BY_STATUS: Record<OrderStatus, "default" | "outline" | "secondary"> = {
  [OrderStatus.Draft]: "outline",
  [OrderStatus.Placed]: "secondary",
  [OrderStatus.PartiallyPaid]: "secondary",
  [OrderStatus.Paid]: "default",
  [OrderStatus.Fulfilled]: "default",
  [OrderStatus.Cancelled]: "outline",
};

interface OrderStatusBadgeProps {
  status: OrderStatus;
}

export function OrderStatusBadge({ status }: OrderStatusBadgeProps) {
  return <Badge variant={VARIANT_BY_STATUS[status]}>{status}</Badge>;
}
