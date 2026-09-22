"use client";

import { useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { History, Plus } from "lucide-react";
import { Input } from "@/components/ui/input";
import { NativeSelect } from "@/components/ui/native-select";
import {
  DataTable,
  type DataTableAction,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { ProductSelect } from "@/modules/inventory/components/product-select";
import { RecordAdjustmentDialog } from "@/modules/inventory/components/record-adjustment-dialog";
import { StockMovementReasonBadge } from "@/modules/inventory/components/stock-movement-reason-badge";
import type { StockAdjustmentDto } from "@/modules/inventory/types/stock";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

interface StockAdjustmentsDataTableProps {
  locations: LocationTreeNodeDto[];
  productId: string;
  locationId: string;
  sourceOrderId: string;
  records: StockAdjustmentDto[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  error?: DataTableErrorState;
  canManage?: boolean;
  /** Reveals the unit cost / value change columns (values arrive null without the permission). */
  canViewCost?: boolean;
}

function formatQuantityDelta(quantityDelta: number): string {
  const formatted = new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(
    Math.abs(quantityDelta),
  );
  // Sign is semantically load-bearing here (adds vs. removes stock), so it's always shown explicitly.
  // CostRevaluation rows are quantity-neutral (delta 0) — shown as a plain "0", not "+0".
  if (quantityDelta === 0) return formatted;
  return quantityDelta > 0 ? `+${formatted}` : `-${formatted}`;
}

/** Money display is #0,000.00 — null/undefined renders blank; the sign of a value change is shown explicitly. */
function formatAmount(amount?: number | null, signed = false): string {
  if (amount === null || amount === undefined) return "";
  const formatted = new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(
    signed ? Math.abs(amount) : amount,
  );
  if (!signed) return formatted;
  return amount >= 0 ? `+${formatted}` : `-${formatted}`;
}

/** Uses `adj*`-prefixed query params so its pagination/filters don't collide with `StockLevelsDataTable`'s `level*` params on the same Inventory page URL. */
export function StockAdjustmentsDataTable({
  locations,
  productId,
  locationId,
  sourceOrderId,
  records,
  pageNumber,
  pageSize,
  totalPages,
  totalRecords,
  error,
  canManage,
  canViewCost,
}: StockAdjustmentsDataTableProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();

  const [dialogOpen, setDialogOpen] = useState(false);
  const [dialogKey, setDialogKey] = useState(0);

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

  const [pendingSourceOrderId, setPendingSourceOrderId] = useState(sourceOrderId);
  const [lastSourceOrderId, setLastSourceOrderId] = useState(sourceOrderId);
  if (sourceOrderId !== lastSourceOrderId) {
    setLastSourceOrderId(sourceOrderId);
    setPendingSourceOrderId(sourceOrderId);
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

  function openRecordDialog() {
    setDialogKey((key) => key + 1);
    setDialogOpen(true);
  }

  const actions: DataTableAction[] | undefined = canManage
    ? [
        {
          key: "record",
          label: "Record adjustment",
          icon: Plus,
          onClick: openRecordDialog,
        },
      ]
    : undefined;

  const columns: DataTableColumn<StockAdjustmentDto>[] = [
    {
      id: "product",
      header: "Product",
      hideable: false,
      cell: (adjustment) => (
        <div className="flex flex-col gap-1">
          <span className="font-medium">{adjustment.productId}</span>
          {/* Mobile-only: the other columns collapse (hidden below `sm`), so their
              info is folded into this card-style block instead of being lost. */}
          <div className="flex flex-col gap-1 sm:hidden">
            <span className="text-xs text-muted-foreground">{locationName(adjustment.locationId)}</span>
            <span className="text-sm">{formatQuantityDelta(adjustment.quantityDelta)}</span>
            <StockMovementReasonBadge reason={adjustment.reason} />
            {canViewCost && (
              <>
                <span className="text-xs text-muted-foreground">
                  Unit cost: {formatAmount(adjustment.unitCostBase)}
                </span>
                <span className="text-xs text-muted-foreground">
                  Value change: {formatAmount(adjustment.valueDeltaBase, true)}
                </span>
              </>
            )}
            <LocalDateTime value={adjustment.occurredAt} className="text-xs text-muted-foreground" />
          </div>
        </div>
      ),
    },
    {
      id: "location",
      header: "Location",
      className: "hidden sm:table-cell",
      cell: (adjustment) => locationName(adjustment.locationId),
    },
    {
      id: "quantityDelta",
      header: "Quantity change",
      className: "hidden sm:table-cell",
      cell: (adjustment) => formatQuantityDelta(adjustment.quantityDelta),
    },
    ...(canViewCost
      ? [
          {
            id: "unitCost",
            header: "Unit cost",
            className: "hidden sm:table-cell",
            cell: (adjustment: StockAdjustmentDto) => formatAmount(adjustment.unitCostBase),
          },
          {
            id: "valueDelta",
            header: "Value change",
            className: "hidden sm:table-cell",
            cell: (adjustment: StockAdjustmentDto) => formatAmount(adjustment.valueDeltaBase, true),
          },
        ]
      : []),
    {
      id: "reason",
      header: "Reason",
      className: "hidden sm:table-cell",
      cell: (adjustment) => <StockMovementReasonBadge reason={adjustment.reason} />,
    },
    {
      id: "occurredAt",
      header: "Occurred at",
      className: "hidden sm:table-cell",
      cell: (adjustment) => <LocalDateTime value={adjustment.occurredAt} />,
    },
    {
      id: "note",
      header: "Note",
      className: "hidden sm:table-cell",
      cell: (adjustment) => adjustment.note,
    },
    {
      id: "sourceOrderId",
      header: "Source order",
      className: "hidden sm:table-cell",
      // No deep link — Orders has no per-order detail route yet.
      cell: (adjustment) => adjustment.sourceOrderId,
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
      <Input
        className="w-full sm:w-40"
        aria-label="Filter by source order ID"
        placeholder="Source order ID"
        type="number"
        value={pendingSourceOrderId}
        onChange={(event) => setPendingSourceOrderId(event.target.value)}
      />
    </>
  );

  return (
    <>
      <DataTable
        columns={columns}
        data={records}
        rowKey={(adjustment) => adjustment.id}
        isLoading={isPending}
        actions={actions}
        customSearch={customSearch}
        onCustomSearch={() =>
          navigate({
            adjProductId: pendingProductId || undefined,
            adjLocationId: pendingLocationId || undefined,
            adjSourceOrderId: pendingSourceOrderId || undefined,
            adjPage: undefined,
          })
        }
        onRefresh={() => startTransition(() => router.refresh())}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalPages={totalPages}
        totalRecords={totalRecords}
        onPageChange={(page) => navigate({ adjPage: String(page) })}
        error={error}
        emptyState={{
          icon: History,
          title: "No stock adjustments found",
          description: "Try adjusting your product, location or source order filter.",
        }}
      />
      {canManage && (
        <RecordAdjustmentDialog
          key={`dialog-${dialogKey}`}
          open={dialogOpen}
          onOpenChange={setDialogOpen}
          locations={locations}
          onRecorded={() => router.refresh()}
        />
      )}
    </>
  );
}
