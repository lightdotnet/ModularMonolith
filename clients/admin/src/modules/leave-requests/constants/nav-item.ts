import { CalendarDays } from "lucide-react";
import type { NavItem } from "@/types/nav";

/** No `permission` gate: every authenticated user can view/create/edit/delete their own leave
 * requests, so — like the Approvals "Requests" entry — this only requires a session. */
export const LEAVE_REQUESTS_NAV_ITEM: NavItem = {
  label: "Leave requests",
  href: "/leave-requests",
  icon: CalendarDays,
};
