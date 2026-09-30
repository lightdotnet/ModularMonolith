"use client";

import * as React from "react";
import { CheckIcon, ChevronsUpDownIcon, XIcon } from "lucide-react";

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

export interface ComboboxOption<TValue extends string = string> {
  value: TValue;
  label: string;
  description?: string;
  disabled?: boolean;
}

export interface ComboboxProps<TValue extends string = string> {
  options: ComboboxOption<TValue>[];
  value: TValue | null;
  onValueChange: (value: TValue) => void;
  placeholder?: string;
  /** Shown in the popover when no option matches the typed search. */
  emptyMessage?: string;
  disabled?: boolean;
  id?: string;
  /** Renders a hidden input so this select can submit inside a native `<form>`. */
  name?: string;
  "aria-label"?: string;
  className?: string;
  /** Shows a clear (X) button in place of the chevron once a value is selected. Omit for required fields. */
  onClear?: () => void;
}

/**
 * shadcn's standard Popover + Command combobox, generic over a static
 * options list. Replaces the previous EntitySelect/SearchSelect pair — both
 * were "pick one from an already-fetched list" selects that only differed in
 * whether the trigger doubled as a text input; the Command list's built-in
 * filtering covers that difference on its own.
 */
export function Combobox<TValue extends string = string>({
  options,
  value,
  onValueChange,
  placeholder = "Select...",
  emptyMessage = "No options found.",
  disabled,
  id,
  name,
  "aria-label": ariaLabel,
  className,
  onClear,
}: ComboboxProps<TValue>) {
  const [open, setOpen] = React.useState(false);

  const selectedOption = options.find((option) => option.value === value);
  const showClear = !!onClear && !!selectedOption;

  function handleSelect(nextValue: TValue) {
    onValueChange(nextValue);
    setOpen(false);
  }

  return (
    <>
      {name && <input type="hidden" name={name} value={value ?? ""} />}
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
              disabled={disabled}
              data-placeholder={!selectedOption || undefined}
              className={cn(
                "w-full justify-between font-normal data-placeholder:text-muted-foreground",
                className,
              )}
            >
              <span className="min-w-0 flex-1 truncate text-left">
                {selectedOption ? selectedOption.label : placeholder}
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
            <Command>
              <CommandInput placeholder={placeholder} />
              <CommandList>
                <CommandEmpty>{emptyMessage}</CommandEmpty>
                <CommandGroup>
                  {options.map((option) => (
                    <CommandItem
                      key={option.value}
                      value={option.label}
                      disabled={option.disabled}
                      onSelect={() => handleSelect(option.value)}
                    >
                      <CheckIcon
                        className={cn(
                          option.value === value ? "opacity-100" : "opacity-0",
                        )}
                      />
                      <div className="flex min-w-0 flex-col">
                        <span className="truncate">{option.label}</span>
                        {option.description && (
                          <span className="truncate text-xs text-muted-foreground">
                            {option.description}
                          </span>
                        )}
                      </div>
                    </CommandItem>
                  ))}
                </CommandGroup>
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
