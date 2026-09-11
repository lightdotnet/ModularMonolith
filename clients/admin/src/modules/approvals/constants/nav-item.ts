import { CheckCheck } from "lucide-react";
import { APPROVAL_DOCUMENT_TYPES_PERMISSIONS } from "./document-type-permissions";
import type { NavItem } from "@/types/nav";

/** The parent and its "Requests" child carry no `permission` gate: every authenticated
 * user can have approvals routed to them, so — like the page itself — they only require
 * a session. "Document types" is admin-only and gated on its view permission. */
export const APPROVALS_NAV_ITEM: NavItem = {
  label: "Approvals",
  href: "/approvals",
  icon: CheckCheck,
  children: [
    {
      label: "Requests",
      href: "/approvals/requests" },
    {
      label: "Document types",
      href: "/approvals/document-types",
      permission: APPROVAL_DOCUMENT_TYPES_PERMISSIONS.View,
    },
  ],
};
