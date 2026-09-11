import { Building2 } from "lucide-react";
import { COMPANIES_PERMISSIONS } from "./permissions";
import type { NavItem } from "@/types/nav";

export const COMPANIES_NAV_ITEM: NavItem = {
  label: "Companies",
  href: "/organization/companies",
  icon: Building2,
  permission: COMPANIES_PERMISSIONS.View,
};
