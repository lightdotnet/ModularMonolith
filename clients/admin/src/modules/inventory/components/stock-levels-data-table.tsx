"use client";

import { useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Boxes } from "lucide-react";
import { NativeSelect } from "@/components/ui/native-select";
import {
  DataTable,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { ProductSelect } from "@/modules/inventory/components/product-select";
import type { ProductStockTotalDto, StockLevelDto } from "@/modules/inventory/types/stock";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

interface StockLevelsDataTableProps {
  locations: LocationTreeNodeDto[];
  productId: string;
  locationId: string;
  records: StockLevelDto[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  error?: DataTableErrorState;
  /** All-locations total for the selected product — independent of the location filter below. Only present when a product filter is active. */
  productTotal?: ProductStockTotalDto;
}

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

/**
 * Read-only — Stock Levels has no mutating action of its own (quantities only
 * change via recorded adjustments), so this never renders a `DataTableAction`.
 * Uses `level*`-prefixed query params so its pagination/filters don't collide
 * with `StockAdjustmentsDataTable`'s `adj*` params on the same Inventory page URL.
 */
export function StockLevelsDataTable({
  locations,
  productId,
  locationId,
  records,
  pageNumber,
  pageSize,
  totalPages,
  totalRecords,
  error,
  productTotal,
}: StockLevelsDataTableProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();

  const [pendingProductId, setPendingProductId] = useState(productId);
  const [lastProductId, setLastProductId] = useState(productId);
  if (productId !== lastProductId) {
    setLastProductId(productId);
    setPendingProductId(productId);
  }

  const [pendingLocationId, setPendingLocationId] = useState(locationId);
  const [lastLocationId, setLastLocationId] = useState(locationId);
  if (locationId !== lastLocationId) {
    setLastLocationId(locationId);
    setPendingLocationId(locationId);
  }

  function navigate(nextParams: Record<string, string | undefined>) {
    const params = new URLSearchParams(searchParams.toString());
    for (const [key, value] of Object.entries(nextParams)) {
      if (value) params.set(key, value);
      else params.delete(key);
    }

    startTransition(() => {
      router.push(`${pathname}?${params.toString()}`);
    });
  }

  function locationName(id: string): string {
    return locations.find((location) => location.id === id)?.name ?? "";
  }

  const columns: DataTableColumn<StockLevelDto>[] = [
    {
      id: "product",
      header: "Product",
      hideable: false,
      cell: (level) => (
        <div className="flex flex-col gap-1">
          <span className="font-medium">{level.productId}</span>
          {/* Mobile-only: the other columns collapse (hidden below `sm`), so their
              info is folded into this card-style block instead of being lost. */}
          <div className="flex flex-col gap-1 sm:hidden">
            <span className="text-xs text-muted-foreground">{locationName(level.locationId)}</span>
            <span className="text-sm">{formatQuantity(level.quantityOnHand)}</span>
          </div>
        </div>
      ),
    },
    {
      id: "location",
      header: "Location",
      className: "hidden sm:table-cell",
      cell: (level) => locationName(level.locationId),
    },
    {
      id: "quantityOnHand",
      header: "Quantity on hand",
      className: "hidden sm:table-cell",
      cell: (level) => formatQuantity(level.quantityOnHand),
    },
  ];

  const customSearch = (
    <>
      <div className="w-full sm:w-64">
        <ProductSelect
          value={pendingProductId}
          onValueChange={(product) => setPendingProductId(product.id)}
          onClear={() => setPendingProductId("")}
          placeholder="All products"
        />
      </div>
      <NativeSelect
        className="w-full sm:w-56"
        aria-label="Filter by location"
        placeholder="All locations"
        value={pendingLocationId}
        onChange={setPendingLocationId}
        options={locations.map((location) => ({ value: location.id, label: location.name }))}
      />
    </>
  );

  return (
    <div className="flex flex-col gap-3">
      {productTotal && (
        <div className="flex items-center gap-2 rounded-2xl border border-border bg-muted/30 p-4">
          <Boxes className="size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
          <p className="text-sm text-foreground">
            Total on hand: <span className="font-semibold">{formatQuantity(productTotal.totalQuantityOnHand)}</span>{" "}
            across {productTotal.locationCount} location{productTotal.locationCount === 1 ? "" : "s"}
          </p>
        </div>
      )}
      <DataTable
        columns={columns}
        data={records}
        rowKey={(level) => level.id}
        isLoading={isPending}
        customSearch={customSearch}
        onCustomSearch={() =>
          navigate({
            levelProductId: pendingProductId || undefined,
            levelLocationId: pendingLocationId || undefined,
            levelPage: undefined,
          })
        }
        onRefresh={() => startTransition(() => router.refresh())}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalPages={totalPages}
        totalRecords={totalRecords}
        onPageChange={(page) => navigate({ levelPage: String(page) })}
        error={error}
        emptyState={{
          icon: Boxes,
          title: "No stock levels found",
          description: "Try adjusting your product or location filter.",
        }}
      />
    </div>
  );
}
