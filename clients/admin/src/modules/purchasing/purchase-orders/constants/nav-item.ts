import { ClipboardList } from "lucide-react";
import { PURCHASING_PERMISSIONS } from "@/modules/purchasing/common";
import type { NavItem } from "@/types/nav";

export const PURCHASE_ORDERS_NAV_ITEM: NavItem = {
  label: "Purchase orders",
  href: "/purchasing/orders",
  icon: ClipboardList,
  permission: PURCHASING_PERMISSIONS.Orders.View,
};
