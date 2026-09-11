"use client";

import * as React from "react";
import { CheckIcon, ChevronsUpDownIcon, Loader2Icon, XIcon } from "lucide-react";
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
// Search only fires once the query has at least 3 characters.
const MIN_SEARCH_LENGTH = 3;

function optionLabel(user: UserDto) {
  return `${getDisplayName(user)} (@${user.userName})`;
}

interface UserSelectProps {
  id?: string;
  name?: string;
  value: string;
  /** Hands back the full user record (not just the id) so callers that also need the display name — e.g. to fill a "fromName" field — don't have to look it up separately. */
  onValueChange: (user: UserDto) => void;
  placeholder?: string;
  triggerClassName?: string;
  ariaLabel?: string;
  /** Shows a clear (X) button once a user is selected. Omit for required fields. */
  onClear?: () => void;
}

/**
 * Recipient/sender picker for the notifications feature. Loads no options
 * until the admin types at least 3 characters, then debounces a search
 * against the first page of matches — never preloads the workspace's users.
 */
export function UserSelect({
  id,
  name,
  value,
  onValueChange,
  placeholder = "Select a user",
  triggerClassName,
  ariaLabel,
  onClear,
}: UserSelectProps) {
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

  const showClear = !!onClear && !!value;
  const triggerLabel = value ? (selectedLabel ?? value) : placeholder;

  return (
    <>
      {name && <input type="hidden" name={name} value={value} />}
      <div className="relative">
        <Popover open={open} onOpenChange={setOpen}>
          <PopoverTrigger asChild>
            <Button
              id={id}
              type="button"
              variant="outline"
              role="combobox"
              aria-expanded={open}
              aria-label={ariaLabel}
              data-placeholder={!value || undefined}
              className={cn(
                "w-full justify-between font-normal data-placeholder:text-muted-foreground",
                triggerClassName,
              )}
            >
              <span className="line-clamp-1 flex items-center gap-1.5">
                {triggerLabel}
              </span>
              <ChevronsUpDownIcon
                className={cn(
                  "size-4 shrink-0 text-muted-foreground",
                  showClear && "invisible",
                )}
              />
            </Button>
          </PopoverTrigger>
          <PopoverContent
            align="start"
            className="w-(--radix-popover-trigger-width) p-0"
          >
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
                        <CommandItem
                          key={user.id}
                          value={user.id}
                          onSelect={() => handleSelect(user)}
                        >
                          <CheckIcon
                            className={cn(
                              user.id === value ? "opacity-100" : "opacity-0",
                            )}
                          />
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

        {showClear && (
          <button
            type="button"
            aria-label="Clear selection"
            onClick={(event) => {
              event.stopPropagation();
              setOpen(false);
              setSelectedLabel(null);
              onClear?.();
            }}
            className="absolute top-1/2 right-2 flex size-4 -translate-y-1/2 items-center justify-center rounded-sm text-muted-foreground hover:text-foreground"
          >
            <XIcon className="size-3.5" />
          </button>
        )}
      </div>
    </>
  );
}
