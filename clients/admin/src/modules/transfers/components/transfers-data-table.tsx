"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { ArrowRight, ArrowRightLeft, Eye, Pencil, Plus } from "lucide-react";
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
import { CreateTransferDialog } from "@/modules/transfers/components/create-transfer-dialog";
import {
  TransferStatusBadge,
  formatTransferStatus,
} from "@/modules/transfers/components/transfer-status-badge";
import { TransferStatus, type StockTransferDto } from "@/modules/transfers/types/transfer";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

interface TransfersDataTableProps {
  locations: LocationTreeNodeDto[];
  status: string;
  sourceLocationId: string;
  destinationLocationId: string;
  searchValue: string;
  records: StockTransferDto[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  error?: DataTableErrorState;
  canCreate?: boolean;
}

const STATUS_OPTIONS = Object.values(TransferStatus).map((value) => ({
  value,
  label: formatTransferStatus(value),
}));

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

export function TransfersDataTable({
  locations,
  status,
  sourceLocationId,
  destinationLocationId,
  searchValue,
  records,
  pageNumber,
  pageSize,
  totalPages,
  totalRecords,
  error,
  canCreate,
}: TransfersDataTableProps) {
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

  const [pendingStatus, setPendingStatus] = useState(status);
  const [lastStatus, setLastStatus] = useState(status);
  if (status !== lastStatus) {
    setLastStatus(status);
    setPendingStatus(status);
  }

  const [pendingSource, setPendingSource] = useState(sourceLocationId);
  const [lastSource, setLastSource] = useState(sourceLocationId);
  if (sourceLocationId !== lastSource) {
    setLastSource(sourceLocationId);
    setPendingSource(sourceLocationId);
  }

  const [pendingDestination, setPendingDestination] = useState(destinationLocationId);
  const [lastDestination, setLastDestination] = useState(destinationLocationId);
  if (destinationLocationId !== lastDestination) {
    setLastDestination(destinationLocationId);
    setPendingDestination(destinationLocationId);
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
    ? [
        {
          key: "create",
          label: "New transfer",
          icon: Plus,
          onClick: () => setCreateOpen(true),
        },
      ]
    : undefined;

  const columns: DataTableColumn<StockTransferDto>[] = [
    {
      id: "transferCode",
      header: "Transfer",
      cell: (transfer) => (
        <div className="flex flex-col gap-1">
          <span className="font-medium">{transfer.transferCode}</span>
          {/* Mobile-only: the other columns collapse (hidden below `sm`), so their
              info is folded into this card-style block instead of being lost. */}
          <div className="flex flex-col gap-1 sm:hidden">
            <span className="flex flex-wrap items-center gap-1 text-xs text-muted-foreground">
              {transfer.sourceLocationName}
              <ArrowRight className="size-3" aria-hidden />
              {transfer.destinationLocationName}
            </span>
            <div>
              <TransferStatusBadge status={transfer.status} />
            </div>
            <span className="text-xs text-muted-foreground">
              {`Requested ${formatQuantity(transfer.totalRequestedQuantity)} · ${transfer.lines?.length ?? 0} lines`}
            </span>
            <span className="text-xs text-muted-foreground">
              {`In transit ${formatQuantity(transfer.totalInTransitQuantity)}`}
            </span>
            <span className="text-xs text-muted-foreground">
              <LocalDateTime value={transfer.requestedAt} />
            </span>
          </div>
        </div>
      ),
    },
    {
      id: "route",
      header: "Route",
      className: "hidden sm:table-cell",
      cell: (transfer) => (
        <span className="flex flex-wrap items-center gap-1.5">
          {transfer.sourceLocationName}
          <ArrowRight className="size-3.5 text-muted-foreground" aria-hidden />
          {transfer.destinationLocationName}
        </span>
      ),
    },
    {
      id: "status",
      header: "Status",
      className: "hidden sm:table-cell",
      cell: (transfer) => <TransferStatusBadge status={transfer.status} />,
    },
    {
      id: "quantity",
      header: "Requested qty",
      className: "hidden sm:table-cell",
      cell: (transfer) => formatQuantity(transfer.totalRequestedQuantity),
    },
    {
      id: "inTransit",
      header: "In transit",
      className: "hidden sm:table-cell",
      cell: (transfer) => formatQuantity(transfer.totalInTransitQuantity),
    },
    {
      id: "created",
      header: "Created",
      className: "hidden sm:table-cell",
      cell: (transfer) => <LocalDateTime value={transfer.requestedAt} />,
    },
    {
      id: "actions",
      header: "",
      hideable: false,
      cell: (transfer) => {
        const isDraft = transfer.status === TransferStatus.Draft;
        return (
          <div className="flex justify-end">
            <Button
              asChild
              aria-label={`${isDraft ? "Resume" : "View"} transfer ${transfer.transferCode}`}
              size="icon"
              variant="outline"
            >
              <Link href={`/transfers/${transfer.id}`}>{isDraft && canCreate ? <Pencil /> : <Eye />}</Link>
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
        aria-label="Search by transfer code"
        placeholder="Transfer code..."
        value={pendingSearch}
        onChange={(event) => setPendingSearch(event.target.value)}
      />
      <NativeSelect
        className="w-full sm:w-44"
        aria-label="Filter by status"
        placeholder="All statuses"
        value={pendingStatus}
        onChange={setPendingStatus}
        options={STATUS_OPTIONS}
      />
      <NativeSelect
        className="w-full sm:w-48"
        aria-label="Filter by source location"
        placeholder="Any source"
        value={pendingSource}
        onChange={setPendingSource}
        options={locations.map((location) => ({ value: String(location.id), label: location.name }))}
      />
      <NativeSelect
        className="w-full sm:w-48"
        aria-label="Filter by destination location"
        placeholder="Any destination"
        value={pendingDestination}
        onChange={setPendingDestination}
        options={locations.map((location) => ({ value: String(location.id), label: location.name }))}
      />
    </>
  );

  return (
    <>
      <DataTable
        columns={columns}
        data={records}
        rowKey={(transfer) => String(transfer.id)}
        isLoading={isPending}
        actions={actions}
        customSearch={customSearch}
        onCustomSearch={() =>
          navigate({
            q: pendingSearch.trim() || undefined,
            status: pendingStatus || undefined,
            sourceLocationId: pendingSource || undefined,
            destinationLocationId: pendingDestination || undefined,
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
          icon: ArrowRightLeft,
          title: "No transfers found",
          description: "Try adjusting your search, status or location filters.",
        }}
      />
      {canCreate && (
        <CreateTransferDialog open={createOpen} onOpenChange={setCreateOpen} locations={locations} />
      )}
    </>
  );
}
