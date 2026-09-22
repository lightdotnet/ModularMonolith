"use client";

import { useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { ArrowLeftRight, Plus } from "lucide-react";
import { Input } from "@/components/ui/input";
import { NativeSelect } from "@/components/ui/native-select";
import {
  DataTable,
  type DataTableAction,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { CURRENCY_LIMITS } from "@/modules/currency/common/constants/limits";
import {
  RecordRateDialog,
  type RateCurrencyOption,
} from "@/modules/currency/exchange-rates/components/record-rate-dialog";
import type { ExchangeRateDto } from "@/modules/currency/exchange-rates/types/exchange-rate";
import { formatRate } from "@/modules/currency/exchange-rates/utils/format-rate";

interface ExchangeRatesDataTableProps {
  currencyCode: string;
  /** `yyyy-MM-dd` or "". */
  from: string;
  to: string;
  records: ExchangeRateDto[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  error?: DataTableErrorState;
  canManage?: boolean;
  /** Active non-base currencies for the record dialog. */
  recordCurrencies: RateCurrencyOption[];
  /** Currencies offered by the history filter (every currency, so history of inactive ones stays reachable). */
  filterCurrencies: RateCurrencyOption[];
  lookupFailed?: boolean;
  truncated?: boolean;
  baseCode?: string;
}

export function ExchangeRatesDataTable({
  currencyCode,
  from,
  to,
  records,
  pageNumber,
  pageSize,
  totalPages,
  totalRecords,
  error,
  canManage,
  recordCurrencies,
  filterCurrencies,
  lookupFailed,
  truncated,
  baseCode,
}: ExchangeRatesDataTableProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [recordOpen, setRecordOpen] = useState(false);

  const [pendingCode, setPendingCode] = useState(currencyCode);
  const [lastCode, setLastCode] = useState(currencyCode);
  if (currencyCode !== lastCode) {
    setLastCode(currencyCode);
    setPendingCode(currencyCode);
  }

  const [pendingFrom, setPendingFrom] = useState(from);
  const [lastFrom, setLastFrom] = useState(from);
  if (from !== lastFrom) {
    setLastFrom(from);
    setPendingFrom(from);
  }

  const [pendingTo, setPendingTo] = useState(to);
  const [lastTo, setLastTo] = useState(to);
  if (to !== lastTo) {
    setLastTo(to);
    setPendingTo(to);
  }

  const rangeInvalid = !!pendingFrom && !!pendingTo && pendingTo < pendingFrom;

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
    ? [{ key: "record", label: "Record rate", icon: Plus, onClick: () => setRecordOpen(true) }]
    : undefined;

  const columns: DataTableColumn<ExchangeRateDto>[] = [
    {
      id: "rate",
      header: "Currency / rate",
      cell: (rate) => (
        <div className="flex flex-col gap-1">
          <span className="font-medium">
            1 {rate.currencyCode} = {formatRate(rate.rate)} {baseCode}
          </span>
          {/* Mobile-only: the other columns collapse below `sm`, so their info is folded in here. */}
          <div className="flex flex-col gap-1 text-xs text-muted-foreground sm:hidden">
            <span>
              Effective <LocalDateTime value={rate.effectiveFrom} />
            </span>
            <span className="break-all">{rate.recordedBy}</span>
            {rate.note && <span>{rate.note}</span>}
          </div>
        </div>
      ),
    },
    {
      id: "effectiveFrom",
      header: "Effective from",
      className: "hidden sm:table-cell",
      cell: (rate) => <LocalDateTime value={rate.effectiveFrom} />,
    },
    {
      id: "recordedBy",
      header: "Recorded by",
      className: "hidden sm:table-cell",
      cell: (rate) => <span className="break-all">{rate.recordedBy}</span>,
    },
    {
      id: "note",
      header: "Note",
      className: "hidden md:table-cell",
      cell: (rate) => rate.note,
    },
  ];

  const currencyOptions = filterCurrencies.map((currency) => ({
    value: currency.code,
    label: `${currency.code} - ${currency.name}`,
  }));

  const customSearch = (
    <>
      {filterCurrencies.length > 0 ? (
        <NativeSelect
          className="w-full sm:w-52"
          aria-label="Filter by currency"
          placeholder="All currencies"
          value={pendingCode}
          onChange={setPendingCode}
          options={currencyOptions}
        />
      ) : (
        <Input
          className="w-full sm:w-32"
          aria-label="Filter by currency code"
          placeholder="Code, e.g. USD"
          maxLength={CURRENCY_LIMITS.codeLength}
          value={pendingCode}
          onChange={(event) => setPendingCode(event.target.value.toUpperCase())}
        />
      )}
      {/* The page is server-rendered and cannot know the viewer's time zone, so the day bounds are UTC and labelled so. */}
      <div className="flex w-full flex-col gap-1 sm:w-40">
        <span className="text-xs text-muted-foreground">From (UTC)</span>
        <Input
          type="date"
          aria-label="Effective from date (UTC)"
          value={pendingFrom}
          max={pendingTo || undefined}
          onChange={(event) => setPendingFrom(event.target.value)}
        />
      </div>
      <div className="flex w-full flex-col gap-1 sm:w-40">
        <span className="text-xs text-muted-foreground">To (UTC)</span>
        <Input
          type="date"
          aria-label="Effective to date (UTC)"
          value={pendingTo}
          min={pendingFrom || undefined}
          onChange={(event) => setPendingTo(event.target.value)}
          aria-invalid={rangeInvalid || undefined}
        />
      </div>
    </>
  );

  return (
    <>
      <DataTable
        columns={columns}
        data={records}
        rowKey={(rate) => String(rate.id)}
        isLoading={isPending}
        actions={actions}
        customSearch={customSearch}
        onCustomSearch={() => {
          if (rangeInvalid) return;
          navigate({
            currency: pendingCode.trim() || undefined,
            from: pendingFrom || undefined,
            to: pendingTo || undefined,
            page: undefined,
          });
        }}
        onRefresh={() => startTransition(() => router.refresh())}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalPages={totalPages}
        totalRecords={totalRecords}
        onPageChange={(page) => navigate({ page: String(page) })}
        error={error}
        emptyState={{
          icon: ArrowLeftRight,
          title: "No exchange rates found",
          description: "Try adjusting the currency or date range.",
        }}
      />
      {canManage && (
        <RecordRateDialog
          key={recordOpen ? "record-open" : "record-closed"}
          open={recordOpen}
          onOpenChange={setRecordOpen}
          currencies={recordCurrencies}
          lookupFailed={lookupFailed}
          truncated={truncated}
          baseCode={baseCode}
          initialCurrencyCode={currencyCode || undefined}
        />
      )}
    </>
  );
}
