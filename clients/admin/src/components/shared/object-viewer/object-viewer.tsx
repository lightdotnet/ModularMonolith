"use client";

import type { CSSProperties, ReactNode } from "react";
import { useId, useMemo, useRef, useState } from "react";
import {
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { cn } from "@/lib/shared/utils";
import { ColumnResizeHandle } from "@/components/shared/object-viewer/column-resize-handle";
import {
  OBJECT_VIEWER_DEFAULT_LABEL_WIDTH,
  ObjectViewerLayoutContext,
} from "@/components/shared/object-viewer/object-viewer-layout-context";
import { ObjectViewerRow } from "@/components/shared/object-viewer/object-viewer-row";
import { formatPrimitiveValue, isPlainObject } from "@/components/shared/object-viewer/utils";

type Container = Record<string, unknown> | unknown[];

interface ObjectViewerProps {
  data: unknown;
  className?: string;
}

export function ObjectViewer({ data, className }: ObjectViewerProps) {
  const [labelWidth, setLabelWidth] = useState(OBJECT_VIEWER_DEFAULT_LABEL_WIDTH);
  const containerRef = useRef<HTMLDivElement | null>(null);

  const contextValue = useMemo(
    () => ({ labelWidth, setLabelWidth, containerRef }),
    [labelWidth],
  );

  const style = { "--ov-label-w": `${labelWidth}px` } as CSSProperties;

  return (
    <ObjectViewerLayoutContext.Provider value={contextValue}>
      <div className={cn("relative space-y-2", className)} ref={containerRef} style={style}>
        <ColumnResizeHandle />
        {renderRoot(data)}
      </div>
    </ObjectViewerLayoutContext.Provider>
  );
}

function renderRoot(data: unknown): ReactNode {
  if (data === null || data === undefined) {
    return <ObjectViewerRow label="value" value="-" />;
  }

  if (Array.isArray(data) || isPlainObject(data)) {
    if (isEmptyContainer(data)) {
      return (
        <ObjectViewerRow
          label="value"
          value={Array.isArray(data) ? "(empty array)" : "(empty)"}
        />
      );
    }

    return renderEntries(data, []);
  }

  return <ObjectViewerRow label="value" value={formatPrimitiveValue(data)} />;
}

function isEmptyContainer(container: Container): boolean {
  return Array.isArray(container)
    ? container.length === 0
    : Object.keys(container).length === 0;
}

function isListOfPlainObjects(value: unknown[]): boolean {
  return (
    value.some((item) => isPlainObject(item)) &&
    value.every((item) => item === null || item === undefined || isPlainObject(item))
  );
}

interface Entry {
  key: string;
  label: string | undefined;
  value: unknown;
}

function toEntries(container: Container): Entry[] {
  return Array.isArray(container)
    ? container.map((value, index) => ({ key: String(index), label: undefined, value }))
    : Object.entries(container).map(([key, value]) => ({ key, label: key, value }));
}

function renderEntries(container: Container, ancestors: object[]): ReactNode {
  return (
    <>
      {toEntries(container).map((entry) => (
        <ObjectViewerField
          ancestors={ancestors}
          key={entry.key}
          label={entry.label}
          value={entry.value}
        />
      ))}
    </>
  );
}

function ObjectViewerField({
  label,
  value,
  ancestors,
}: {
  label?: string;
  value: unknown;
  ancestors: object[];
}) {
  const isListItem = label === undefined;

  if (value === null || value === undefined) {
    return withListItemCard(isListItem, <ObjectViewerRow label={label} value="-" />);
  }

  if (Array.isArray(value) || isPlainObject(value)) {
    if (ancestors.includes(value)) {
      return withListItemCard(
        isListItem,
        <ObjectViewerRow label={label} value="[Circular]" />,
      );
    }

    if (isEmptyContainer(value)) {
      return withListItemCard(
        isListItem,
        <ObjectViewerRow
          label={label}
          value={Array.isArray(value) ? "(empty array)" : "(empty)"}
        />,
      );
    }

    if (Array.isArray(value) && isListOfPlainObjects(value)) {
      return withListItemCard(
        isListItem,
        <ObjectViewerTable ancestors={ancestors} items={value} label={label} />,
      );
    }

    return (
      <ObjectViewerGroup isList={Array.isArray(value)} label={label}>
        {renderEntries(value, [...ancestors, value])}
      </ObjectViewerGroup>
    );
  }

  return withListItemCard(
    isListItem,
    <ObjectViewerRow label={label} value={formatPrimitiveValue(value)} />,
  );
}

function withListItemCard(isListItem: boolean, row: ReactNode): ReactNode {
  if (!isListItem) {
    return row;
  }

  return <div className="rounded-xl border bg-muted/40 p-3">{row}</div>;
}

function ObjectViewerGroup({
  label,
  isList,
  children,
}: {
  label?: string;
  isList: boolean;
  children: ReactNode;
}) {
  if (isList) {
    return (
      <div className="space-y-2">
        {label ? (
          <span className="text-sm font-semibold text-foreground">{label}</span>
        ) : null}
        <div className="space-y-2">{children}</div>
      </div>
    );
  }

  return (
    <div className="space-y-2 rounded-xl border bg-muted/40 p-3">
      {label ? (
        <span className="text-sm font-semibold text-foreground">{label}</span>
      ) : null}
      <div className={cn("space-y-2", label && "border-l pl-4")}>{children}</div>
    </div>
  );
}

function ObjectViewerTable({
  items,
  ancestors,
  label,
}: {
  items: unknown[];
  ancestors: object[];
  label?: string;
}) {
  const labelId = useId();

  const columns = useMemo(() => {
    const seen = new Set<string>();
    const orderedColumns: string[] = [];

    for (const item of items) {
      if (!isPlainObject(item)) {
        continue;
      }

      for (const key of Object.keys(item)) {
        if (!seen.has(key)) {
          seen.add(key);
          orderedColumns.push(key);
        }
      }
    }

    return orderedColumns;
  }, [items]);

  return (
    <div
      aria-label={label ? undefined : "Scrollable table"}
      aria-labelledby={label ? labelId : undefined}
      className="relative z-20 rounded-xl border bg-card"
      role="region"
      tabIndex={0}
    >
      <Table>
        {label ? (
          <TableCaption className="px-4 pt-2 text-left text-sm font-semibold text-foreground" id={labelId}>
            {label}
          </TableCaption>
        ) : null}
        <TableHeader className="bg-muted">
          <TableRow>
            {columns.map((column) => (
              <TableHead
                key={column}
                className="text-xs font-semibold tracking-wide text-muted-foreground uppercase"
                scope="col"
              >
                {column}
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {items.map((item, index) => {
            const itemAncestors = isPlainObject(item) ? [...ancestors, item] : ancestors;

            return (
              <TableRow key={index}>
                {columns.map((column) => {
                  const cellValue = isPlainObject(item) ? item[column] : undefined;
                  const formatted = formatCellValue(cellValue, itemAncestors);

                  return (
                    <TableCell
                      className="max-w-[320px] truncate"
                      key={column}
                      title={formatCellTitle(cellValue, formatted)}
                    >
                      {formatted}
                    </TableCell>
                  );
                })}
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
}

function formatCellValue(value: unknown, ancestors: object[]): string {
  if (value === null || value === undefined) {
    return "-";
  }

  if (typeof value === "object" && ancestors.includes(value)) {
    return "[Circular]";
  }

  if (Array.isArray(value)) {
    return `(${value.length} items)`;
  }

  if (isPlainObject(value)) {
    return "(object)";
  }

  return formatPrimitiveValue(value);
}

function formatCellTitle(value: unknown, formatted: string): string {
  if (Array.isArray(value) || isPlainObject(value)) {
    try {
      return JSON.stringify(value, null, 2);
    } catch {
      return formatted;
    }
  }

  return formatted;
}
