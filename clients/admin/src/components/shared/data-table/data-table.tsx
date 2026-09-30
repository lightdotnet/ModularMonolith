"use client";

import { useMemo, useState } from "react";
import { ArrowDown, ArrowUp, ArrowUpDown, Inbox } from "lucide-react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import { Empty, EmptyDescription, EmptyMedia, EmptyTitle } from "@/components/ui/empty";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { DataTableToolbar } from "@/components/shared/data-table/data-table-toolbar";
import { DataTableButtons } from "@/components/shared/data-table/data-table-buttons";
import { DataTablePagination } from "@/components/shared/data-table/data-table-pagination";
import { DataTableVirtualBody } from "@/components/shared/data-table/data-table-virtual-body";
import { cn } from "@/lib/shared/utils";
import type {
  DataTableAction,
  DataTableColumn,
  DataTableEmptyState,
  DataTableErrorState,
} from "@/components/shared/data-table/types";

interface SortState {
  columnId: string;
  direction: "asc" | "desc";
}

const SKELETON_ROWS = 5;

export interface DataTableProps<TData> {
  columns: DataTableColumn<TData>[];
  data: TData[];
  rowKey: (row: TData) => string;
  isLoading?: boolean;

  actions?: DataTableAction[];

  searchValue?: string;
  onSearchChange?: (value: string) => void;
  searchPlaceholder?: string;

  /** Custom multi-field filter UI rendered in the search row instead of the built-in text search. */
  customSearch?: React.ReactNode;
  /** When set alongside `customSearch`, renders a "Search" button that applies the custom filters on click rather than as each field changes. */
  onCustomSearch?: () => void;

  onExport?: () => void;
  onRefresh?: () => void;

  /**
   * `"paginated"` (default) keeps today's exact behavior — server-driven
   * pagination via `pageNumber`/`totalPages`/`onPageChange`. `"virtualized"`
   * and `"infinite"` render through DataTableVirtualBody instead, for large
   * or open-ended result sets; both switch the row markup to an ARIA-grid
   * div layout since a native `<table>` can't virtualize its rows cleanly.
   */
  mode?: "paginated" | "virtualized" | "infinite";
  /** Row height estimate for virtualized/infinite modes. Defaults to 40px. */
  rowHeight?: number;
  /** Scroll-container height for virtualized/infinite modes. Defaults to 480px. */
  virtualHeight?: number;
  /** `"infinite"` mode only: called when the user scrolls near the end of the loaded rows. */
  onLoadMore?: () => void;
  /** `"infinite"` mode only: whether more rows exist beyond `data`. */
  hasMore?: boolean;

  /**
   * When provided, column sort clicks call this instead of sorting `data`
   * locally — required for `"virtualized"`/`"infinite"` modes with a
   * server-paged/streamed dataset, optional for `"paginated"` callers who
   * still want the default client-side sort of their single fetched page.
   */
  onSortChange?: (columnId: string, direction: "asc" | "desc" | null) => void;

  pageNumber?: number;
  pageSize?: number;
  totalPages?: number;
  totalRecords: number;
  onPageChange?: (page: number) => void;

  emptyState?: DataTableEmptyState;
  error?: DataTableErrorState;
}

export function DataTable<TData>({
  columns,
  data,
  rowKey,
  isLoading,
  actions,
  searchValue,
  onSearchChange,
  searchPlaceholder,
  customSearch,
  onCustomSearch,
  onExport,
  onRefresh,
  mode = "paginated",
  rowHeight,
  virtualHeight,
  onLoadMore,
  hasMore,
  onSortChange,
  pageNumber,
  totalPages,
  totalRecords,
  onPageChange,
  emptyState,
  error,
}: DataTableProps<TData>) {
  const [hiddenColumnIds, setHiddenColumnIds] = useState<Set<string>>(new Set());
  const [sortState, setSortState] = useState<SortState | null>(null);

  function toggleColumn(columnId: string) {
    setHiddenColumnIds((previous) => {
      const next = new Set(previous);
      if (next.has(columnId)) next.delete(columnId);
      else next.add(columnId);
      return next;
    });
  }

  function toggleSort(columnId: string) {
    const next: SortState | null =
      !sortState || sortState.columnId !== columnId
        ? { columnId, direction: "asc" }
        : sortState.direction === "asc"
          ? { columnId, direction: "desc" }
          : null;

    setSortState(next);
    onSortChange?.(columnId, next?.direction ?? null);
  }

  function renderHeaderContent(column: DataTableColumn<TData>) {
    if (!column.sortable || !column.sortValue) return column.header;

    return (
      <button
        type="button"
        onClick={() => toggleSort(column.id)}
        className="inline-flex items-center gap-1 hover:text-foreground"
      >
        {column.header}
        {sortState?.columnId === column.id ? (
          sortState.direction === "asc" ? (
            <ArrowUp className="size-3.5" />
          ) : (
            <ArrowDown className="size-3.5" />
          )
        ) : (
          <ArrowUpDown className="size-3.5 opacity-50" />
        )}
      </button>
    );
  }

  const visibleColumns = columns.filter((column) => !hiddenColumnIds.has(column.id));
  const hasButtons = !!(onExport || onRefresh || columns.some((column) => column.hideable !== false));
  const buttonsNode = hasButtons ? (
    <DataTableButtons
      columns={columns}
      hiddenColumnIds={hiddenColumnIds}
      onToggleColumn={toggleColumn}
      onExport={onExport}
      onRefresh={onRefresh}
      isLoading={isLoading}
    />
  ) : null;
  const isEmpty = !isLoading && data.length === 0;
  // While a fresh request is in flight, prefer the table's own loading state
  // (skeleton rows) over a stale error left over from the previous render.
  const showError = !!error && !isLoading;

  const sortColumn = sortState
    ? columns.find((column) => column.id === sortState.columnId)
    : undefined;

  // Sorts only the `data` that was passed in — meaningless for callers that
  // paginate server-side and hand DataTable a single page at a time. Skipped
  // entirely when `onSortChange` is provided: the caller owns sorting then.
  const sortedData = useMemo(() => {
    if (onSortChange || !sortState || !sortColumn?.sortValue) return data;

    const { direction } = sortState;
    const { sortValue } = sortColumn;
    return [...data].sort((a, b) => {
      const aValue = sortValue(a);
      const bValue = sortValue(b);
      if (aValue < bValue) return direction === "asc" ? -1 : 1;
      if (aValue > bValue) return direction === "asc" ? 1 : -1;
      return 0;
    });
  }, [data, sortState, sortColumn, onSortChange]);

  const content = showError ? (
    <Alert variant="destructive">
      <AlertTitle>{error.title}</AlertTitle>
      {error.description && <AlertDescription>{error.description}</AlertDescription>}
    </Alert>
  ) : isEmpty ? (
    <Empty>
      <EmptyMedia variant="icon">{emptyState?.icon ? <emptyState.icon /> : <Inbox />}</EmptyMedia>
      <EmptyTitle>{emptyState?.title ?? "No records"}</EmptyTitle>
      {emptyState?.description && <EmptyDescription>{emptyState.description}</EmptyDescription>}
    </Empty>
  ) : mode === "paginated" ? (
    <Table>
      <TableHeader className="bg-muted">
        <TableRow>
          {visibleColumns.map((column) => (
            <TableHead key={column.id} className={cn("text-muted-foreground", column.className)}>
              {renderHeaderContent(column)}
            </TableHead>
          ))}
        </TableRow>
      </TableHeader>
      <TableBody>
        {isLoading
          ? Array.from({ length: SKELETON_ROWS }, (_, rowIndex) => (
              <TableRow key={`skeleton-${rowIndex}`}>
                {visibleColumns.map((column) => (
                  <TableCell key={column.id} className={column.className}>
                    <Skeleton className="h-4 w-full max-w-32" />
                  </TableCell>
                ))}
              </TableRow>
            ))
          : sortedData.map((row) => (
              <TableRow key={rowKey(row)}>
                {visibleColumns.map((column) => (
                  <TableCell key={column.id} className={column.className}>
                    {column.cell(row)}
                  </TableCell>
                ))}
              </TableRow>
            ))}
      </TableBody>
    </Table>
  ) : (
    // Virtualized/infinite modes: a native <table> can't virtualize its
    // rows cleanly, so header and body both switch to an ARIA-grid div
    // layout together rather than mixing a real table header with a div body.
    <div role="table" className="w-full overflow-hidden rounded-md border border-border text-sm">
      <div role="row" className="flex bg-muted">
        {visibleColumns.map((column) => (
          <div
            key={column.id}
            role="columnheader"
            className={cn("flex-1 px-3 py-2 text-muted-foreground", column.className)}
          >
            {renderHeaderContent(column)}
          </div>
        ))}
      </div>

      {isLoading && sortedData.length === 0 ? (
        <div>
          {Array.from({ length: SKELETON_ROWS }, (_, rowIndex) => (
            <div key={`skeleton-${rowIndex}`} role="row" className="flex items-center border-b border-border">
              {visibleColumns.map((column) => (
                <div role="cell" key={column.id} className={cn("flex-1 px-3 py-2", column.className)}>
                  <Skeleton className="h-4 w-full max-w-32" />
                </div>
              ))}
            </div>
          ))}
        </div>
      ) : (
        <DataTableVirtualBody
          data={sortedData}
          columns={visibleColumns}
          rowKey={rowKey}
          rowHeight={rowHeight}
          height={virtualHeight}
          onLoadMore={onLoadMore}
          hasMore={hasMore}
          isLoading={isLoading}
        />
      )}
    </div>
  );

  return (
    <div className="flex flex-col">
      <DataTableToolbar
        actions={actions}
        searchValue={searchValue}
        onSearchChange={onSearchChange}
        searchPlaceholder={searchPlaceholder}
        customSearch={customSearch}
        onCustomSearch={onCustomSearch}
        buttons={customSearch ? undefined : buttonsNode}
      />

      <div className="mt-2 rounded-md border border-border">
        {customSearch && hasButtons && (
          <div className="flex justify-end px-2 pt-2 pb-1">{buttonsNode}</div>
        )}
        <div className={cn("p-2", customSearch && hasButtons && "pt-1")}>{content}</div>
      </div>

      {!showError && (
        <div className="mt-2">
          {mode === "paginated" ? (
            <DataTablePagination
              pageNumber={pageNumber ?? 1}
              totalPages={totalPages ?? 1}
              totalRecords={totalRecords}
              onPageChange={onPageChange ?? (() => {})}
            />
          ) : (
            <p className="text-sm text-muted-foreground">{totalRecords} records</p>
          )}
        </div>
      )}
    </div>
  );
}
