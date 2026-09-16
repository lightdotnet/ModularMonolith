"use client";

import { useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Eye, Pencil, Plus, ShoppingCart } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { NativeSelect } from "@/components/ui/native-select";
import {
  DataTable,
  type DataTableAction,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { OrderPanel } from "@/modules/orders/components/order-panel";
import { OrderStatusBadge } from "@/modules/orders/components/order-status-badge";
import { OrderStatus, type OrderDto } from "@/modules/orders/types/order";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

interface OrdersDataTableProps {
  locations: LocationTreeNodeDto[];
  locationId: string;
  status: string;
  records: OrderDto[];
  searchValue: string;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  error?: DataTableErrorState;
  canManage?: boolean;
}

const STATUS_OPTIONS = Object.values(OrderStatus).map((value) => ({ value, label: value }));

function formatAmount(amount: number, currency: string): string {
  try {
    return new Intl.NumberFormat("en-US", { style: "currency", currency }).format(amount);
  } catch {
    return `${amount} ${currency}`;
  }
}

export function OrdersDataTable({
  locations,
  locationId,
  status,
  records,
  searchValue,
  pageNumber,
  pageSize,
  totalPages,
  totalRecords,
  error,
  canManage,
}: OrdersDataTableProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();

  const [panelOpen, setPanelOpen] = useState(false);
  const [panelKey, setPanelKey] = useState(0);
  const [panelOrderId, setPanelOrderId] = useState<string | null>(null);

  const [pendingSearch, setPendingSearch] = useState(searchValue);
  const [lastSearch, setLastSearch] = useState(searchValue);
  if (searchValue !== lastSearch) {
    setLastSearch(searchValue);
    setPendingSearch(searchValue);
  }

  const [pendingLocationId, setPendingLocationId] = useState(locationId);
  const [lastLocationId, setLastLocationId] = useState(locationId);
  if (locationId !== lastLocationId) {
    setLastLocationId(locationId);
    setPendingLocationId(locationId);
  }

  const [pendingStatus, setPendingStatus] = useState(status);
  const [lastStatus, setLastStatus] = useState(status);
  if (status !== lastStatus) {
    setLastStatus(status);
    setPendingStatus(status);
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

  function openCreate() {
    setPanelOrderId(null);
    setPanelKey((key) => key + 1);
    setPanelOpen(true);
  }

  function openOrder(order: OrderDto) {
    setPanelOrderId(order.id);
    setPanelKey((key) => key + 1);
    setPanelOpen(true);
  }

  const actions: DataTableAction[] | undefined = canManage
    ? [
        {
          key: "create",
          label: "New order",
          icon: Plus,
          onClick: openCreate,
        },
      ]
    : undefined;

  const columns: DataTableColumn<OrderDto>[] = [
    {
      id: "orderCode",
      header: "Order",
      cell: (order) => (
        <div className="flex flex-col gap-1">
          <span className="font-medium">{order.orderCode}</span>
          {/* Mobile-only: the other columns collapse (hidden below `sm`), so their
              info is folded into this card-style block instead of being lost. */}
          <div className="flex flex-col gap-1 sm:hidden">
            <OrderStatusBadge status={order.status} />
            <span className="text-xs text-muted-foreground">{locationName(order.locationId)}</span>
            <span className="text-sm">{formatAmount(order.total, order.currency)}</span>
            <span className="text-xs text-muted-foreground">
              {order.placedAt ? new Date(order.placedAt).toLocaleString() : ""}
            </span>
          </div>
        </div>
      ),
    },
    {
      id: "location",
      header: "Location",
      className: "hidden sm:table-cell",
      cell: (order) => locationName(order.locationId),
    },
    {
      id: "status",
      header: "Status",
      className: "hidden sm:table-cell",
      cell: (order) => <OrderStatusBadge status={order.status} />,
    },
    {
      id: "total",
      header: "Total",
      className: "hidden sm:table-cell",
      cell: (order) => formatAmount(order.total, order.currency),
    },
    {
      id: "date",
      header: "Placed / Created",
      className: "hidden sm:table-cell",
      cell: (order) => (order.placedAt ? new Date(order.placedAt).toLocaleString() : ""),
    },
    {
      id: "actions",
      header: "",
      hideable: false,
      cell: (order) => (
        <div className="flex justify-end">
          <Button
            aria-label={order.status === OrderStatus.Draft ? "Resume order" : "View order"}
            onClick={() => openOrder(order)}
            size="icon"
            variant="outline"
          >
            {order.status === OrderStatus.Draft ? <Pencil /> : <Eye />}
          </Button>
        </div>
      ),
    },
  ];

  const customSearch = (
    <>
      <Input
        className="w-full sm:w-56"
        aria-label="Search orders"
        placeholder="Search orders..."
        value={pendingSearch}
        onChange={(event) => setPendingSearch(event.target.value)}
      />
      <NativeSelect
        className="w-full sm:w-56"
        aria-label="Filter by location"
        placeholder="All locations"
        value={pendingLocationId}
        onChange={setPendingLocationId}
        options={locations.map((location) => ({ value: location.id, label: location.name }))}
      />
      <NativeSelect
        className="w-full sm:w-40"
        aria-label="Filter by status"
        placeholder="All statuses"
        value={pendingStatus}
        onChange={setPendingStatus}
        options={STATUS_OPTIONS}
      />
    </>
  );

  return (
    <>
      <DataTable
        columns={columns}
        data={records}
        rowKey={(order) => order.id}
        isLoading={isPending}
        actions={actions}
        customSearch={customSearch}
        onCustomSearch={() =>
          navigate({
            q: pendingSearch || undefined,
            locationId: pendingLocationId || undefined,
            status: pendingStatus || undefined,
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
          icon: ShoppingCart,
          title: "No orders found",
          description: "Try adjusting your search, location or status filter.",
        }}
      />
      <OrderPanel
        key={`panel-${panelKey}`}
        open={panelOpen}
        onOpenChange={setPanelOpen}
        orderId={panelOrderId}
        locations={locations}
        onChanged={() => router.refresh()}
      />
    </>
  );
}
