"use client";

import { useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Coins, Pencil, Plus, Power, PowerOff } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { NativeSelect } from "@/components/ui/native-select";
import {
  DataTable,
  type DataTableAction,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { CurrencyDialog } from "@/modules/currency/currencies/components/currency-dialog";
import { CurrencyStatusDialog } from "@/modules/currency/currencies/components/currency-status-dialog";
import type { CurrencyDto } from "@/modules/currency/currencies/types/currency";

interface CurrenciesDataTableProps {
  searchValue: string;
  /** "true" | "false" | "" (all). */
  active: string;
  records: CurrencyDto[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  error?: DataTableErrorState;
  canManage?: boolean;
}

const ACTIVE_OPTIONS = [
  { value: "true", label: "Active" },
  { value: "false", label: "Inactive" },
];

function CurrencyBadges({ currency }: { currency: CurrencyDto }) {
  return (
    <div className="flex flex-wrap items-center gap-1">
      {currency.isBase && <Badge>Base</Badge>}
      <Badge variant={currency.isActive ? "default" : "outline"}>
        {currency.isActive ? "Active" : "Inactive"}
      </Badge>
    </div>
  );
}

export function CurrenciesDataTable({
  searchValue,
  active,
  records,
  pageNumber,
  pageSize,
  totalPages,
  totalRecords,
  error,
  canManage,
}: CurrenciesDataTableProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();

  const [createOpen, setCreateOpen] = useState(false);
  const [editing, setEditing] = useState<CurrencyDto | null>(null);
  const [toggling, setToggling] = useState<CurrencyDto | null>(null);

  const [pendingSearch, setPendingSearch] = useState(searchValue);
  const [lastSearch, setLastSearch] = useState(searchValue);
  if (searchValue !== lastSearch) {
    setLastSearch(searchValue);
    setPendingSearch(searchValue);
  }

  const [pendingActive, setPendingActive] = useState(active);
  const [lastActive, setLastActive] = useState(active);
  if (active !== lastActive) {
    setLastActive(active);
    setPendingActive(active);
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
    ? [{ key: "create", label: "New currency", icon: Plus, onClick: () => setCreateOpen(true) }]
    : undefined;

  const columns: DataTableColumn<CurrencyDto>[] = [
    {
      id: "currency",
      header: "Currency",
      cell: (currency) => (
        <div className="flex flex-col gap-1">
          <span className="font-medium">
            {currency.code}
            {currency.symbol && <span className="ml-2 text-muted-foreground">{currency.symbol}</span>}
          </span>
          <span className="text-xs text-muted-foreground">{currency.name}</span>
          {/* Mobile-only: the other columns collapse below `sm`, so their info is folded in here. */}
          <div className="flex flex-col gap-1 sm:hidden">
            <CurrencyBadges currency={currency} />
            <span className="text-xs text-muted-foreground">
              {currency.decimalPlaces} decimal place{currency.decimalPlaces === 1 ? "" : "s"}
            </span>
          </div>
        </div>
      ),
    },
    {
      id: "symbol",
      header: "Symbol",
      className: "hidden sm:table-cell",
      cell: (currency) => currency.symbol,
    },
    {
      id: "decimalPlaces",
      header: "Decimal places",
      className: "hidden sm:table-cell",
      cell: (currency) => currency.decimalPlaces,
    },
    {
      id: "status",
      header: "Status",
      className: "hidden sm:table-cell",
      cell: (currency) => <CurrencyBadges currency={currency} />,
    },
    {
      id: "actions",
      header: "",
      hideable: false,
      cell: (currency) => {
        if (!canManage) return null;
        return (
          <div className="flex justify-end gap-2">
            <Button
              aria-label={`Edit currency ${currency.code}`}
              size="icon"
              variant="outline"
              onClick={() => setEditing(currency)}
            >
              <Pencil />
            </Button>
            {/* The base currency cannot be deactivated, so the toggle is absent for it. */}
            {!currency.isBase && (
              <Button
                aria-label={`${currency.isActive ? "Deactivate" : "Activate"} currency ${currency.code}`}
                size="icon"
                variant="outline"
                onClick={() => setToggling(currency)}
              >
                {currency.isActive ? <PowerOff /> : <Power />}
              </Button>
            )}
          </div>
        );
      },
    },
  ];

  const customSearch = (
    <>
      <Input
        className="w-full sm:w-56"
        aria-label="Search currencies"
        placeholder="Code or name..."
        value={pendingSearch}
        onChange={(event) => setPendingSearch(event.target.value)}
      />
      <NativeSelect
        className="w-full sm:w-44"
        aria-label="Filter by status"
        placeholder="All statuses"
        value={pendingActive}
        onChange={setPendingActive}
        options={ACTIVE_OPTIONS}
      />
    </>
  );

  return (
    <>
      <DataTable
        columns={columns}
        data={records}
        rowKey={(currency) => currency.code}
        isLoading={isPending}
        actions={actions}
        customSearch={customSearch}
        onCustomSearch={() =>
          navigate({
            q: pendingSearch.trim() || undefined,
            active: pendingActive || undefined,
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
          icon: Coins,
          title: "No currencies found",
          description: "Try adjusting your search or status filter.",
        }}
      />
      {canManage && (
        <>
          <CurrencyDialog
            key={createOpen ? "create-open" : "create-closed"}
            open={createOpen}
            onOpenChange={setCreateOpen}
          />
          <CurrencyDialog
            key={editing ? `edit-${editing.code}` : "edit-closed"}
            open={!!editing}
            onOpenChange={(open) => !open && setEditing(null)}
            currency={editing ?? undefined}
          />
          <CurrencyStatusDialog
            open={!!toggling}
            onOpenChange={(open) => !open && setToggling(null)}
            currency={toggling}
          />
        </>
      )}
    </>
  );
}
