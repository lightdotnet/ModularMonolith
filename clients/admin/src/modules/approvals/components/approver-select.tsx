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
import { searchEmployeesAction } from "@/modules/organization/employees/api/search-employees-action";
import type { EmployeeDto } from "@/modules/organization/employees";

const PAGE_SIZE = 10;
const SEARCH_DEBOUNCE_MS = 300;
const MIN_SEARCH_LENGTH = 3;

function optionLabel(employee: EmployeeDto) {
  const name = `${employee.firstName ?? ""} ${employee.lastName ?? ""}`.trim();
  return name ? `${name} (${employee.employeeCode})` : employee.employeeCode;
}

interface ApproverSelectProps {
  value: string;
  onValueChange: (employee: EmployeeDto) => void;
  placeholder?: string;
  ariaLabel?: string;
}

/**
 * On-demand Organization employee picker for one level of an approval chain —
 * restricted to employees that have a linked Identity login (`linkedToUserOnly`),
 * so the parent gets both a real `employee.id` and the backing `employee.userId`.
 * Same debounced-search shape as the other on-demand pickers in this codebase,
 * kept as its own small copy per that convention rather than a cross-feature
 * component import.
 */
export function ApproverSelect({
  value,
  onValueChange,
  placeholder = "Select an approver",
  ariaLabel,
}: ApproverSelectProps) {
  const [open, setOpen] = React.useState(false);
  const [query, setQuery] = React.useState("");
  const [options, setOptions] = React.useState<EmployeeDto[]>([]);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [selectedLabel, setSelectedLabel] = React.useState<string | null>(null);

  const trimmedQuery = query.trim();
  const belowThreshold = trimmedQuery.length < MIN_SEARCH_LENGTH;

  React.useEffect(() => {
    if (belowThreshold) return;

    let cancelled = false;
    const timeout = setTimeout(() => {
      (async () => {
        setLoading(true);
        setError(null);
        const result = await searchEmployeesAction({
          searchValue: trimmedQuery,
          linkedToUserOnly: true,
          pageNumber: 1,
          pageSize: PAGE_SIZE,
        });
        if (cancelled) return;
        setOptions(result.data?.records ?? []);
        setError(result.data ? null : (result.error ?? "Could not load employees."));
        setLoading(false);
      })();
    }, SEARCH_DEBOUNCE_MS);

    return () => {
      cancelled = true;
      clearTimeout(timeout);
    };
  }, [belowThreshold, trimmedQuery]);

  function handleSelect(employee: EmployeeDto) {
    setSelectedLabel(optionLabel(employee));
    onValueChange(employee);
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
          aria-label={ariaLabel}
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
            ) : error ? (
              <p className="py-4 text-center text-sm text-destructive">{error}</p>
            ) : (
              <>
                <CommandEmpty>No employees with a login account match.</CommandEmpty>
                <CommandGroup>
                  {options.map((employee) => (
                    <CommandItem
                      key={employee.id}
                      value={employee.id}
                      onSelect={() => handleSelect(employee)}
                    >
                      <CheckIcon className={cn(employee.id === value ? "opacity-100" : "opacity-0")} />
                      <span className="truncate">{optionLabel(employee)}</span>
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
