"use client";

import { useState } from "react";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { addTransferLineAction } from "@/modules/transfers/api/add-transfer-line-action";
import { ProductSelect } from "@/modules/transfers/components/product-select";
import { MAX_TRANSFER_LINE_QUANTITY } from "@/modules/transfers/types/transfer";
import type { ProductDto } from "@/modules/catalog/types/product";

interface AddTransferLineFormProps {
  transferId: string;
  refresh: () => void;
}

export function AddTransferLineForm({ transferId, refresh }: AddTransferLineFormProps) {
  const [adding, runAdd] = useGuardedAction();
  const [product, setProduct] = useState<ProductDto | null>(null);
  const [quantity, setQuantity] = useState("1");

  const parsedQuantity = Number(quantity);
  const validQuantity =
    Number.isInteger(parsedQuantity) && parsedQuantity >= 1 && parsedQuantity <= MAX_TRANSFER_LINE_QUANTITY;

  function handleAdd() {
    if (!product || !validQuantity) return;

    runAdd(
      () => addTransferLineAction(transferId, product.id, parsedQuantity),
      `"${product.name}" added.`,
      () => {
        setProduct(null);
        setQuantity("1");
        refresh();
      },
    );
  }

  return (
    <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
      <div className="flex-1">
        <ProductSelect value={product?.id ?? ""} onValueChange={setProduct} />
      </div>
      <div className="flex items-end gap-2">
        <div className="w-24">
          <Input
            aria-label="Quantity"
            type="number"
            min="1"
            step="1"
            max={MAX_TRANSFER_LINE_QUANTITY}
            value={quantity}
            onChange={(event) => setQuantity(event.target.value)}
          />
        </div>
        <Button type="button" disabled={!product || !validQuantity} loading={adding} onClick={handleAdd}>
          <Plus />
          Add
        </Button>
      </div>
    </div>
  );
}
