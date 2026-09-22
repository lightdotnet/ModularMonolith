"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Eye, PackageCheck, Undo2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { NativeSelect } from "@/components/ui/native-select";
import {
  DataTable,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { LocalDateTime } from "@/components/shared/local-date-time";
import {
  GoodsReceiptStatus,
  GoodsReceiptStatusBadge,
  formatGoodsReceiptStatus,
} from "@/modules/purchasing/common";
import type { GoodsReceiptDto } from "@/modules/purchasing/goods-receipts/types/goods-receipt";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

interface GoodsReceiptsDataTableProps {
  locations: LocationTreeNodeDto[];
  suppliers: { value: string; label: string }[];
  status: string;
  supplierId: string;
  locationId: string;
  searchValue: string;
  records: GoodsReceiptDto[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  error?: DataTableErrorState;
  /** Offer the "Return items" row action on Posted receipts (needs returns.create and returns.view). */
  canReturn?: boolean;
}

const STATUS_OPTIONS = Object.values(GoodsReceiptStatus).map((value) => ({
  value,
  label: formatGoodsReceiptStatus(value),
}));

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

function formatMoney(amount: number | null | undefined): string {
  if (amount === null || amount === undefined) return "";
  return new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}

export function GoodsReceiptsDataTable({
  locations,
  suppliers,
  status,
  supplierId,
  locationId,
  searchValue,
  records,
  pageNumber,
  pageSize,
  totalPages,
  totalRecords,
  error,
  canReturn,
}: GoodsReceiptsDataTableProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();

  const [pendingSearch, setPendingSearch] = useState(searchValue);
  const [lastSearch, setLastSearch] = useState(searchValue);
  if (searchValue !== lastSearch) {
    setLastSearch(searchValue);
    setPendingSearch(searchValue);
  }

  const [pendingStatus, setPendingStatus] = useState(status);
  const [lastStatus, setLastStatus] = useState(status);
  if (status !== lastStatus) {
    setLastStatus(status);
    setPendingStatus(status);
  }

  const [pendingSupplier, setPendingSupplier] = useState(supplierId);
  const [lastSupplier, setLastSupplier] = useState(supplierId);
  if (supplierId !== lastSupplier) {
    setLastSupplier(supplierId);
    setPendingSupplier(supplierId);
  }

  const [pendingLocation, setPendingLocation] = useState(locationId);
  const [lastLocation, setLastLocation] = useState(locationId);
  if (locationId !== lastLocation) {
    setLastLocation(locationId);
    setPendingLocation(locationId);
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

  const columns: DataTableColumn<GoodsReceiptDto>[] = [
    {
      id: "receiptNumber",
      header: "Receipt",
      cell: (receipt) => (
        <div className="flex flex-col gap-1">
          <span className="font-medium">{receipt.receiptNumber}</span>
          {/* Mobile-only: the other columns collapse below `sm`, so their info is folded in here. */}
          <div className="flex flex-col gap-1 sm:hidden">
            <span className="text-xs text-muted-foreground">{receipt.poNumber}</span>
            <span className="text-xs text-muted-foreground">{receipt.supplierName}</span>
            <span className="text-xs text-muted-foreground">{receipt.locationName}</span>
            <div>
              <GoodsReceiptStatusBadge status={receipt.status} />
            </div>
            <span className="text-xs text-muted-foreground">
              {`${formatQuantity(receipt.totalQuantity)} units · ${formatMoney(receipt.totalCostBase)}`}
            </span>
            <span className="text-xs text-muted-foreground">
              <LocalDateTime value={receipt.receivedAt} />
            </span>
          </div>
        </div>
      ),
    },
    {
      id: "purchaseOrder",
      header: "Purchase order",
      className: "hidden sm:table-cell",
      cell: (receipt) => receipt.poNumber,
    },
    {
      id: "supplier",
      header: "Supplier",
      className: "hidden sm:table-cell",
      cell: (receipt) => receipt.supplierName,
    },
    {
      id: "location",
      header: "Location",
      className: "hidden md:table-cell",
      cell: (receipt) => receipt.locationName,
    },
    {
      id: "deliveryNote",
      header: "Delivery note",
      className: "hidden lg:table-cell",
      cell: (receipt) => receipt.deliveryNoteRef,
    },
    {
      id: "status",
      header: "Status",
      className: "hidden sm:table-cell",
      cell: (receipt) => <GoodsReceiptStatusBadge status={receipt.status} />,
    },
    {
      id: "quantity",
      header: "Qty",
      className: "hidden sm:table-cell text-right",
      cell: (receipt) => formatQuantity(receipt.totalQuantity),
    },
    {
      id: "cost",
      header: "Total cost",
      className: "hidden sm:table-cell text-right",
      cell: (receipt) => formatMoney(receipt.totalCostBase),
    },
    {
      id: "receivedAt",
      header: "Received",
      className: "hidden md:table-cell",
      cell: (receipt) => <LocalDateTime value={receipt.receivedAt} />,
    },
    {
      id: "actions",
      header: "",
      hideable: false,
      cell: (receipt) => (
        <div className="flex justify-end gap-2">
          {canReturn && receipt.status === GoodsReceiptStatus.Posted && (
            <Button
              asChild
              aria-label={`Return items from goods receipt ${receipt.receiptNumber}`}
              size="icon"
              variant="outline"
            >
              <Link href={`/purchasing/returns/new?receiptId=${receipt.id}`}>
                <Undo2 />
              </Link>
            </Button>
          )}
          <Button asChild aria-label={`View goods receipt ${receipt.receiptNumber}`} size="icon" variant="outline">
            <Link href={`/purchasing/receipts/${receipt.id}`}>
              <Eye />
            </Link>
          </Button>
        </div>
      ),
    },
  ];

  const customSearch = (
    <>
      <Input
        className="w-full sm:w-52"
        aria-label="Search by receipt number or delivery note"
        placeholder="Receipt no. or delivery note..."
        value={pendingSearch}
        onChange={(event) => setPendingSearch(event.target.value)}
      />
      <NativeSelect
        className="w-full sm:w-40"
        aria-label="Filter by status"
        placeholder="All statuses"
        value={pendingStatus}
        onChange={setPendingStatus}
        options={STATUS_OPTIONS}
      />
      <NativeSelect
        className="w-full sm:w-48"
        aria-label="Filter by supplier"
        placeholder="Any supplier"
        value={pendingSupplier}
        onChange={setPendingSupplier}
        options={suppliers}
      />
      <NativeSelect
        className="w-full sm:w-48"
        aria-label="Filter by location"
        placeholder="Any location"
        value={pendingLocation}
        onChange={setPendingLocation}
        options={locations.map((location) => ({ value: String(location.id), label: location.name }))}
      />
    </>
  );

  return (
    <DataTable
      columns={columns}
      data={records}
      rowKey={(receipt) => String(receipt.id)}
      isLoading={isPending}
      customSearch={customSearch}
      onCustomSearch={() =>
        navigate({
          q: pendingSearch.trim() || undefined,
          status: pendingStatus || undefined,
          supplierId: pendingSupplier || undefined,
          locationId: pendingLocation || undefined,
          page: undefined,
        })
      }
      onRefresh={() => startTransition(() => router.refresh())}
      pageNumber={pageNumber}
      pageSize={pageSize}
      totalPages={totalPages}
      totalRecords={totalRecords}
      onPageChange={(page) => navigate({ page: String(page) })}
      error={error}
      emptyState={{
        icon: PackageCheck,
        title: "No goods receipts found",
        description: "Try adjusting your search, status, supplier or location filters.",
      }}
    />
  );
}
