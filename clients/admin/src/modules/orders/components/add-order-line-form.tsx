"use client";

import { useState } from "react";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { addOrderLineAction } from "@/modules/orders/api/add-order-line-action";
import { ProductSelect } from "@/modules/orders/components/product-select";
import type { ProductDto } from "@/modules/catalog/types/product";

interface AddOrderLineFormProps {
  orderId: string;
  refresh: () => void;
}

export function AddOrderLineForm({ orderId, refresh }: AddOrderLineFormProps) {
  const [adding, runAdd] = useGuardedAction();
  const [product, setProduct] = useState<ProductDto | null>(null);
  const [quantity, setQuantity] = useState("1");

  function handleAdd() {
    if (!product) return;
    const parsedQuantity = Number(quantity) || 1;

    runAdd(
      () => addOrderLineAction(orderId, { productId: product.id, quantity: parsedQuantity }),
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
        <div className="w-20">
          <Input
            aria-label="Quantity"
            type="number"
            min="1"
            value={quantity}
            onChange={(event) => setQuantity(event.target.value)}
          />
        </div>
        <Button type="button" disabled={!product} loading={adding} onClick={handleAdd}>
          <Plus />
          Add
        </Button>
      </div>
    </div>
  );
}
