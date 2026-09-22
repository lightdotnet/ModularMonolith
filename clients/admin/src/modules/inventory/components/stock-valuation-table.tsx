"use client";

import { useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Coins } from "lucide-react";
import { NativeSelect } from "@/components/ui/native-select";
import {
  DataTable,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { ProductSelect } from "@/modules/inventory/components/product-select";
import type { StockValuationLineDto } from "@/modules/inventory/types/stock";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

interface StockValuationTableProps {
  locations: LocationTreeNodeDto[];
  productId: string;
  locationId: string;
  records: StockValuationLineDto[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  /** Totals over every line matching the filter, not just the current page. */
  grandTotalQuantity?: number;
  grandTotalValue?: number;
  error?: DataTableErrorState;
}

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

/** Money display is #0,000.00. */
function formatAmount(amount: number): string {
  return new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}

/** Read-only valuation lines with a grand-total summary; product/location filters and paging live in the URL. */
export function StockValuationTable({
  locations,
  productId,
  locationId,
  records,
  pageNumber,
  pageSize,
  totalPages,
  totalRecords,
  grandTotalQuantity,
  grandTotalValue,
  error,
}: StockValuationTableProps) {
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

  const columns: DataTableColumn<StockValuationLineDto>[] = [
    {
      id: "product",
      header: "Product",
      hideable: false,
      cell: (line) => (
        <div className="flex flex-col gap-1">
          <span className="font-medium">{line.productId}</span>
          {/* Mobile-only: the other columns collapse (hidden below `sm`), so their
              info is folded into this card-style block instead of being lost. */}
          <div className="flex flex-col gap-1 sm:hidden">
            <span className="text-xs text-muted-foreground">{locationName(line.locationId)}</span>
            <span className="text-sm">Quantity: {formatQuantity(line.quantityOnHand)}</span>
            <span className="text-xs text-muted-foreground">Avg cost: {formatAmount(line.averageCostBase)}</span>
            <span className="text-xs text-muted-foreground">Total value: {formatAmount(line.totalValueBase)}</span>
          </div>
        </div>
      ),
    },
    {
      id: "location",
      header: "Location",
      className: "hidden sm:table-cell",
      cell: (line) => locationName(line.locationId),
    },
    {
      id: "quantityOnHand",
      header: "Quantity on hand",
      className: "hidden sm:table-cell",
      cell: (line) => formatQuantity(line.quantityOnHand),
    },
    {
      id: "averageCost",
      header: "Average cost",
      className: "hidden sm:table-cell",
      cell: (line) => formatAmount(line.averageCostBase),
    },
    {
      id: "totalValue",
      header: "Total value",
      className: "hidden sm:table-cell",
      cell: (line) => formatAmount(line.totalValueBase),
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
      {/* Totals are only meaningful when the load succeeded — never show fabricated zeros on error. */}
      {!error && grandTotalQuantity !== undefined && grandTotalValue !== undefined && (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div className="flex flex-col gap-1 rounded-2xl border border-border bg-muted/30 p-4">
            <span className="text-xs text-muted-foreground">Total quantity</span>
            <span className="text-lg font-semibold">{formatQuantity(grandTotalQuantity)}</span>
          </div>
          <div className="flex flex-col gap-1 rounded-2xl border border-border bg-muted/30 p-4">
            <span className="text-xs text-muted-foreground">Total value</span>
            <span className="text-lg font-semibold">{formatAmount(grandTotalValue)}</span>
          </div>
        </div>
      )}
      <DataTable
        columns={columns}
        data={records}
        rowKey={(line) => `${line.productId}:${line.locationId}`}
        isLoading={isPending}
        customSearch={customSearch}
        onCustomSearch={() =>
          navigate({
            productId: pendingProductId || undefined,
            locationId: pendingLocationId || undefined,
            page: undefined,
          })
        }
        onRefresh={() => startTransition(() => router.refresh())}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalPages={totalPages}
        totalRecords={totalRecords}
        onPageChange={(nextPage) => navigate({ page: String(nextPage) })}
        error={error}
        emptyState={{
          icon: Coins,
          title: "No stock valuation lines found",
          description: "Try adjusting your product or location filter.",
        }}
      />
    </div>
  );
}
