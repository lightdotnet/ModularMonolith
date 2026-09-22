"use client";

import { useEffect, useRef, useState } from "react";
import { getProductByIdAction } from "@/modules/catalog/api/get-product-by-id-action";
import { OrderLineRow } from "@/modules/orders/components/order-line-row";
import type { OrderDto } from "@/modules/orders/types/order";

interface OrderLineListProps {
  order: OrderDto;
  readOnly: boolean;
  refresh: () => void;
}

/**
 * `OrderLineDto` carries no image — Orders only snapshots name/sku/price/VAT at add-line time,
 * deliberately decoupled from Catalog afterward. Reuses the existing `getProductByIdAction`
 * (already used by the product panel) instead of adding a dedicated endpoint, fetching only
 * product ids not already in `requestedIds` so each distinct product is looked up at most once
 * per panel session, not on every render/refresh.
 */
export function OrderLineList({ order, readOnly, refresh }: OrderLineListProps) {
  const [imagesByProductId, setImagesByProductId] = useState<Record<string, string | null>>({});
  const requestedIds = useRef<Set<string>>(new Set());

  useEffect(() => {
    const missingIds = [...new Set(order.lines.map((line) => line.productId))].filter(
      (id) => !requestedIds.current.has(id),
    );
    if (missingIds.length === 0) return;
    missingIds.forEach((id) => requestedIds.current.add(id));

    let cancelled = false;
    (async () => {
      const entries = await Promise.all(
        missingIds.map(async (id) => {
          const result = await getProductByIdAction(id);
          return [id, result.data?.images[0]?.url ?? null] as const;
        }),
      );
      if (cancelled) return;
      setImagesByProductId((prev) => ({ ...prev, ...Object.fromEntries(entries) }));
    })();

    return () => {
      cancelled = true;
    };
  }, [order.lines]);

  if (order.lines.length === 0) {
    return <p className="text-sm text-muted-foreground">No lines yet.</p>;
  }

  return (
    <div className="flex flex-col gap-2">
      {order.lines.map((line) => (
        <OrderLineRow
          key={line.id}
          orderId={order.id}
          line={line}
          currency={order.currency}
          readOnly={readOnly}
          refresh={refresh}
          imageUrl={imagesByProductId[line.productId]}
        />
      ))}
    </div>
  );
}
