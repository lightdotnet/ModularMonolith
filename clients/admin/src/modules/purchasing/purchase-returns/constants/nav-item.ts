import { Undo2 } from "lucide-react";
import { PURCHASING_PERMISSIONS } from "@/modules/purchasing/common";
import type { NavItem } from "@/types/nav";

export const PURCHASE_RETURNS_NAV_ITEM: NavItem = {
  label: "Purchase returns",
  href: "/purchasing/returns",
  icon: Undo2,
  permission: PURCHASING_PERMISSIONS.Returns.View,
};
