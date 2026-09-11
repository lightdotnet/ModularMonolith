"use client";

import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import { LogOut, Settings, User } from "lucide-react";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { getDisplayName, getInitials } from "@/lib/shared/user-display";
import { logoutAction } from "@/modules/identity/auth/api/logout-action";
import type { ProfileData } from "@/types/session";

export function UserMenu({ user }: { user: ProfileData | null }) {
  const name = user ? getDisplayName(user) : "Unknown user";
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const currentPath = `${pathname}${searchParams.toString() ? `?${searchParams.toString()}` : ""}`;

  return (
    <DropdownMenu modal={false}>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          className="h-auto gap-2.5 rounded-lg border border-border bg-background px-5 py-1 text-foreground hover:bg-background/80"
        >
          <Avatar>
            <AvatarFallback>{user ? getInitials(user) : "?"}</AvatarFallback>
          </Avatar>
          <span className="hidden flex-col items-start gap-0 leading-none sm:flex">
            <span className="text-sm font-semibold">{name}</span>
            <span className="-mt-0.5 text-xs text-muted-foreground">
              {user ? `@${user.userName}` : "—"}
            </span>
          </span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel className="flex flex-col gap-0.5 font-normal">
          <span className="text-sm font-medium text-foreground">{name}</span>
          <span className="text-xs text-muted-foreground">
            {user?.email ?? "—"}
          </span>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem asChild>
          <Link href="/user-profile">
            <User />
            Profile
          </Link>
        </DropdownMenuItem>
        <DropdownMenuItem>
          <Settings />
          Settings
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem variant="destructive" onClick={() => void logoutAction(currentPath)}>
          <LogOut />
          Log out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
