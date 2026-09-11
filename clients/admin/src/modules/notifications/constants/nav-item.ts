import { Bell } from "lucide-react";
import { NOTIFICATIONS_PERMISSIONS } from "./permissions";
import type { NavItem } from "@/types/nav";

export const NOTIFICATIONS_NAV_ITEM: NavItem = {
  label: "Notifications",
  href: "/notifications",
  icon: Bell,
  permission: NOTIFICATIONS_PERMISSIONS.Read,
};
