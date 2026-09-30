"use client";

import { useRef } from "react";
import { useVirtualizer } from "@tanstack/react-virtual";

export interface UseVirtualListOptions {
  count: number;
  estimateSize?: number;
  overscan?: number;
}

/** Thin, opinionated wrapper around @tanstack/react-virtual shared by the
 * DataTable's virtualized/infinite modes and the Command Palette. */
export function useVirtualList({ count, estimateSize = 36, overscan = 8 }: UseVirtualListOptions) {
  const scrollRef = useRef<HTMLDivElement>(null);

  const virtualizer = useVirtualizer({
    count,
    getScrollElement: () => scrollRef.current,
    estimateSize: () => estimateSize,
    overscan,
  });

  return { scrollRef, virtualizer };
}
