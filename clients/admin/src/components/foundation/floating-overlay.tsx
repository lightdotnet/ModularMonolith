"use client";

import type { ReactElement, RefObject } from "react";
import {
  FloatingFocusManager,
  FloatingOverlay as FloatingUiOverlay,
  FloatingPortal,
  type FloatingContext,
} from "@floating-ui/react";
import { cn } from "@/lib/shared/utils";

export interface FloatingModalOverlayProps {
  context: FloatingContext;
  children: ReactElement;
  className?: string;
  initialFocus?: RefObject<HTMLElement | null> | number;
}

/**
 * Backdrop + focus trap for the Command Palette, built directly on Floating
 * UI rather than the Radix `Dialog` the rest of `ui/` uses — Radix's Dialog
 * locks body scroll by default, which conflicts with this repo's explicit
 * "no body scroll locking" requirement for the new overlay components.
 * `lockScroll={false}` below is the default; kept explicit as documentation.
 */
export function FloatingModalOverlay({
  context,
  children,
  className,
  initialFocus,
}: FloatingModalOverlayProps) {
  if (!context.open) return null;

  return (
    <FloatingPortal>
      <FloatingUiOverlay
        lockScroll={false}
        className={cn(
          "fixed inset-0 z-50 flex items-start justify-center bg-black/50 p-4 pt-[10vh]",
          className,
        )}
      >
        <FloatingFocusManager context={context} initialFocus={initialFocus}>
          {children}
        </FloatingFocusManager>
      </FloatingUiOverlay>
    </FloatingPortal>
  );
}
