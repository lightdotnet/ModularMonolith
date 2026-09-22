import { ArrowRightLeft } from "lucide-react";
import { TRANSFERS_PERMISSIONS } from "./permissions";
import type { NavItem } from "@/types/nav";

export const TRANSFERS_NAV_ITEM: NavItem = {
  label: "Transfers",
  href: "/transfers",
  icon: ArrowRightLeft,
  permission: TRANSFERS_PERMISSIONS.View,
};
