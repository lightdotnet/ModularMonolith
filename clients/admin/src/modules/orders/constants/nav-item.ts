import { ShoppingCart } from "lucide-react";
import { ORDERS_PERMISSIONS } from "./permissions";
import type { NavItem } from "@/types/nav";

export const ORDERS_NAV_ITEM: NavItem = {
  label: "Orders",
  href: "/orders",
  icon: ShoppingCart,
  permission: ORDERS_PERMISSIONS.View,
};
