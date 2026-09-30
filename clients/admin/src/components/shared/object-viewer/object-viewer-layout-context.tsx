"use client";

import { createContext, useContext, type RefObject } from "react";

export const OBJECT_VIEWER_DEFAULT_LABEL_WIDTH = 180;
export const OBJECT_VIEWER_MIN_LABEL_WIDTH = 100;
export const OBJECT_VIEWER_MAX_LABEL_WIDTH = 420;

export function clampLabelWidth(value: number): number {
  return Math.min(
    Math.max(value, OBJECT_VIEWER_MIN_LABEL_WIDTH),
    OBJECT_VIEWER_MAX_LABEL_WIDTH,
  );
}

interface ObjectViewerLayoutContextValue {
  labelWidth: number;
  setLabelWidth: (width: number) => void;
  containerRef: RefObject<HTMLDivElement | null>;
}

const defaultContainerRef: RefObject<HTMLDivElement | null> = { current: null };

export const ObjectViewerLayoutContext = createContext<ObjectViewerLayoutContextValue>({
  labelWidth: OBJECT_VIEWER_DEFAULT_LABEL_WIDTH,
  setLabelWidth: () => {},
  containerRef: defaultContainerRef,
});

export function useObjectViewerLayout() {
  return useContext(ObjectViewerLayoutContext);
}
