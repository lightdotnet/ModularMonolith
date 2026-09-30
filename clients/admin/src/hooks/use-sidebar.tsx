"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { usePathname } from "next/navigation";

const HIDDEN_KEY = "admin.sidebar.hidden";

interface SidebarContextValue {
  hidden: boolean;
  toggleSidebar: () => void;
  /** Explicit user override for a nav group's expanded state, if any — `undefined` means no override yet. */
  isExpanded: (href: string) => boolean | undefined;
  toggleExpanded: (href: string, current: boolean) => void;
  mobileOpen: boolean;
  setMobileOpen: (open: boolean) => void;
}

const SidebarContext = createContext<SidebarContextValue | null>(null);

export function SidebarProvider({ children }: { children: ReactNode }) {
  const [hidden, setHidden] = useState(false);
  // Explicit per-href expand/collapse overrides — absence means "no override
  // yet," letting a group's default (auto-expand when it contains the active
  // route) show through until the user clicks it themselves.
  const [expandedOverrides, setExpandedOverrides] = useState<Map<string, boolean>>(new Map());
  const [mobileOpen, setMobileOpen] = useState(false);
  const [hydrated, setHydrated] = useState(false);

  // Close the mobile drawer after navigating — a nav click routes but Radix's
  // Sheet only closes itself on outside-click/Escape, not on in-content navigation.
  // Adjusted during render (not an effect) per the "adjusting state when a prop
  // changes" pattern: https://react.dev/learn/you-might-not-need-an-effect
  const pathname = usePathname();
  const [prevPathname, setPrevPathname] = useState(pathname);
  if (pathname !== prevPathname) {
    setPrevPathname(pathname);
    setMobileOpen(false);
  }

  // Restore persisted state after mount — avoids a server/client markup mismatch.
  // localStorage can't be read during SSR/first render, so this can't be derived inline;
  // it's a genuine one-time sync from a browser-only store, not derivable state.
  /* eslint-disable react-hooks/set-state-in-effect */
  useEffect(() => {
    const storedHidden = localStorage.getItem(HIDDEN_KEY);
    if (storedHidden !== null) setHidden(storedHidden === "true");

    setHydrated(true);
  }, []);
  /* eslint-enable react-hooks/set-state-in-effect */

  useEffect(() => {
    if (hydrated) localStorage.setItem(HIDDEN_KEY, String(hidden));
  }, [hidden, hydrated]);

  const toggleSidebar = useCallback(() => setHidden((prev) => !prev), []);

  const isExpanded = useCallback(
    (href: string) => expandedOverrides.get(href),
    [expandedOverrides],
  );

  const toggleExpanded = useCallback(
    (href: string, current: boolean) =>
      setExpandedOverrides((prev) => {
        const next = new Map(prev);
        next.set(href, !current);
        return next;
      }),
    [],
  );

  const value = useMemo(
    () => ({
      hidden,
      toggleSidebar,
      isExpanded,
      toggleExpanded,
      mobileOpen,
      setMobileOpen,
    }),
    [hidden, toggleSidebar, isExpanded, toggleExpanded, mobileOpen],
  );

  return (
    <SidebarContext.Provider value={value}>
      {children}
    </SidebarContext.Provider>
  );
}

export function useSidebar() {
  const context = useContext(SidebarContext);
  if (!context) {
    throw new Error("useSidebar must be used within a SidebarProvider");
  }
  return context;
}
