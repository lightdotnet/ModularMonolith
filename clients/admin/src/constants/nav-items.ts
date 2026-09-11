import { Building2, Settings, ShieldCheck, ShieldCog } from "lucide-react";
// Imported directly (not via each feature's barrel): the barrels also re-export
// server-only code (Server Components, session-backed API calls), and this file
// is reachable from the client-side Sidebar — pulling in the full barrel would
// drag that server-only chain into the client bundle.
import { HOME_NAV_ITEM } from "@/features/home/constants/nav-item";
import { USERS_NAV_ITEM } from "@/modules/identity/users/constants/nav-item";
import { ROLES_NAV_ITEM } from "@/modules/identity/roles/constants/nav-item";
import { NOTIFICATIONS_NAV_ITEM } from "@/modules/notifications/constants/nav-item";
import { COMPANIES_NAV_ITEM } from "@/modules/organization/companies/constants/nav-item";
import { DEPARTMENTS_NAV_ITEM } from "@/modules/organization/departments/constants/nav-item";
import { EMPLOYEES_NAV_ITEM } from "@/modules/organization/employees/constants/nav-item";
import { APPROVALS_NAV_ITEM } from "@/modules/approvals/constants/nav-item";
import { LEAVE_REQUESTS_NAV_ITEM } from "@/modules/leave-requests/constants/nav-item";
import type { NavItem } from "@/types/nav";

export const NAV_ITEMS: NavItem[] = [
  HOME_NAV_ITEM,
  {
    label: "Administration",
    href: "/administration",
    icon: ShieldCog,
    children: [
      USERS_NAV_ITEM,
      ROLES_NAV_ITEM,
      NOTIFICATIONS_NAV_ITEM,
    ],
  },
  {
    label: "Organization",
    href: "/organization",
    icon: Building2,
    children: [
      COMPANIES_NAV_ITEM,
      DEPARTMENTS_NAV_ITEM,
      EMPLOYEES_NAV_ITEM,
    ],
  },
  APPROVALS_NAV_ITEM,
  LEAVE_REQUESTS_NAV_ITEM,
  {
    label: "Settings",
    href: "/settings",
    icon: Settings,
  },
];
