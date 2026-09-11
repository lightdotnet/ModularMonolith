"use client";

import { Fragment } from "react";
import { usePathname } from "next/navigation";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { NAV_ITEMS } from "@/constants/nav-items";
import type { NavItem } from "@/types/nav";

function flattenLabels(items: NavItem[], map = new Map<string, string>()) {
  for (const item of items) {
    map.set(item.href, item.label);
    if (item.children) flattenLabels(item.children, map);
  }
  return map;
}

const LABELS = flattenLabels(NAV_ITEMS);

function toTitleCase(segment: string) {
  return segment
    .replace(/-/g, " ")
    .replace(/\b\w/g, (char) => char.toUpperCase());
}

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Route params like `[id]` land here as raw identifiers with no nav label — a UUID, a long
 * hex string, or a numeric id. Title-casing those produces noise, so show a generic label. */
function isOpaqueId(segment: string) {
  return (
    UUID_PATTERN.test(segment) ||
    /^[0-9a-f]{16,}$/i.test(segment) ||
    /^\d+$/.test(segment)
  );
}

function labelForSegment(segment: string) {
  return isOpaqueId(segment) ? "Details" : toTitleCase(segment);
}

export function Breadcrumbs() {
  const pathname = usePathname();
  const segments = pathname.split("/").filter(Boolean);

  const crumbs = [
    { href: "/", label: LABELS.get("/") ?? "Dashboard" },
    ...segments.map((segment, index) => {
      const href = "/" + segments.slice(0, index + 1).join("/");
      return { href, label: LABELS.get(href) ?? labelForSegment(segment) };
    }),
  ];

  return (
    <Breadcrumb>
      <BreadcrumbList className="flex-nowrap">
        {crumbs.map((crumb, index) => (
          <Fragment key={crumb.href}>
            {index > 0 && <BreadcrumbSeparator />}
            <BreadcrumbItem>
              {index === crumbs.length - 1 ? (
                <BreadcrumbPage>{crumb.label}</BreadcrumbPage>
              ) : (
                <BreadcrumbLink href={crumb.href}>{crumb.label}</BreadcrumbLink>
              )}
            </BreadcrumbItem>
          </Fragment>
        ))}
      </BreadcrumbList>
    </Breadcrumb>
  );
}
