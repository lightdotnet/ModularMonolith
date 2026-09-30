"use client";

import { useEffect, useRef } from "react";
import { useListNavigation, useTypeahead, type FloatingContext } from "@floating-ui/react";

export interface UseListboxOptions {
  context: FloatingContext;
  labels: (string | null)[];
  activeIndex: number | null;
  onActiveIndexChange: (index: number | null) => void;
  selectedIndex?: number | null;
  loop?: boolean;
  /**
   * Typeahead ("type a letter to jump to a matching item") is only correct
   * for a closed, button-triggered listbox. On a real text input (e.g. the
   * Command Palette), Floating UI's useTypeahead calls preventDefault on
   * every character keydown while open — which blocks normal typing
   * entirely. Text-input consumers must pass `false` here.
   */
  enableTypeahead?: boolean;
}

/**
 * Keyboard-navigation engine for the Command Palette's result list. Uses
 * Floating UI's "virtual" focus mode: DOM focus stays on the trigger/input,
 * active state is communicated via `aria-activedescendant` (the listbox
 * pattern), not by moving real focus onto list items — which is what lets a
 * text input stay focused while arrow keys move the active option.
 */
export function useListbox({
  context,
  labels,
  activeIndex,
  onActiveIndexChange,
  selectedIndex = null,
  loop = true,
  enableTypeahead = true,
}: UseListboxOptions) {
  const listRef = useRef<Array<HTMLElement | null>>([]);
  const labelsRef = useRef<Array<string | null>>(labels);
  useEffect(() => {
    labelsRef.current = labels;
  });

  const listNavigation = useListNavigation(context, {
    listRef,
    activeIndex,
    selectedIndex,
    onNavigate: onActiveIndexChange,
    loop,
    virtual: true,
    focusItemOnHover: true,
  });

  const typeahead = useTypeahead(context, {
    listRef: labelsRef,
    activeIndex,
    onMatch: context.open ? onActiveIndexChange : undefined,
    enabled: enableTypeahead,
  });

  // Not merged into `useInteractions` here — see useFloatingPopover's
  // comment. The caller combines these with click/dismiss/role into one
  // useInteractions call.
  return { listRef, listNavigation, typeahead };
}
