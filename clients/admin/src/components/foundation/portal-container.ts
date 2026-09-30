"use client";

import { createContext, useContext } from "react";

/**
 * DOM node that a nested Radix portal (Popover, etc.) should render into
 * instead of `document.body`. A modal Dialog's scroll lock only recognizes
 * content that's an actual DOM descendant of its own content node as
 * "inside" — anything portaled elsewhere (Radix's default) gets its
 * wheel/touch scroll blocked as if it were page background. DialogContent
 * provides its own node here so a nested Combobox/Popover stays scrollable;
 * this resolves to `null` (Radix's own `document.body` default) outside a Dialog.
 */
const PortalContainerContext = createContext<HTMLElement | null>(null);

export const PortalContainerProvider = PortalContainerContext.Provider;

export function usePortalContainer(): HTMLElement | null {
  return useContext(PortalContainerContext);
}
