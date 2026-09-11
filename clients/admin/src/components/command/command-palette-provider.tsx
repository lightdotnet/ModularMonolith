"use client";

import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { CommandPalette } from "@/components/command/command-palette";
import type { CommandGroup } from "@/components/command/types";

interface CommandPaletteContextValue {
  open: boolean;
  setOpen: (open: boolean) => void;
}

const CommandPaletteContext = createContext<CommandPaletteContextValue | null>(null);

export function useCommandPalette() {
  const context = useContext(CommandPaletteContext);
  if (!context) throw new Error("useCommandPalette must be used within a CommandPaletteProvider");
  return context;
}

export interface CommandPaletteProviderProps {
  groups: CommandGroup[];
  children: ReactNode;
}

/** Owns the global Cmd+K / Ctrl+K shortcut and the palette's open state. */
export function CommandPaletteProvider({ groups, children }: CommandPaletteProviderProps) {
  const [open, setOpen] = useState(false);

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if ((event.metaKey || event.ctrlKey) && event.key?.toLowerCase() === "k") {
        event.preventDefault();
        setOpen((previous) => !previous);
      }
    }

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, []);

  return (
    <CommandPaletteContext.Provider value={{ open, setOpen }}>
      {children}
      <CommandPalette open={open} onOpenChange={setOpen} groups={groups} />
    </CommandPaletteContext.Provider>
  );
}
