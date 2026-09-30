"use client";

import { useEffect, useRef, type ReactNode } from "react";
import { cn } from "@/lib/shared/utils";
import { useVirtualList } from "@/components/foundation/use-virtual-list";
import type { DataTableColumn } from "@/components/shared/data-table/types";

const LOAD_MORE_THRESHOLD = 5;

export interface DataTableVirtualBodyProps<TData> {
  data: TData[];
  columns: DataTableColumn<TData>[];
  rowKey: (row: TData) => string;
  rowHeight?: number;
  height?: number;
  onLoadMore?: () => void;
  hasMore?: boolean;
  isLoading?: boolean;
}

/**
 * Row renderer for DataTable's `"virtualized"`/`"infinite"` modes. Native
 * `<table>` markup can't virtualize cleanly (browsers require the full row
 * set for correct table layout), so these modes switch to an ARIA-grid div
 * layout instead — this is that body; the header switches alongside it in
 * data-table.tsx so markup stays consistent within one mode.
 */
export function DataTableVirtualBody<TData>({
  data,
  columns,
  rowKey,
  rowHeight = 40,
  height = 480,
  onLoadMore,
  hasMore,
  isLoading,
}: DataTableVirtualBodyProps<TData>): ReactNode {
  const { scrollRef, virtualizer } = useVirtualList({ count: data.length, estimateSize: rowHeight });
  const hasRequestedMore = useRef(false);

  const virtualItems = virtualizer.getVirtualItems();
  const lastItem = virtualItems[virtualItems.length - 1];

  useEffect(() => {
    hasRequestedMore.current = false;
  }, [data.length]);

  useEffect(() => {
    if (!onLoadMore || !hasMore || isLoading || !lastItem) return;
    if (hasRequestedMore.current) return;
    if (lastItem.index >= data.length - 1 - LOAD_MORE_THRESHOLD) {
      hasRequestedMore.current = true;
      onLoadMore();
    }
  }, [lastItem, data.length, hasMore, isLoading, onLoadMore]);

  return (
    <div ref={scrollRef} role="rowgroup" style={{ height }} className="overflow-y-auto">
      <div style={{ height: virtualizer.getTotalSize(), position: "relative", width: "100%" }}>
        {virtualItems.map((virtualRow) => {
          const row = data[virtualRow.index];
          return (
            <div
              key={rowKey(row)}
              role="row"
              aria-rowindex={virtualRow.index + 1}
              ref={virtualizer.measureElement}
              data-index={virtualRow.index}
              style={{
                position: "absolute",
                top: 0,
                left: 0,
                width: "100%",
                transform: `translateY(${virtualRow.start}px)`,
              }}
              className="flex items-center border-b border-border text-sm"
            >
              {columns.map((column) => (
                <div role="cell" key={column.id} className={cn("flex-1 truncate px-3 py-2", column.className)}>
                  {column.cell(row)}
                </div>
              ))}
            </div>
          );
        })}
      </div>
      {isLoading && hasMore && (
        <div className="py-3 text-center text-sm text-muted-foreground">Loading more...</div>
      )}
    </div>
  );
}
