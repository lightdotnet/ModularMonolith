"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { Search } from "lucide-react";
import { cn } from "@/lib/shared/utils";
import { CommandPalette, type CommandGroup } from "@/components/command";
import { NAV_ITEMS } from "@/constants/nav-items";
import { buildVisibleMenu, flattenNavLeaves } from "@/lib/shared/menu";
import { hasPermission } from "@/lib/shared/authorization";

export function SearchBox({
  permissions,
  userName,
  className,
}: {
  permissions: string[];
  userName: string | null;
  className?: string;
}) {
  const router = useRouter();
  const [open, setOpen] = useState(false);

  const groups = useMemo<CommandGroup[]>(() => {
    const can = (permission: string) => hasPermission(permissions, userName, permission);
    const menu = buildVisibleMenu(NAV_ITEMS, can);
    const leaves = flattenNavLeaves(menu);

    const bySection = new Map<string, CommandGroup>();
    for (const { item, section } of leaves) {
      let group = bySection.get(section);
      if (!group) {
        group = { heading: section, items: [] };
        bySection.set(section, group);
      }
      group.items.push({
        id: item.href,
        label: item.label,
        description: section === item.label ? item.href : `${section} · ${item.href}`,
        icon: item.icon,
        keywords: [section, item.href],
        onSelect: () => router.push(item.href),
      });
    }
    return [...bySection.values()];
  }, [permissions, userName, router]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key?.toLowerCase() === "k" && (event.metaKey || event.ctrlKey)) {
        event.preventDefault();
        setOpen((prev) => !prev);
      }
    };
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, []);

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className={cn(
          "relative hidden h-9 w-full max-w-sm items-center rounded-md border border-input bg-background pr-12 pl-9 text-left text-sm text-muted-foreground outline-none transition-colors duration-fast ease-standard hover:bg-accent/40 focus-visible:border-ring focus-visible:ring-2 focus-visible:ring-ring/50 sm:flex",
          className,
        )}
      >
        <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
        <span>Search...</span>
        <kbd className="pointer-events-none absolute top-1/2 right-2.5 -translate-y-1/2 rounded-sm border border-border bg-muted px-1.5 py-0.5 text-[0.7rem] text-muted-foreground">
          ⌘K
        </kbd>
      </button>

      <CommandPalette
        open={open}
        onOpenChange={setOpen}
        groups={groups}
        placeholder="Search pages..."
        emptyMessage="No pages found."
      />
    </>
  );
}
