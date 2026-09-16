"use client";

import { Button } from "@/components/ui/button";
import { OrderStatus, type OrderDto } from "@/modules/orders/types/order";

function formatAmount(amount: number, currency: string): string {
  try {
    return new Intl.NumberFormat("en-US", { style: "currency", currency }).format(amount);
  } catch {
    return `${amount} ${currency}`;
  }
}

interface OrderSummaryFooterProps {
  order: OrderDto;
  placing: boolean;
  onPlace: () => void;
  onCancel: () => void;
}

export function OrderSummaryFooter({ order, placing, onPlace, onCancel }: OrderSummaryFooterProps) {
  const canPlace = order.status === OrderStatus.Draft && order.lines.length > 0;
  const canCancel = order.status !== OrderStatus.Cancelled && order.status !== OrderStatus.Fulfilled;

  return (
    <div className="flex flex-col gap-2 border-t border-border p-4">
      <div className="flex flex-col gap-1 text-sm">
        <div className="flex justify-between text-muted-foreground">
          <span>Subtotal</span>
          <span>{formatAmount(order.subtotal, order.currency)}</span>
        </div>
        <div className="flex justify-between text-muted-foreground">
          <span>Discount</span>
          <span>-{formatAmount(order.discountAmount, order.currency)}</span>
        </div>
        <div className="flex justify-between text-muted-foreground">
          <span>Fees</span>
          <span>{formatAmount(order.feesTotal, order.currency)}</span>
        </div>
        <div className="flex justify-between text-base font-semibold text-foreground">
          <span>Total</span>
          <span>{formatAmount(order.total, order.currency)}</span>
        </div>
      </div>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" disabled={!canCancel} onClick={onCancel}>
          Cancel order
        </Button>
        <Button type="button" loading={placing} disabled={!canPlace} onClick={onPlace}>
          Place order
        </Button>
      </div>
    </div>
  );
}
