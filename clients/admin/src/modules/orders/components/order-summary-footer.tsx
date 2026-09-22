"use client";

import { Button } from "@/components/ui/button";
import { OrderStatus, type OrderDto } from "@/modules/orders/types/order";

const AMOUNT_FORMAT = new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** #,##0.00 followed by the ISO currency code. */
function formatAmount(amount: number, currency: string): string {
  return `${AMOUNT_FORMAT.format(amount)} ${currency}`;
}

interface OrderSummaryFooterProps {
  order: OrderDto;
  placing: boolean;
  onPlace: () => void;
  onCancel: () => void;
  fulfilling: boolean;
  onFulfill: () => void;
}

export function OrderSummaryFooter({
  order,
  placing,
  onPlace,
  onCancel,
  fulfilling,
  onFulfill,
}: OrderSummaryFooterProps) {
  const canPlace = order.status === OrderStatus.Draft && order.lines.length > 0;
  const canCancel = order.status !== OrderStatus.Cancelled && order.status !== OrderStatus.Fulfilled;
  const canFulfill = order.status === OrderStatus.Paid;
  const remaining = Math.max(order.total - order.amountPaid, 0);

  return (
    <div className="flex flex-col gap-2 border-t border-border p-4">
      <div className="grid grid-cols-[auto_auto] justify-end gap-x-3 gap-y-1 text-sm">
        <span className="text-right text-muted-foreground">Subtotal</span>
        <span className="min-w-32 text-right text-muted-foreground">{formatAmount(order.subtotal, order.currency)}</span>

        <span className="text-right text-muted-foreground">Discount</span>
        <span className="min-w-32 text-right text-muted-foreground">
          -{formatAmount(order.discountAmount, order.currency)}
        </span>

        <span className="text-right text-muted-foreground">Fees</span>
        <span className="min-w-32 text-right text-muted-foreground">{formatAmount(order.feesTotal, order.currency)}</span>

        <span className="text-right text-base font-semibold text-foreground">Total</span>
        <span className="min-w-32 text-right text-base font-semibold text-foreground">
          {formatAmount(order.total, order.currency)}
        </span>

        {order.status !== OrderStatus.Draft && (
          <>
            <span className="text-right text-muted-foreground">Amount paid</span>
            <span className="min-w-32 text-right text-muted-foreground">
              {formatAmount(order.amountPaid, order.currency)}
            </span>

            <span className="text-right text-muted-foreground">Remaining</span>
            <span className="min-w-32 text-right text-muted-foreground">{formatAmount(remaining, order.currency)}</span>
          </>
        )}
      </div>

      <div className="flex flex-wrap justify-end gap-2">
        <Button type="button" variant="outline" disabled={!canCancel} onClick={onCancel}>
          Cancel order
        </Button>
        <Button type="button" loading={placing} disabled={!canPlace} onClick={onPlace}>
          Place order
        </Button>
        <Button type="button" loading={fulfilling} disabled={!canFulfill} onClick={onFulfill}>
          Fulfill order
        </Button>
      </div>
    </div>
  );
}
