import { Network } from "lucide-react";
import { ORG_UNITS_PERMISSIONS } from "./permissions";
import type { NavItem } from "@/types/nav";

export const DEPARTMENTS_NAV_ITEM: NavItem = {
  label: "Departments & Teams",
  href: "/organization/departments",
  icon: Network,
  permission: ORG_UNITS_PERMISSIONS.View,
};
