"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Eye, Pencil, Plus, Undo2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { NativeSelect } from "@/components/ui/native-select";
import {
  DataTable,
  type DataTableAction,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { LocalDateTime } from "@/components/shared/local-date-time";
import {
  PurchaseReturnStatus,
  PurchaseReturnStatusBadge,
  formatPurchaseReturnReason,
  formatPurchaseReturnStatus,
} from "@/modules/purchasing/common";
import type { PurchaseReturnDto } from "@/modules/purchasing/purchase-returns/types/purchase-return";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

interface PurchaseReturnsDataTableProps {
  locations: LocationTreeNodeDto[];
  suppliers: { value: string; label: string }[];
  status: string;
  supplierId: string;
  locationId: string;
  searchValue: string;
  records: PurchaseReturnDto[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  error?: DataTableErrorState;
  canCreate?: boolean;
  /** Can open the receipts list to start a return (needs returns.create and receipts.view). */
  canStart?: boolean;
}

const STATUS_OPTIONS = Object.values(PurchaseReturnStatus).map((value) => ({
  value,
  label: formatPurchaseReturnStatus(value),
}));

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

function formatMoney(amount: number | null | undefined): string {
  if (amount === null || amount === undefined) return "";
  return new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}

export function PurchaseReturnsDataTable({
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
  canCreate,
  canStart,
}: PurchaseReturnsDataTableProps) {
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

  // A return is always started from a posted goods receipt, so "New return" leads to the receipts list.
  const actions: DataTableAction[] | undefined = canStart
    ? [
        {
          key: "create",
          label: "New return",
          icon: Plus,
          onClick: () => router.push("/purchasing/receipts?status=Posted"),
        },
      ]
    : undefined;

  const columns: DataTableColumn<PurchaseReturnDto>[] = [
    {
      id: "returnNumber",
      header: "Return",
      cell: (item) => (
        <div className="flex flex-col gap-1">
          <span className="font-medium">{item.returnNumber}</span>
          {/* Mobile-only: the other columns collapse below `sm`, so their info is folded in here. */}
          <div className="flex flex-col gap-1 sm:hidden">
            <span className="text-xs text-muted-foreground">{item.supplierName}</span>
            <span className="text-xs text-muted-foreground">{`${item.receiptNumber} · ${item.locationName}`}</span>
            <div>
              <PurchaseReturnStatusBadge status={item.status} />
            </div>
            <span className="text-xs text-muted-foreground">
              {`${formatPurchaseReturnReason(item.reason)} · ${formatQuantity(item.totalQuantity)} units`}
            </span>
            <span className="text-xs text-muted-foreground">
              <LocalDateTime value={item.created} />
            </span>
          </div>
        </div>
      ),
    },
    {
      id: "supplier",
      header: "Supplier",
      className: "hidden sm:table-cell",
      cell: (item) => item.supplierName,
    },
    {
      id: "receipt",
      header: "Receipt",
      className: "hidden sm:table-cell",
      cell: (item) => item.receiptNumber,
    },
    {
      id: "location",
      header: "Location",
      className: "hidden md:table-cell",
      cell: (item) => item.locationName,
    },
    {
      id: "reason",
      header: "Reason",
      className: "hidden md:table-cell",
      cell: (item) => formatPurchaseReturnReason(item.reason),
    },
    {
      id: "status",
      header: "Status",
      className: "hidden sm:table-cell",
      cell: (item) => <PurchaseReturnStatusBadge status={item.status} />,
    },
    {
      id: "quantity",
      header: "Qty",
      className: "hidden sm:table-cell text-right",
      cell: (item) => formatQuantity(item.totalQuantity),
    },
    {
      id: "expectedCredit",
      header: "Expected credit",
      className: "hidden lg:table-cell text-right",
      cell: (item) => formatMoney(item.expectedCreditBase),
    },
    {
      id: "created",
      header: "Created",
      className: "hidden lg:table-cell",
      cell: (item) => <LocalDateTime value={item.created} />,
    },
    {
      id: "actions",
      header: "",
      hideable: false,
      cell: (item) => {
        const isDraft = item.status === PurchaseReturnStatus.Draft;
        return (
          <div className="flex justify-end">
            <Button
              asChild
              aria-label={`${isDraft && canCreate ? "Open" : "View"} purchase return ${item.returnNumber}`}
              size="icon"
              variant="outline"
            >
              <Link href={`/purchasing/returns/${item.id}`}>{isDraft && canCreate ? <Pencil /> : <Eye />}</Link>
            </Button>
          </div>
        );
      },
    },
  ];

  const customSearch = (
    <>
      <Input
        className="w-full sm:w-44"
        aria-label="Search by return number"
        placeholder="Return number..."
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
      rowKey={(item) => String(item.id)}
      isLoading={isPending}
      actions={actions}
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
        icon: Undo2,
        title: "No purchase returns found",
        description: "Try adjusting your search, status, supplier or location filters.",
      }}
    />
  );
}
