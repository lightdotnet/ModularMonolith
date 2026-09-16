"use client";

import { useState } from "react";
import { Plus, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { NativeSelect } from "@/components/ui/native-select";
import { NumberInput } from "@/components/ui/number-input";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { addOrderFeeAction } from "@/modules/orders/api/add-order-fee-action";
import { removeOrderFeeAction } from "@/modules/orders/api/remove-order-fee-action";
import { OrderFeeType, type OrderDto } from "@/modules/orders/types/order";

/** Project-wide display convention: numbers group by thousands (#0,000), decimals shown only when present (up to 2). */
function formatNumber(value: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 2 }).format(value);
}

const TYPE_OPTIONS = [
  { value: OrderFeeType.Shipping, label: "Shipping" },
  { value: OrderFeeType.Other, label: "Other" },
];

interface OrderFeeListProps {
  order: OrderDto;
  readOnly: boolean;
  refresh: () => void;
}

export function OrderFeeList({ order, readOnly, refresh }: OrderFeeListProps) {
  const [adding, runAdd] = useGuardedAction();
  const [removing, runRemove] = useGuardedAction();
  const [name, setName] = useState("");
  const [amount, setAmount] = useState("");
  const [type, setType] = useState<OrderFeeType>(OrderFeeType.Shipping);

  function handleAdd() {
    const parsedAmount = Number(amount);
    if (!name.trim() || !amount || Number.isNaN(parsedAmount)) return;

    runAdd(
      () => addOrderFeeAction(order.id, { name: name.trim(), amount: parsedAmount, type }),
      "Fee added.",
      () => {
        setName("");
        setAmount("");
        refresh();
      },
    );
  }

  function handleRemove(feeId: string, feeName: string) {
    runRemove(() => removeOrderFeeAction(order.id, feeId), `"${feeName}" removed.`, refresh);
  }

  return (
    <div className="flex flex-col gap-2">
      <span className="text-sm font-medium">Fees</span>

      {order.fees.length === 0 ? (
        <p className="text-sm text-muted-foreground">No fees yet.</p>
      ) : (
        <div className="flex flex-col gap-1">
          {order.fees.map((fee) => (
            <div key={fee.id} className="flex items-center gap-2 rounded-md border border-border px-2.5 py-1.5">
              <span className="flex-1 truncate text-sm">{fee.name}</span>
              <span className="text-xs text-muted-foreground">{fee.type}</span>
              <span className="text-sm">{formatNumber(fee.amount)}</span>
              {!readOnly && (
                <Button
                  aria-label="Remove fee"
                  disabled={removing}
                  onClick={() => handleRemove(fee.id, fee.name)}
                  size="icon-xs"
                  type="button"
                  variant="outline"
                >
                  <Trash2 />
                </Button>
              )}
            </div>
          ))}
        </div>
      )}

      {!readOnly && (
        <div className="flex items-end gap-2">
          <div className="flex-1">
            <Input
              aria-label="Fee name"
              placeholder="Fee name"
              value={name}
              onChange={(event) => setName(event.target.value)}
            />
          </div>
          <div className="w-28">
            <NumberInput aria-label="Fee amount" value={amount} onValueChange={setAmount} />
          </div>
          <div className="w-32">
            <NativeSelect aria-label="Fee type" value={type} onChange={setType} options={TYPE_OPTIONS} />
          </div>
          <Button type="button" loading={adding} disabled={!name.trim() || !amount} onClick={handleAdd}>
            <Plus />
            Add
          </Button>
        </div>
      )}
    </div>
  );
}
