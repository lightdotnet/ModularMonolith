"use client";

import { useEffect, useRef, useState } from "react";
import { cn } from "@/lib/shared/utils";
import {
  OBJECT_VIEWER_MAX_LABEL_WIDTH,
  OBJECT_VIEWER_MIN_LABEL_WIDTH,
  clampLabelWidth,
  useObjectViewerLayout,
} from "@/components/shared/object-viewer/object-viewer-layout-context";

const KEYBOARD_STEP = 10;
const KEYBOARD_STEP_LARGE = 20;

interface DragState {
  pointerId: number;
  startX: number;
  startWidth: number;
  lastWidth: number;
}

export function ColumnResizeHandle({ className }: { className?: string }) {
  const { labelWidth, setLabelWidth, containerRef } = useObjectViewerLayout();
  const [isDragging, setIsDragging] = useState(false);
  const dragState = useRef<DragState | null>(null);

  function handlePointerDown(event: React.PointerEvent<HTMLDivElement>) {
    event.preventDefault();
    dragState.current = {
      pointerId: event.pointerId,
      startX: event.clientX,
      startWidth: labelWidth,
      lastWidth: labelWidth,
    };
    setIsDragging(true);
  }

  useEffect(() => {
    if (!isDragging) {
      return;
    }

    const container = containerRef.current;

    function finishDrag(pointerId: number) {
      const state = dragState.current;
      if (!state || state.pointerId !== pointerId) {
        return;
      }

      dragState.current = null;
      setIsDragging(false);
      setLabelWidth(state.lastWidth);
    }

    function handlePointerMove(moveEvent: PointerEvent) {
      const state = dragState.current;
      if (!state || moveEvent.pointerId !== state.pointerId) {
        return;
      }

      const delta = moveEvent.clientX - state.startX;
      const nextWidth = clampLabelWidth(state.startWidth + delta);
      state.lastWidth = nextWidth;
      container?.style.setProperty("--ov-label-w", `${nextWidth}px`);
    }

    function handlePointerUp(upEvent: PointerEvent) {
      finishDrag(upEvent.pointerId);
    }

    function handlePointerCancel(cancelEvent: PointerEvent) {
      finishDrag(cancelEvent.pointerId);
    }

    document.addEventListener("pointermove", handlePointerMove);
    document.addEventListener("pointerup", handlePointerUp);
    document.addEventListener("pointercancel", handlePointerCancel);

    return () => {
      document.removeEventListener("pointermove", handlePointerMove);
      document.removeEventListener("pointerup", handlePointerUp);
      document.removeEventListener("pointercancel", handlePointerCancel);

      // If the component unmounts mid-drag, make sure the DOM isn't left
      // showing an uncommitted width that React's state no longer reflects.
      const state = dragState.current;
      if (state) {
        dragState.current = null;
        container?.style.setProperty("--ov-label-w", `${state.startWidth}px`);
      }
    };
  }, [isDragging, containerRef, setLabelWidth]);

  function handleKeyDown(event: React.KeyboardEvent<HTMLDivElement>) {
    const step = event.shiftKey ? KEYBOARD_STEP_LARGE : KEYBOARD_STEP;

    switch (event.key) {
      case "ArrowLeft":
        event.preventDefault();
        setLabelWidth(clampLabelWidth(labelWidth - step));
        break;
      case "ArrowRight":
        event.preventDefault();
        setLabelWidth(clampLabelWidth(labelWidth + step));
        break;
      case "Home":
        event.preventDefault();
        setLabelWidth(OBJECT_VIEWER_MIN_LABEL_WIDTH);
        break;
      case "End":
        event.preventDefault();
        setLabelWidth(OBJECT_VIEWER_MAX_LABEL_WIDTH);
        break;
      default:
        break;
    }
  }

  return (
    <div
      aria-label="Resize label column"
      aria-orientation="vertical"
      aria-valuemax={OBJECT_VIEWER_MAX_LABEL_WIDTH}
      aria-valuemin={OBJECT_VIEWER_MIN_LABEL_WIDTH}
      aria-valuenow={labelWidth}
      className={cn(
        "group absolute top-0 bottom-0 z-10 hidden w-3 -translate-x-1/2 cursor-col-resize touch-none select-none items-center justify-center outline-none focus-visible:ring-2 focus-visible:ring-ring/50 sm:flex",
        className,
      )}
      onKeyDown={handleKeyDown}
      onPointerDown={handlePointerDown}
      role="separator"
      style={{ left: "var(--ov-label-w)" }}
      tabIndex={0}
    >
      <div className="h-full w-px bg-border transition-colors group-hover:bg-muted-foreground" />
    </div>
  );
}
