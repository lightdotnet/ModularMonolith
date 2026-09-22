"use client";

import { useState } from "react";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { MAX_AMOUNT, MAX_QUANTITY, isValidAmountText, isValidQuantity } from "@/modules/purchasing/common";
import { addPurchaseOrderLineAction } from "@/modules/purchasing/purchase-orders/api/add-purchase-order-line-action";
import { ProductSelect } from "@/modules/purchasing/purchase-orders/components/product-select";
import type { ProductDto } from "@/modules/catalog/types/product";

interface AddPurchaseOrderLineFormProps {
  purchaseOrderId: string;
  refresh: () => void;
}

/** Product picker + quantity + unit cost; stacks vertically on mobile with a full-width Add button. */
export function AddPurchaseOrderLineForm({ purchaseOrderId, refresh }: AddPurchaseOrderLineFormProps) {
  const [adding, runAdd] = useGuardedAction();
  const [product, setProduct] = useState<ProductDto | null>(null);
  const [quantity, setQuantity] = useState("1");
  const [unitCost, setUnitCost] = useState("");

  const validQuantity = isValidQuantity(Number(quantity));
  const validCost = isValidAmountText(unitCost);

  function handleAdd() {
    if (!product || !validQuantity || !validCost) return;

    runAdd(
      () => addPurchaseOrderLineAction(purchaseOrderId, product.id, Number(quantity), Number(unitCost)),
      `"${product.name}" added.`,
      () => {
        setProduct(null);
        setQuantity("1");
        setUnitCost("");
        refresh();
      },
    );
  }

  return (
    <div className="flex flex-col gap-3 rounded-lg border border-dashed border-border p-3 sm:flex-row sm:items-end">
      <div className="flex flex-1 flex-col gap-1">
        <Label className="text-xs text-muted-foreground" htmlFor="new-line-product">
          Product
        </Label>
        <ProductSelect id="new-line-product" value={product?.id ?? ""} onValueChange={setProduct} />
      </div>
      <div className="grid grid-cols-2 gap-3 sm:flex sm:items-end">
        <div className="flex flex-col gap-1 sm:w-24">
          <Label className="text-xs text-muted-foreground" htmlFor="new-line-quantity">
            Quantity
          </Label>
          <Input
            id="new-line-quantity"
            type="number"
            min="1"
            step="1"
            max={MAX_QUANTITY}
            value={quantity}
            onChange={(event) => setQuantity(event.target.value)}
          />
        </div>
        <div className="flex flex-col gap-1 sm:w-32">
          <Label className="text-xs text-muted-foreground" htmlFor="new-line-cost">
            Unit cost
          </Label>
          <Input
            id="new-line-cost"
            type="number"
            min="0"
            step="any"
            max={MAX_AMOUNT}
            value={unitCost}
            onChange={(event) => setUnitCost(event.target.value)}
          />
        </div>
        <Button
          type="button"
          className="col-span-2 w-full sm:w-auto"
          disabled={!product || !validQuantity || !validCost}
          loading={adding}
          onClick={handleAdd}
        >
          <Plus />
          Add
        </Button>
      </div>
    </div>
  );
}
