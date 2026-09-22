"use client";

import { useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Pencil, Plus, Power, PowerOff, Truck } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { NativeSelect } from "@/components/ui/native-select";
import {
  DataTable,
  type DataTableAction,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { SupplierStatus, SupplierStatusBadge } from "@/modules/purchasing/common";
import { SupplierDialog } from "@/modules/purchasing/suppliers/components/supplier-dialog";
import { SupplierStatusDialog } from "@/modules/purchasing/suppliers/components/supplier-status-dialog";
import type { SupplierDto } from "@/modules/purchasing/suppliers/types/supplier";

interface SuppliersDataTableProps {
  status: string;
  searchValue: string;
  records: SupplierDto[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  error?: DataTableErrorState;
  canManage?: boolean;
}

const STATUS_OPTIONS = [
  { value: SupplierStatus.Active, label: "Active" },
  { value: SupplierStatus.Inactive, label: "Inactive" },
];

export function SuppliersDataTable({
  status,
  searchValue,
  records,
  pageNumber,
  pageSize,
  totalPages,
  totalRecords,
  error,
  canManage,
}: SuppliersDataTableProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();

  const [createOpen, setCreateOpen] = useState(false);
  const [editing, setEditing] = useState<SupplierDto | null>(null);
  const [toggling, setToggling] = useState<SupplierDto | null>(null);

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

  const actions: DataTableAction[] | undefined = canManage
    ? [{ key: "create", label: "New supplier", icon: Plus, onClick: () => setCreateOpen(true) }]
    : undefined;

  const columns: DataTableColumn<SupplierDto>[] = [
    {
      id: "supplier",
      header: "Supplier",
      cell: (supplier) => (
        <div className="flex flex-col gap-1">
          <span className="font-medium">{supplier.name}</span>
          <span className="text-xs text-muted-foreground">{supplier.code}</span>
          {/* Mobile-only: the other columns collapse below `sm`, so their info is folded in here. */}
          <div className="flex flex-col gap-1 sm:hidden">
            <div>
              <SupplierStatusBadge status={supplier.status} />
            </div>
            {supplier.contactName && (
              <span className="text-xs text-muted-foreground">{supplier.contactName}</span>
            )}
            {supplier.phone && <span className="text-xs text-muted-foreground">{supplier.phone}</span>}
            {supplier.email && (
              <span className="break-all text-xs text-muted-foreground">{supplier.email}</span>
            )}
          </div>
        </div>
      ),
    },
    {
      id: "contact",
      header: "Contact",
      className: "hidden sm:table-cell",
      cell: (supplier) => (
        <div className="flex flex-col">
          <span>{supplier.contactName}</span>
          <span className="text-xs text-muted-foreground">{supplier.phone}</span>
        </div>
      ),
    },
    {
      id: "email",
      header: "Email",
      className: "hidden sm:table-cell",
      cell: (supplier) => supplier.email,
    },
    {
      id: "paymentTerms",
      header: "Payment terms",
      className: "hidden md:table-cell",
      cell: (supplier) => supplier.paymentTerms,
    },
    {
      id: "status",
      header: "Status",
      className: "hidden sm:table-cell",
      cell: (supplier) => <SupplierStatusBadge status={supplier.status} />,
    },
    {
      id: "actions",
      header: "",
      hideable: false,
      cell: (supplier) => {
        if (!canManage) return null;
        const active = supplier.status === SupplierStatus.Active;
        return (
          <div className="flex justify-end gap-2">
            <Button
              aria-label={`Edit supplier ${supplier.code}`}
              size="icon"
              variant="outline"
              onClick={() => setEditing(supplier)}
            >
              <Pencil />
            </Button>
            <Button
              aria-label={`${active ? "Deactivate" : "Activate"} supplier ${supplier.code}`}
              size="icon"
              variant="outline"
              onClick={() => setToggling(supplier)}
            >
              {active ? <PowerOff /> : <Power />}
            </Button>
          </div>
        );
      },
    },
  ];

  const customSearch = (
    <>
      <Input
        className="w-full sm:w-56"
        aria-label="Search suppliers"
        placeholder="Code or name..."
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
    </>
  );

  return (
    <>
      <DataTable
        columns={columns}
        data={records}
        rowKey={(supplier) => String(supplier.id)}
        isLoading={isPending}
        actions={actions}
        customSearch={customSearch}
        onCustomSearch={() =>
          navigate({
            q: pendingSearch.trim() || undefined,
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
          icon: Truck,
          title: "No suppliers found",
          description: "Try adjusting your search or status filter.",
        }}
      />
      {canManage && (
        <>
          <SupplierDialog
            key={createOpen ? "create-open" : "create-closed"}
            open={createOpen}
            onOpenChange={setCreateOpen}
          />
          <SupplierDialog
            key={editing ? `edit-${editing.id}` : "edit-closed"}
            open={!!editing}
            onOpenChange={(open) => !open && setEditing(null)}
            supplier={editing ?? undefined}
          />
          <SupplierStatusDialog
            open={!!toggling}
            onOpenChange={(open) => !open && setToggling(null)}
            supplier={toggling}
          />
        </>
      )}
    </>
  );
}
