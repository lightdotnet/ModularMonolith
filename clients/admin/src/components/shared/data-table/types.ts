import type { LucideIcon } from "lucide-react";
import type { Button } from "@/components/ui/button";

export interface DataTableColumn<TData> {
  id: string;
  header: React.ReactNode;
  cell: (row: TData) => React.ReactNode;
  /** Applied to both the `th` and `td` for this column. */
  className?: string;
  /** Whether this column can be hidden via the columns menu. Defaults to `true`. */
  hideable?: boolean;
  /**
   * Enables client-side sorting on this column. Only sorts the `data` array
   * that was passed in, so only meaningful when the caller already holds the
   * full result set (no server-side pagination) — requires `sortValue`.
   */
  sortable?: boolean;
  /** Comparable value used to sort this column when `sortable` is set. */
  sortValue?: (row: TData) => string | number;
}

export interface DataTableAction {
  key: string;
  label: string;
  icon?: LucideIcon;
  onClick: () => void;
  variant?: React.ComponentProps<typeof Button>["variant"];
}

export interface DataTableEmptyState {
  icon?: LucideIcon;
  title: string;
  description?: string;
}

export interface DataTableErrorState {
  title: string;
  description?: string;
}
