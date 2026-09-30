"use client";

import { useEffect, useRef } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/shared/utils";
import { useSidebar } from "@/hooks/use-sidebar";
import type { NavItem } from "@/types/nav";

function isActivePath(pathname: string, href: string, exact?: boolean): boolean {
  if (href === "/" || exact) return pathname === href;
  return pathname === href || pathname.startsWith(`${href}/`);
}

function containsActive(item: NavItem, pathname: string): boolean {
  if (isActivePath(pathname, item.href, item.exact)) return true;
  return (
    item.children?.some((child) => containsActive(child, pathname)) ?? false
  );
}

export function SidebarNavItem({ item }: { item: NavItem }) {
  const pathname = usePathname();
  const { isExpanded, toggleExpanded } = useSidebar();
  const active = isActivePath(pathname, item.href, item.exact);
  const hasChildren = !!item.children?.length;
  const branchActive = containsActive(item, pathname);
  // No explicit user override yet -> default to auto-expanding the branch
  // that contains the active route. Once the user toggles it, that choice
  // wins even while a child route stays active (otherwise the group could
  // never be collapsed while its own page is open).
  const expanded = hasChildren && (isExpanded(item.href) ?? branchActive);
  const Icon = item.icon;

  const linkRef = useRef<HTMLAnchorElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (active) {
      (linkRef.current ?? buttonRef.current)?.scrollIntoView({
        block: "nearest",
      });
    }
  }, [active]);

  if (hasChildren) {
    return (
      <div>
        <button
          ref={buttonRef}
          type="button"
          onClick={() => toggleExpanded(item.href, expanded)}
          aria-expanded={expanded}
          className={cn(
            "flex w-full cursor-pointer items-center gap-2.5 rounded-md px-2.5 py-2 text-sm font-medium text-sidebar-foreground/80 transition-colors duration-fast ease-standard hover:bg-sidebar-accent hover:text-sidebar-accent-foreground",
            branchActive && "bg-sidebar-primary text-sidebar-primary-foreground",
          )}
        >
          {Icon && <Icon className="size-4 shrink-0" />}
          <span className="flex-1 truncate text-left">{item.label}</span>
          <ChevronDown
            className={cn(
              "size-4 shrink-0 transition-transform duration-fast ease-standard",
              expanded && "rotate-180",
            )}
          />
        </button>
        {expanded && (
          <div className="mt-0.5 ml-4 flex flex-col gap-0.5 border-l border-sidebar-border pl-2.5">
            {item.children!.map((child) => (
              <SidebarNavItem key={child.href} item={child} />
            ))}
          </div>
        )}
      </div>
    );
  }

  return (
    <Link
      ref={linkRef}
      href={item.href}
      aria-current={active ? "page" : undefined}
      className={cn(
        "flex items-center gap-2.5 rounded-md px-2.5 py-2 text-sm font-medium transition-colors duration-fast ease-standard",
        active
          ? "bg-sidebar-primary text-sidebar-primary-foreground"
          : "text-sidebar-foreground/80 hover:bg-sidebar-accent hover:text-sidebar-accent-foreground",
      )}
    >
      {Icon && <Icon className="size-4 shrink-0" />}
      <span className="truncate">{item.label}</span>
    </Link>
  );
}
