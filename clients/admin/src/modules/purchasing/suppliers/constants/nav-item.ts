import { Truck } from "lucide-react";
import { PURCHASING_PERMISSIONS } from "@/modules/purchasing/common";
import type { NavItem } from "@/types/nav";

export const SUPPLIERS_NAV_ITEM: NavItem = {
  label: "Suppliers",
  href: "/purchasing/suppliers",
  icon: Truck,
  permission: PURCHASING_PERMISSIONS.Suppliers.View,
};
