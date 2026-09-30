"use client";

import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";

export const ACCENT_COLORS = [
  { value: "green", label: "Green" },
  { value: "blue", label: "Blue" },
  { value: "violet", label: "Violet" },
  { value: "rose", label: "Rose" },
  { value: "orange", label: "Orange" },
  { value: "amber", label: "Amber" },
  { value: "black", label: "Black" },
] as const;

export type AccentColor = (typeof ACCENT_COLORS)[number]["value"];

const STORAGE_KEY = "admin.accent-color";
const DEFAULT_ACCENT: AccentColor = "green";

interface AccentColorContextValue {
  accentColor: AccentColor;
  setAccentColor: (color: AccentColor) => void;
}

const AccentColorContext = createContext<AccentColorContextValue | null>(null);

function isAccentColor(value: string): value is AccentColor {
  return ACCENT_COLORS.some((accent) => accent.value === value);
}

export function AccentColorProvider({ children }: { children: ReactNode }) {
  const [accentColor, setAccentColor] = useState<AccentColor>(DEFAULT_ACCENT);
  const [hydrated, setHydrated] = useState(false);

  // Restore persisted state after mount — localStorage can't be read during
  // SSR/first render, so this is a genuine one-time browser-store sync.
  /* eslint-disable react-hooks/set-state-in-effect */
  useEffect(() => {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored && isAccentColor(stored)) setAccentColor(stored);
    setHydrated(true);
  }, []);
  /* eslint-enable react-hooks/set-state-in-effect */

  useEffect(() => {
    document.documentElement.setAttribute("data-accent", accentColor);
    if (hydrated) localStorage.setItem(STORAGE_KEY, accentColor);
  }, [accentColor, hydrated]);

  return (
    <AccentColorContext.Provider value={{ accentColor, setAccentColor }}>
      {children}
    </AccentColorContext.Provider>
  );
}

export function useAccentColor() {
  const context = useContext(AccentColorContext);
  if (!context) {
    throw new Error(
      "useAccentColor must be used within an AccentColorProvider",
    );
  }
  return context;
}
