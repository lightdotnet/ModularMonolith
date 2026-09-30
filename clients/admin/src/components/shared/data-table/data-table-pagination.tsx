"use client";

import { useState } from "react";
import { Input } from "@/components/ui/input";
import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";

/**
 * Always keeps page 1 and the last page visible, plus up to `siblingCount`
 * pages on each side of `current`, with an "ellipsis" marker over any gap.
 */
export function getPageWindow(
  current: number,
  total: number,
  siblingCount = 3,
): (number | "ellipsis")[] {
  if (total <= 0) return [];

  const pages = new Set<number>([1, total]);
  for (let i = current - siblingCount; i <= current + siblingCount; i++) {
    if (i >= 1 && i <= total) pages.add(i);
  }

  const sorted = Array.from(pages).sort((a, b) => a - b);
  const result: (number | "ellipsis")[] = [];
  sorted.forEach((page, index) => {
    if (index > 0 && page - sorted[index - 1] > 1) {
      result.push("ellipsis");
    }
    result.push(page);
  });

  return result;
}

interface DataTablePaginationProps {
  pageNumber: number;
  totalPages: number;
  totalRecords: number;
  onPageChange: (page: number) => void;
}

export function DataTablePagination({
  pageNumber,
  totalPages,
  totalRecords,
  onPageChange,
}: DataTablePaginationProps) {
  const [pageInput, setPageInput] = useState(() => String(pageNumber));
  const [lastPageNumber, setLastPageNumber] = useState(pageNumber);

  if (pageNumber !== lastPageNumber) {
    setLastPageNumber(pageNumber);
    setPageInput(String(pageNumber));
  }

  function commitPageInput() {
    const parsed = Number(pageInput);
    const clamped = Number.isFinite(parsed)
      ? Math.min(Math.max(Math.trunc(parsed), 1), Math.max(totalPages, 1))
      : pageNumber;

    setPageInput(String(clamped));
    if (clamped !== pageNumber) onPageChange(clamped);
  }

  const pageWindow = getPageWindow(pageNumber, totalPages);

  return (
    <div className="flex flex-col items-center justify-between gap-4 sm:flex-row">
      <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
        <Input
          type="number"
          min={1}
          max={Math.max(totalPages, 1)}
          value={pageInput}
          onChange={(event) => setPageInput(event.target.value)}
          onBlur={commitPageInput}
          onKeyDown={(event) => {
            if (event.key === "Enter") commitPageInput();
          }}
          className="h-7 w-10 field-sizing-content px-1 text-center"
        />
        <span>
          of <strong className="font-semibold text-foreground">{Math.max(totalPages, 1)}</strong>
        </span>
        <span aria-hidden="true" className="size-1 shrink-0 rounded-full bg-muted-foreground" />
        <span>{totalRecords} records</span>
      </div>

      {totalPages > 1 && (
        <Pagination className="mx-0 w-auto justify-end">
          <PaginationContent>
            <PaginationItem>
              <PaginationPrevious
                href="#"
                text=""
                size="icon"
                onClick={(event) => {
                  event.preventDefault();
                  if (pageNumber > 1) onPageChange(pageNumber - 1);
                }}
                className={pageNumber === 1 ? "pointer-events-none opacity-50" : undefined}
              />
            </PaginationItem>
            {pageWindow.map((page, index) =>
              page === "ellipsis" ? (
                <PaginationItem key={`ellipsis-${index}`}>
                  <PaginationEllipsis />
                </PaginationItem>
              ) : (
                <PaginationItem key={page}>
                  <PaginationLink
                    href="#"
                    isActive={page === pageNumber}
                    onClick={(event) => {
                      event.preventDefault();
                      onPageChange(page);
                    }}
                  >
                    {page}
                  </PaginationLink>
                </PaginationItem>
              ),
            )}
            <PaginationItem>
              <PaginationNext
                href="#"
                text=""
                size="icon"
                onClick={(event) => {
                  event.preventDefault();
                  if (pageNumber < totalPages) onPageChange(pageNumber + 1);
                }}
                className={
                  pageNumber === totalPages ? "pointer-events-none opacity-50" : undefined
                }
              />
            </PaginationItem>
          </PaginationContent>
        </Pagination>
      )}
    </div>
  );
}
