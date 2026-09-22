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
import type { OrderDto } from "@/modules/orders/types/order";
import { OrderTypeStatus, type OrderTypeDto } from "@/modules/orders/types/order-type";

/** Project-wide display convention: numbers group by thousands (#0,000), decimals shown only when present (up to 2). */
function formatNumber(value: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 2 }).format(value);
}

interface OrderFeeListProps {
  order: OrderDto;
  readOnly: boolean;
  refresh: () => void;
  feeTypes: OrderTypeDto[];
}

export function OrderFeeList({ order, readOnly, refresh, feeTypes }: OrderFeeListProps) {
  const [adding, runAdd] = useGuardedAction();
  const [removing, runRemove] = useGuardedAction();
  const [name, setName] = useState("");
  const [amount, setAmount] = useState("");

  const activeFeeTypes = feeTypes.filter((t) => t.status === OrderTypeStatus.Active);
  const [feeTypeId, setFeeTypeId] = useState(activeFeeTypes[0]?.id ?? "");

  function handleAdd() {
    const parsedAmount = Number(amount);
    if (!name.trim() || !amount || Number.isNaN(parsedAmount) || !feeTypeId) return;

    runAdd(
      () => addOrderFeeAction(order.id, { name: name.trim(), amount: parsedAmount, feeTypeId }),
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
              <span className="text-xs text-muted-foreground">{fee.feeTypeName}</span>
              <span className="text-sm">
                {formatNumber(fee.amount)} {order.currency}
              </span>
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
        <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
          <div className="w-32">
            <NativeSelect
              aria-label="Fee type"
              value={feeTypeId}
              onChange={setFeeTypeId}
              options={activeFeeTypes.map((t) => ({ value: t.id, label: t.name }))}
            />
          </div>
          <div className="flex-1">
            <Input
              aria-label="Fee name"
              placeholder="Fee name"
              value={name}
              onChange={(event) => setName(event.target.value)}
            />
          </div>
          <div>
            <NumberInput aria-label="Fee amount" autoWidth value={amount} onValueChange={setAmount} />
          </div>
          <Button
            type="button"
            loading={adding}
            disabled={!name.trim() || !amount || !feeTypeId}
            onClick={handleAdd}
          >
            <Plus />
            Add
          </Button>
        </div>
      )}
    </div>
  );
}
