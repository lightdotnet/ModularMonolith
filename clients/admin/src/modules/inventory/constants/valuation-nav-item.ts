import { Coins } from "lucide-react";
import { INVENTORY_STOCK_PERMISSIONS } from "./permissions";
import type { NavItem } from "@/types/nav";

export const INVENTORY_VALUATION_NAV_ITEM: NavItem = {
  label: "Valuation",
  href: "/inventory/valuation",
  icon: Coins,
  permission: INVENTORY_STOCK_PERMISSIONS.ViewCost,
};
