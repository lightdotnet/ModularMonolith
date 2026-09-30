import type { LucideIcon } from "lucide-react";

export interface CommandItem {
  id: string;
  label: string;
  description?: string;
  icon?: LucideIcon;
  shortcut?: string;
  /** Extra terms matched against the query but not shown in the row. */
  keywords?: string[];
  disabled?: boolean;
  onSelect: () => void;
}

export interface CommandGroup {
  heading: string;
  items: CommandItem[];
}
