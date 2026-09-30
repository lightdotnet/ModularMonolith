"use client";

import { useDeferredValue, useId, useMemo, useState, type KeyboardEvent } from "react";
import { Search } from "lucide-react";
import { useDismiss, useFloating, useInteractions, useRole } from "@floating-ui/react";
import { cn } from "@/lib/shared/utils";
import { FloatingModalOverlay } from "@/components/foundation/floating-overlay";
import { useListbox } from "@/components/foundation/use-listbox";
import { useVirtualList } from "@/components/foundation/use-virtual-list";
import type { CommandGroup, CommandItem } from "@/components/command/types";

const VIRTUALIZE_THRESHOLD = 50;
const ROW_HEIGHT = 44;

export interface CommandPaletteProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  groups: CommandGroup[];
  placeholder?: string;
  emptyMessage?: string;
}

function matches(item: CommandItem, query: string) {
  const haystack = `${item.label} ${item.description ?? ""} ${(item.keywords ?? []).join(" ")}`.toLowerCase();
  return haystack.includes(query.trim().toLowerCase());
}

/**
 * Cmd+K / Ctrl+K global overlay. Built directly on Floating UI rather than
 * the Radix `Dialog` used elsewhere in `ui/` — see
 * components/foundation/floating-overlay.tsx for why (no body scroll lock,
 * one focus/keyboard system shared with the result listbox).
 */
export function CommandPalette({
  open,
  onOpenChange,
  groups,
  placeholder = "Type a command or search...",
  emptyMessage = "No results found.",
}: CommandPaletteProps) {
  const listboxId = useId();
  const [query, setQuery] = useState("");
  const deferredQuery = useDeferredValue(query);
  const [activeIndex, setActiveIndex] = useState<number | null>(0);

  // Reset when `open` flips to false — adjusting state during render
  // (matching DataTablePagination's pattern elsewhere in this codebase)
  // rather than in an effect, since this is "state derived from a prop
  // change," not a sync with an external system.
  const [wasOpen, setWasOpen] = useState(open);
  if (open !== wasOpen) {
    setWasOpen(open);
    if (!open) {
      setQuery("");
      setActiveIndex(0);
    }
  }

  const filteredGroups = useMemo(
    () =>
      groups
        .map((group) => ({
          ...group,
          items: group.items.filter((item) => matches(item, deferredQuery)),
        }))
        .filter((group) => group.items.length > 0),
    [groups, deferredQuery],
  );

  const flatItems = useMemo(() => filteredGroups.flatMap((group) => group.items), [filteredGroups]);
  const safeActiveIndex =
    activeIndex !== null && activeIndex < flatItems.length ? activeIndex : flatItems.length > 0 ? 0 : null;

  const floating = useFloating({ open, onOpenChange });
  const dismiss = useDismiss(floating.context);
  const role = useRole(floating.context, { role: "dialog" });

  // enableTypeahead: false — this is a real text input; the query already
  // filters as-you-type, and useTypeahead would otherwise preventDefault
  // every character keydown while open, blocking typing entirely.
  const { listRef, listNavigation, typeahead } = useListbox({
    context: floating.context,
    labels: flatItems.map((item) => item.label),
    activeIndex: safeActiveIndex,
    onActiveIndexChange: setActiveIndex,
    selectedIndex: null,
    enableTypeahead: false,
  });

  const { getFloatingProps, getItemProps } = useInteractions([dismiss, role, listNavigation, typeahead]);

  const shouldVirtualize = flatItems.length > VIRTUALIZE_THRESHOLD;
  const { scrollRef, virtualizer } = useVirtualList({
    count: shouldVirtualize ? flatItems.length : 0,
    estimateSize: ROW_HEIGHT,
  });

  function runItem(item: CommandItem) {
    if (item.disabled) return;
    onOpenChange(false);
    item.onSelect();
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key !== "Enter") return;
    event.preventDefault();
    if (safeActiveIndex === null) return;
    const item = flatItems[safeActiveIndex];
    if (item) runItem(item);
  }

  if (!open) return null;

  function renderItem(item: CommandItem, index: number) {
    const Icon = item.icon;
    const active = safeActiveIndex === index;
    return (
      <div
        key={item.id}
        id={`${listboxId}-option-${index}`}
        role="option"
        aria-selected={active}
        aria-disabled={item.disabled || undefined}
        aria-setsize={flatItems.length}
        aria-posinset={index + 1}
        data-active={active || undefined}
        ref={(node) => {
          listRef.current[index] = node;
        }}
        className={cn(
          "flex cursor-pointer items-center gap-2 rounded-md px-3 py-2 text-sm outline-hidden select-none",
          "data-[active]:bg-accent data-[active]:text-accent-foreground",
          item.disabled && "pointer-events-none opacity-50",
        )}
        {...getItemProps({ onClick: () => runItem(item) })}
      >
        {Icon && <Icon className="size-4 shrink-0 text-muted-foreground" />}
        <div className="flex min-w-0 flex-1 flex-col">
          <span className="truncate">{item.label}</span>
          {item.description && (
            <span className="truncate text-xs text-muted-foreground">{item.description}</span>
          )}
        </div>
        {item.shortcut && <span className="text-xs text-muted-foreground">{item.shortcut}</span>}
      </div>
    );
  }

  return (
    <FloatingModalOverlay context={floating.context} initialFocus={0}>
      <div
        ref={floating.refs.setFloating}
        {...getFloatingProps()}
        className="flex w-full max-w-lg flex-col overflow-hidden rounded-xl bg-popover text-popover-foreground shadow-2xl ring-1 ring-foreground/10"
      >
        <div className="flex items-center gap-2 border-b border-border px-3">
          <Search className="size-4 shrink-0 text-muted-foreground" />
          <input
            autoFocus
            role="combobox"
            aria-expanded="true"
            aria-controls={listboxId}
            aria-activedescendant={
              safeActiveIndex !== null ? `${listboxId}-option-${safeActiveIndex}` : undefined
            }
            value={query}
            onChange={(event) => {
              setQuery(event.target.value);
              setActiveIndex(0);
            }}
            onKeyDown={handleKeyDown}
            placeholder={placeholder}
            className="h-11 w-full border-0 bg-transparent text-sm outline-none placeholder:text-muted-foreground"
          />
        </div>

        <div
          ref={shouldVirtualize ? scrollRef : undefined}
          id={listboxId}
          role="listbox"
          className="max-h-96 overflow-y-auto p-2"
        >
          {flatItems.length === 0 ? (
            <div className="px-3 py-6 text-center text-sm text-muted-foreground">{emptyMessage}</div>
          ) : shouldVirtualize ? (
            <div style={{ height: virtualizer.getTotalSize(), position: "relative", width: "100%" }}>
              {virtualizer.getVirtualItems().map((virtualRow) => (
                <div
                  key={virtualRow.key}
                  ref={virtualizer.measureElement}
                  data-index={virtualRow.index}
                  style={{
                    position: "absolute",
                    top: 0,
                    left: 0,
                    width: "100%",
                    transform: `translateY(${virtualRow.start}px)`,
                  }}
                >
                  {renderItem(flatItems[virtualRow.index], virtualRow.index)}
                </div>
              ))}
            </div>
          ) : (
            filteredGroups.map((group, groupIndex) => {
              const startIndex = filteredGroups
                .slice(0, groupIndex)
                .reduce((sum, previousGroup) => sum + previousGroup.items.length, 0);
              return (
                <div key={group.heading} className="mb-2 last:mb-0">
                  <div className="px-3 py-1 text-xs font-medium text-muted-foreground">
                    {group.heading}
                  </div>
                  {group.items.map((item, index) => renderItem(item, startIndex + index))}
                </div>
              );
            })
          )}
        </div>
      </div>
    </FloatingModalOverlay>
  );
}
