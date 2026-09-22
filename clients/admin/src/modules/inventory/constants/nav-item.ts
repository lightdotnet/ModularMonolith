import { Boxes } from "lucide-react";
import { INVENTORY_STOCK_PERMISSIONS } from "./permissions";
import type { NavItem } from "@/types/nav";

export const INVENTORY_NAV_ITEM: NavItem = {
  label: "Inventory",
  href: "/inventory",
  icon: Boxes,
  permission: INVENTORY_STOCK_PERMISSIONS.View,
  // The Valuation page lives under /inventory/valuation and has its own nav item.
  exact: true,
};
