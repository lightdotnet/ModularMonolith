"use client";

import { useEffect, useState } from "react";
import { Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import type { DataTableAction } from "@/components/shared/data-table/types";

const SEARCH_DEBOUNCE_MS = 400;

interface DataTableToolbarProps {
  actions?: DataTableAction[];
  searchValue?: string;
  onSearchChange?: (value: string) => void;
  searchPlaceholder?: string;
  /** Custom multi-field filter UI rendered in the search row instead of the built-in text search. */
  customSearch?: React.ReactNode;
  /** When set alongside `customSearch`, renders a "Search" button that applies the custom filters on click rather than as each field changes. */
  onCustomSearch?: () => void;
  /**
   * The datatable buttons (export/refresh/columns), rendered by the caller.
   * Only shown here — inline with the search row — when `customSearch` isn't
   * used; a `customSearch` layout renders these itself alongside the table instead.
   */
  buttons?: React.ReactNode;
}

export function DataTableToolbar({
  actions,
  searchValue,
  onSearchChange,
  searchPlaceholder = "Search...",
  customSearch,
  onCustomSearch,
  buttons,
}: DataTableToolbarProps) {
  const [searchText, setSearchText] = useState(() => searchValue ?? "");
  const [lastSearchValue, setLastSearchValue] = useState(searchValue ?? "");

  if ((searchValue ?? "") !== lastSearchValue) {
    setLastSearchValue(searchValue ?? "");
    setSearchText(searchValue ?? "");
  }

  useEffect(() => {
    if (!onSearchChange) return;
    if (searchText === (searchValue ?? "")) return;

    const timeout = setTimeout(() => onSearchChange(searchText), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timeout);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- only re-debounce on searchText changes
  }, [searchText]);

  const actionsSection = actions && actions.length > 0 && (
    <div className="flex flex-wrap items-center gap-2">
      {actions.map((action) => {
        const Icon = action.icon;
        return (
          <Button key={action.key} variant={action.variant} onClick={action.onClick}>
            {Icon && <Icon data-icon="inline-start" />}
            {action.label}
          </Button>
        );
      })}
    </div>
  );

  const searchSection = customSearch ? (
    <div className="flex flex-col gap-2">
      <div className="flex flex-wrap items-center gap-2">{customSearch}</div>
      {onCustomSearch && (
        <div>
          <Button variant="outline" size="sm" onClick={onCustomSearch}>
            <Search data-icon="inline-start" />
            Search
          </Button>
        </div>
      )}
    </div>
  ) : (
    (onSearchChange || buttons) && (
      <div className="flex flex-wrap items-center justify-between gap-2">
        {onSearchChange ? (
          <label className="relative w-full max-w-sm">
            <span className="sr-only">Search</span>
            <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
            <input
              type="search"
              value={searchText}
              onChange={(event) => setSearchText(event.target.value)}
              placeholder={searchPlaceholder}
              className="h-9 w-full rounded-md border border-input bg-background py-1.5 pr-3 pl-9 text-sm outline-none transition-colors duration-fast ease-standard placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-2 focus-visible:ring-ring/50"
            />
          </label>
        ) : (
          <div />
        )}
        {buttons}
      </div>
    )
  );

  const sections = [actionsSection, searchSection].filter(Boolean);

  return (
    <div className="flex flex-col gap-3">
      {sections.map((section, index) => (
        <div key={index} className="flex flex-col gap-3">
          {index > 0 && <Separator />}
          {section}
        </div>
      ))}
    </div>
  );
}
