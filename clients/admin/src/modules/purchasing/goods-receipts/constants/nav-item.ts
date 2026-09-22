import { PackageCheck } from "lucide-react";
import { PURCHASING_PERMISSIONS } from "@/modules/purchasing/common";
import type { NavItem } from "@/types/nav";

export const GOODS_RECEIPTS_NAV_ITEM: NavItem = {
  label: "Goods receipts",
  href: "/purchasing/receipts",
  icon: PackageCheck,
  permission: PURCHASING_PERMISSIONS.Receipts.View,
};
