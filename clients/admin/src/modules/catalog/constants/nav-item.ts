import { Package } from "lucide-react";
import { PRODUCTS_PERMISSIONS } from "./permissions";
import type { NavItem } from "@/types/nav";

export const CATALOG_NAV_ITEM: NavItem = {
  label: "Catalog",
  href: "/catalog",
  icon: Package,
  permission: PRODUCTS_PERMISSIONS.View,
};
