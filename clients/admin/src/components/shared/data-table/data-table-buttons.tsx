"use client";

import { Download, RefreshCw, SlidersHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import { ButtonGroup } from "@/components/ui/button-group";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/shared/utils";
import type { DataTableColumn } from "@/components/shared/data-table/types";

interface DataTableButtonsProps<TData> {
  columns: DataTableColumn<TData>[];
  hiddenColumnIds: Set<string>;
  onToggleColumn: (columnId: string) => void;
  onExport?: () => void;
  onRefresh?: () => void;
  isLoading?: boolean;
}

export function DataTableButtons<TData>({
  columns,
  hiddenColumnIds,
  onToggleColumn,
  onExport,
  onRefresh,
  isLoading,
}: DataTableButtonsProps<TData>) {
  const hideableColumns = columns.filter((column) => column.hideable !== false);

  if (!onExport && !onRefresh && hideableColumns.length === 0) return null;

  return (
    <ButtonGroup>
      {onExport && (
        <Button variant="outline" size="sm" onClick={onExport}>
          <Download data-icon="inline-start" />
          Export
        </Button>
      )}
      {onRefresh && (
        <Button variant="outline" size="sm" onClick={onRefresh} disabled={isLoading}>
          <RefreshCw data-icon="inline-start" className={cn(isLoading && "animate-spin")} />
          Refresh
        </Button>
      )}
      {hideableColumns.length > 0 && (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="sm">
              <SlidersHorizontal data-icon="inline-start" />
              Columns
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuLabel>Toggle columns</DropdownMenuLabel>
            <DropdownMenuSeparator />
            {hideableColumns.map((column) => (
              <DropdownMenuCheckboxItem
                key={column.id}
                checked={!hiddenColumnIds.has(column.id)}
                onCheckedChange={() => onToggleColumn(column.id)}
                onSelect={(event) => event.preventDefault()}
                className={cn(hiddenColumnIds.has(column.id) && "opacity-50")}
              >
                {column.header}
              </DropdownMenuCheckboxItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
      )}
    </ButtonGroup>
  );
}
