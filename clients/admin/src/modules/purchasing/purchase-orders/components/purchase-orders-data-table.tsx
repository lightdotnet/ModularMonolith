"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { ClipboardList, Eye, Pencil, Plus } from "lucide-react";
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
import { cn } from "@/lib/shared/utils";
import {
  PurchaseOrderStatus,
  PurchaseOrderStatusBadge,
  formatDateOnly,
  formatPurchaseOrderStatus,
} from "@/modules/purchasing/common";
import { PurchaseOrderHeaderDialog } from "@/modules/purchasing/purchase-orders/components/purchase-order-header-dialog";
import type { PurchaseOrderDto, SupplierOption } from "@/modules/purchasing/purchase-orders/types/purchase-order";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

interface PurchaseOrdersDataTableProps {
  locations: LocationTreeNodeDto[];
  /** Every supplier, for the filter. */
  suppliers: SupplierOption[];
  /** Active suppliers only, for the create dialog. */
  activeSuppliers: SupplierOption[];
  status: string;
  supplierId: string;
  locationId: string;
  searchValue: string;
  records: PurchaseOrderDto[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  error?: DataTableErrorState;
  canCreate?: boolean;
}

const STATUS_TABS: { value: string; label: string }[] = [
  { value: "", label: "All" },
  ...Object.values(PurchaseOrderStatus).map((value) => ({
    value,
    label: formatPurchaseOrderStatus(value),
  })),
];

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

function formatMoney(amount: number | null | undefined): string {
  if (amount === null || amount === undefined) return "";
  return new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}

export function PurchaseOrdersDataTable({
  locations,
  suppliers,
  activeSuppliers,
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
}: PurchaseOrdersDataTableProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();

  const [createOpen, setCreateOpen] = useState(false);

  const [pendingSearch, setPendingSearch] = useState(searchValue);
  const [lastSearch, setLastSearch] = useState(searchValue);
  if (searchValue !== lastSearch) {
    setLastSearch(searchValue);
    setPendingSearch(searchValue);
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

  const actions: DataTableAction[] | undefined = canCreate
    ? [{ key: "create", label: "New purchase order", icon: Plus, onClick: () => setCreateOpen(true) }]
    : undefined;

  const columns: DataTableColumn<PurchaseOrderDto>[] = [
    {
      id: "poNumber",
      header: "Purchase order",
      cell: (order) => (
        <div className="flex flex-col gap-1">
          <span className="font-medium">{order.poNumber}</span>
          {/* Mobile-only: the other columns collapse below `sm`, so their info is folded in here. */}
          <div className="flex flex-col gap-1 sm:hidden">
            <span className="text-xs text-muted-foreground">{order.supplierName}</span>
            <span className="text-xs text-muted-foreground">{order.locationName}</span>
            <div>
              <PurchaseOrderStatusBadge status={order.status} />
            </div>
            <span className="text-xs text-muted-foreground">
              {`Ordered ${formatQuantity(order.totalOrderedQuantity)} · Received ${formatQuantity(order.totalReceivedQuantity)}`}
            </span>
            <span className="text-xs text-muted-foreground">{`${order.currency} ${formatMoney(order.totalAmount)}`}</span>
            <span className="text-xs text-muted-foreground">
              <LocalDateTime value={order.created} />
            </span>
          </div>
        </div>
      ),
    },
    {
      id: "supplier",
      header: "Supplier",
      className: "hidden sm:table-cell",
      cell: (order) => order.supplierName,
    },
    {
      id: "location",
      header: "Receiving location",
      className: "hidden sm:table-cell",
      cell: (order) => order.locationName,
    },
    {
      id: "status",
      header: "Status",
      className: "hidden sm:table-cell",
      cell: (order) => <PurchaseOrderStatusBadge status={order.status} />,
    },
    {
      id: "expected",
      header: "Expected",
      className: "hidden lg:table-cell",
      cell: (order) => formatDateOnly(order.expectedAt),
    },
    {
      id: "received",
      header: "Received / ordered",
      className: "hidden sm:table-cell",
      cell: (order) =>
        `${formatQuantity(order.totalReceivedQuantity)} / ${formatQuantity(order.totalOrderedQuantity)}`,
    },
    {
      id: "total",
      header: "Total",
      className: "hidden sm:table-cell text-right",
      cell: (order) => `${order.currency} ${formatMoney(order.totalAmount)}`,
    },
    {
      id: "created",
      header: "Created",
      className: "hidden lg:table-cell",
      cell: (order) => <LocalDateTime value={order.created} />,
    },
    {
      id: "actions",
      header: "",
      hideable: false,
      cell: (order) => {
        const editable =
          canCreate &&
          (order.status === PurchaseOrderStatus.Draft || order.status === PurchaseOrderStatus.Rejected);
        return (
          <div className="flex justify-end">
            <Button
              asChild
              aria-label={`${editable ? "Open" : "View"} purchase order ${order.poNumber}`}
              size="icon"
              variant="outline"
            >
              <Link href={`/purchasing/orders/${order.id}`}>{editable ? <Pencil /> : <Eye />}</Link>
            </Button>
          </div>
        );
      },
    },
  ];

  const customSearch = (
    <>
      <Input
        className="w-full sm:w-48"
        aria-label="Search by PO number"
        placeholder="PO number..."
        value={pendingSearch}
        onChange={(event) => setPendingSearch(event.target.value)}
      />
      <NativeSelect
        className="w-full sm:w-52"
        aria-label="Filter by supplier"
        placeholder="Any supplier"
        value={pendingSupplier}
        onChange={setPendingSupplier}
        options={suppliers.map((supplier) => ({ value: supplier.id, label: supplier.name }))}
      />
      <NativeSelect
        className="w-full sm:w-52"
        aria-label="Filter by receiving location"
        placeholder="Any location"
        value={pendingLocation}
        onChange={setPendingLocation}
        options={locations.map((location) => ({ value: String(location.id), label: location.name }))}
      />
    </>
  );

  return (
    <>
      {/* Status tabs — a horizontally scrollable pill row so all eight statuses stay reachable on mobile. */}
      <div
        role="group"
        aria-label="Filter by status"
        className="-mx-1 mb-4 flex gap-1.5 overflow-x-auto px-1 pb-1"
      >
        {STATUS_TABS.map((tab) => (
          <Button
            key={tab.value || "all"}
            type="button"
            size="sm"
            variant={status === tab.value ? "default" : "outline"}
            aria-pressed={status === tab.value}
            className={cn("shrink-0")}
            onClick={() => navigate({ status: tab.value || undefined, page: undefined })}
          >
            {tab.label}
          </Button>
        ))}
      </div>

      <DataTable
        columns={columns}
        data={records}
        rowKey={(order) => String(order.id)}
        isLoading={isPending}
        actions={actions}
        customSearch={customSearch}
        onCustomSearch={() =>
          navigate({
            q: pendingSearch.trim() || undefined,
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
          icon: ClipboardList,
          title: "No purchase orders found",
          description: "Try adjusting your search, status, supplier or location filters.",
        }}
      />
      {canCreate && (
        <PurchaseOrderHeaderDialog
          open={createOpen}
          onOpenChange={setCreateOpen}
          locations={locations}
          suppliers={activeSuppliers}
        />
      )}
    </>
  );
}
