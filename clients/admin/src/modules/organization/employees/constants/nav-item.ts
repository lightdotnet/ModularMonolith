import { Contact } from "lucide-react";
import { EMPLOYEES_PERMISSIONS } from "./permissions";
import type { NavItem } from "@/types/nav";

export const EMPLOYEES_NAV_ITEM: NavItem = {
  label: "Employees",
  href: "/organization/employees",
  icon: Contact,
  permission: EMPLOYEES_PERMISSIONS.View,
};
