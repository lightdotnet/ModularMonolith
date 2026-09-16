"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { NativeSelect } from "@/components/ui/native-select";
import { NumberInput } from "@/components/ui/number-input";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { applyOrderDiscountAction } from "@/modules/orders/api/apply-order-discount-action";
import { removeOrderDiscountAction } from "@/modules/orders/api/remove-order-discount-action";
import { OrderDiscountKind, type OrderDto } from "@/modules/orders/types/order";

/** Project-wide display convention: numbers group by thousands (#0,000), decimals shown only when present (up to 2). */
function formatNumber(value: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 2 }).format(value);
}

const KIND_OPTIONS = [
  { value: OrderDiscountKind.FixedAmount, label: "Fixed amount" },
  { value: OrderDiscountKind.Percentage, label: "Percentage" },
];

interface OrderDiscountEditorProps {
  order: OrderDto;
  readOnly: boolean;
  refresh: () => void;
}

export function OrderDiscountEditor({ order, readOnly, refresh }: OrderDiscountEditorProps) {
  const [pending, run] = useGuardedAction();
  const [kind, setKind] = useState<OrderDiscountKind>(order.discountKind ?? OrderDiscountKind.FixedAmount);
  const [value, setValue] = useState(order.discountValue != null ? String(order.discountValue) : "");

  function handleApply() {
    const parsedValue = Number(value);
    if (!value || Number.isNaN(parsedValue)) return;

    run(() => applyOrderDiscountAction(order.id, { kind, value: parsedValue }), "Discount applied.", refresh);
  }

  function handleRemove() {
    run(() => removeOrderDiscountAction(order.id), "Discount removed.", refresh);
  }

  if (readOnly) {
    return (
      <div className="flex flex-col gap-1">
        <span className="text-sm font-medium">Discount</span>
        <span className="text-sm text-muted-foreground">
          {order.discountKind && order.discountValue != null
            ? `${order.discountKind}: ${formatNumber(order.discountValue)}`
            : ""}
        </span>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-2">
      <span className="text-sm font-medium">Discount</span>
      <div className="flex items-end gap-2">
        <div className="w-40">
          <NativeSelect aria-label="Discount kind" value={kind} onChange={setKind} options={KIND_OPTIONS} />
        </div>
        <div className="w-32">
          <NumberInput aria-label="Discount value" value={value} onValueChange={setValue} />
        </div>
        <Button type="button" loading={pending} disabled={!value} onClick={handleApply}>
          Apply
        </Button>
        {order.discountKind != null && (
          <Button type="button" variant="outline" disabled={pending} onClick={handleRemove}>
            Remove
          </Button>
        )}
      </div>
    </div>
  );
}
