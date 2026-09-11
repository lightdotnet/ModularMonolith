"use client";

import * as React from "react";
import { CheckIcon, ChevronsUpDownIcon, Loader2Icon } from "lucide-react";
import { cn } from "@/lib/shared/utils";
import { Button } from "@/components/ui/button";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { searchUsersAction } from "@/modules/identity/users/api/search-users-action";
import { getDisplayName } from "@/lib/shared/user-display";
import type { UserDto } from "@/modules/identity/users";

const PAGE_SIZE = 10;
const SEARCH_DEBOUNCE_MS = 300;
const MIN_SEARCH_LENGTH = 3;

function optionLabel(user: UserDto) {
  return `${getDisplayName(user)} (@${user.userName})`;
}

interface UserSelectProps {
  value: string;
  onValueChange: (user: UserDto) => void;
  placeholder?: string;
}

/**
 * On-demand Identity user picker for linking an existing login account to an
 * employee. Same pattern as `features/notifications/components/user-select.tsx`
 * (never preloads the full user list, debounces a search against `user/search`)
 * kept as a small feature-owned copy rather than a cross-feature import — the
 * two components aren't otherwise related, and `components/shared/*` may not
 * import from `features/*` (see admin architecture.md's Dependency Direction).
 */
export function UserSelect({ value, onValueChange, placeholder = "Select a user" }: UserSelectProps) {
  const [open, setOpen] = React.useState(false);
  const [query, setQuery] = React.useState("");
  const [options, setOptions] = React.useState<UserDto[]>([]);
  const [loading, setLoading] = React.useState(false);
  const [selectedLabel, setSelectedLabel] = React.useState<string | null>(null);

  const trimmedQuery = query.trim();
  const belowThreshold = trimmedQuery.length < MIN_SEARCH_LENGTH;

  React.useEffect(() => {
    if (belowThreshold) return;

    let cancelled = false;
    const timeout = setTimeout(() => {
      (async () => {
        setLoading(true);
        const result = await searchUsersAction({
          searchValue: trimmedQuery,
          pageNumber: 1,
          pageSize: PAGE_SIZE,
        });
        if (cancelled) return;
        setOptions(result.data?.records ?? []);
        setLoading(false);
      })();
    }, SEARCH_DEBOUNCE_MS);

    return () => {
      cancelled = true;
      clearTimeout(timeout);
    };
  }, [belowThreshold, trimmedQuery]);

  function handleSelect(user: UserDto) {
    setSelectedLabel(optionLabel(user));
    onValueChange(user);
    setOpen(false);
  }

  const triggerLabel = value ? (selectedLabel ?? value) : placeholder;

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="outline"
          role="combobox"
          aria-expanded={open}
          data-placeholder={!value || undefined}
          className="w-full justify-between font-normal data-placeholder:text-muted-foreground"
        >
          <span className="line-clamp-1">{triggerLabel}</span>
          <ChevronsUpDownIcon className="size-4 shrink-0 text-muted-foreground" />
        </Button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-(--radix-popover-trigger-width) p-0">
        <Command shouldFilter={false}>
          <CommandInput
            placeholder="Type at least 3 characters to search..."
            value={query}
            onValueChange={setQuery}
          />
          <CommandList>
            {belowThreshold ? (
              <p className="py-4 text-center text-sm text-muted-foreground">
                Type at least 3 characters to search.
              </p>
            ) : loading ? (
              <div className="flex items-center justify-center gap-2 py-6 text-sm text-muted-foreground">
                <Loader2Icon className="size-4 animate-spin" />
                Searching...
              </div>
            ) : (
              <>
                <CommandEmpty>No users found.</CommandEmpty>
                <CommandGroup>
                  {options.map((user) => (
                    <CommandItem key={user.id} value={user.id} onSelect={() => handleSelect(user)}>
                      <CheckIcon className={cn(user.id === value ? "opacity-100" : "opacity-0")} />
                      <span className="truncate">{optionLabel(user)}</span>
                    </CommandItem>
                  ))}
                </CommandGroup>
              </>
            )}
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}
