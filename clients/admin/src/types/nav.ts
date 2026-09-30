import type { LucideIcon } from "lucide-react";

export interface NavItem {
  label: string;
  href: string;
  icon?: LucideIcon;
  /** Gates visibility of this item (and, if it has children, the recursion into them). */
  permission?: string;
  /** When true, the item is active only on an exact path match (not on nested routes) — for a parent whose sibling lives under its path. */
  exact?: boolean;
  children?: NavItem[];
}
