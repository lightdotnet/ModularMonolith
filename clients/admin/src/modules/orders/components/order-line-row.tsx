"use client";

import { useState } from "react";
import { Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { NumberInput } from "@/components/ui/number-input";
import { ProductImageThumbnail } from "@/modules/catalog/components/product-image-thumbnail";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { removeOrderLineAction } from "@/modules/orders/api/remove-order-line-action";
import { updateOrderLineQuantityAction } from "@/modules/orders/api/update-order-line-quantity-action";
import { updateOrderLineSalePriceAction } from "@/modules/orders/api/update-order-line-sale-price-action";
import type { OrderLineDto } from "@/modules/orders/types/order";

/** Project-wide display convention: numbers group by thousands (#0,000), decimals shown only when present (up to 2). */
function formatNumber(value: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 2 }).format(value);
}

/** Exchange rates keep up to 8 fraction digits (trailing zeros trimmed), unlike amounts. */
function formatRate(value: number): string {
  return new Intl.NumberFormat("en-US", { minimumFractionDigits: 0, maximumFractionDigits: 8 }).format(value);
}

interface OrderLineRowProps {
  orderId: string;
  line: OrderLineDto;
  /** The order currency (the base currency), shown next to the unit price. */
  currency: string;
  readOnly: boolean;
  refresh: () => void;
  /** `null` once resolved with no image, `undefined` while `OrderLineList` hasn't looked it up yet. */
  imageUrl?: string | null;
}

export function OrderLineRow({ orderId, line, currency, readOnly, refresh, imageUrl }: OrderLineRowProps) {
  const [, runQuantity] = useGuardedAction();
  const [, runSalePrice] = useGuardedAction();
  const [removing, runRemove] = useGuardedAction();

  const [quantity, setQuantity] = useState(String(line.quantity));
  const [salePrice, setSalePrice] = useState(
    line.requestedSalePrice != null ? String(line.requestedSalePrice) : "",
  );

  function commitQuantity() {
    const next = Number(quantity);
    if (!next || next <= 0 || next === line.quantity) {
      setQuantity(String(line.quantity));
      return;
    }
    runQuantity(() => updateOrderLineQuantityAction(orderId, line.id, next), undefined, refresh);
  }

  function commitSalePrice() {
    const trimmed = salePrice.trim();
    const next = trimmed === "" ? null : Number(trimmed);
    const current = line.requestedSalePrice ?? null;
    if (next === current || (next !== null && Number.isNaN(next))) {
      setSalePrice(current != null ? String(current) : "");
      return;
    }
    runSalePrice(() => updateOrderLineSalePriceAction(orderId, line.id, next), undefined, refresh);
  }

  function handleRemove() {
    runRemove(() => removeOrderLineAction(orderId, line.id), `"${line.productName}" removed.`, refresh);
  }

  // Present only when the catalog price was in another currency and was converted at add time.
  const converted =
    line.catalogUnitPrice != null && !!line.catalogCurrency && line.appliedRate != null;

  return (
    <div className="flex flex-col gap-2 rounded-lg border border-border p-3">
      <div className="flex gap-3">
        <ProductImageThumbnail url={imageUrl} />

        <div className="flex min-w-0 flex-1 items-start justify-between gap-3">
          {/* Left: name, sku, price — all left-aligned, stacked as one block. */}
          <div className="flex min-w-0 flex-col">
            <span className="truncate font-medium">{line.productName}</span>
            <span className="text-xs text-muted-foreground">{line.sku}</span>
            <span className="text-sm text-muted-foreground">
              {formatNumber(line.unitPrice)} {currency} ({line.vatRate}% VAT)
            </span>
            {converted && (
              <span className="text-xs text-muted-foreground">
                Catalog price {formatNumber(line.catalogUnitPrice as number)} {line.catalogCurrency} ×{" "}
                {formatRate(line.appliedRate as number)} = {formatNumber(line.unitPrice)} {currency}
              </span>
            )}
          </div>

          {/* Right: qty, then sale price + discount — right-aligned, stacked. */}
          <div className="flex flex-col items-end gap-2">
            <NumberInput
              aria-label="Quantity"
              autoWidth
              value={quantity}
              disabled={readOnly}
              onValueChange={setQuantity}
              onBlur={commitQuantity}
            />

            <div className="flex flex-col items-end gap-1">
              <div className="flex items-center gap-1.5">
                <span className="text-xs text-muted-foreground">Sale price</span>
                <NumberInput
                  aria-label="Sale price override"
                  autoWidth
                  className="rounded-none border-0 border-b border-input bg-transparent px-1 text-right focus-visible:ring-0"
                  placeholder={formatNumber(line.unitPrice)}
                  value={salePrice}
                  disabled={readOnly}
                  onValueChange={setSalePrice}
                  onBlur={commitSalePrice}
                />
              </div>
              <span className="text-xs text-muted-foreground">
                Discount: {formatNumber(line.discountAmountPerUnit)}
              </span>
            </div>
          </div>
        </div>
      </div>

      {!readOnly && (
        <div className="flex justify-center">
          <Button
            aria-label="Remove line"
            disabled={removing}
            onClick={handleRemove}
            size="icon-xs"
            type="button"
            variant="outline"
          >
            <Trash2 />
          </Button>
        </div>
      )}
    </div>
  );
}
