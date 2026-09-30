"use client";

import * as React from "react";
import { ChevronDownIcon } from "lucide-react";

import { cn } from "@/lib/shared/utils";
import { Label } from "@/components/ui/label";
import { Spinner } from "@/components/ui/spinner";

export interface NativeSelectOption<TValue extends string = string> {
  value: TValue;
  label: string;
  disabled?: boolean;
}

export interface NativeSelectProps<TValue extends string = string>
  extends Omit<
    React.ComponentProps<"select">,
    "value" | "defaultValue" | "onChange" | "children"
  > {
  options: NativeSelectOption<TValue>[];
  value?: TValue;
  defaultValue?: TValue;
  onChange?: (value: TValue) => void;
  placeholder?: string;
  label?: React.ReactNode;
  helperText?: React.ReactNode;
  error?: string;
  loading?: boolean;
}

/** Empty string is the native `<select>` sentinel for "nothing selected" — the DOM value must be a string, so this stands in for the placeholder option. */
const PLACEHOLDER_VALUE = "";

export function NativeSelect<TValue extends string = string>({
  id,
  options,
  value,
  defaultValue,
  onChange,
  placeholder,
  label,
  helperText,
  error,
  loading = false,
  disabled,
  required,
  className,
  ...props
}: NativeSelectProps<TValue>) {
  const generatedId = React.useId();
  const selectId = id ?? generatedId;
  const messageId = `${selectId}-message`;

  const [internalValue, setInternalValue] = React.useState<TValue>(
    defaultValue ?? (PLACEHOLDER_VALUE as TValue),
  );
  const isControlled = value !== undefined;
  const currentValue = isControlled ? value : internalValue;
  const message = error ?? helperText;
  const isPlaceholderShown = Boolean(placeholder) && currentValue === PLACEHOLDER_VALUE;

  function handleChange(event: React.ChangeEvent<HTMLSelectElement>) {
    const next = event.target.value as TValue;
    if (!isControlled) setInternalValue(next);
    onChange?.(next);
  }

  return (
    <div className="flex flex-col gap-1.5">
      {label && <Label htmlFor={selectId}>{label}</Label>}

      <div className="relative">
        <select
          id={selectId}
          value={currentValue}
          onChange={handleChange}
          disabled={disabled || loading}
          required={required}
          aria-invalid={Boolean(error) || undefined}
          aria-describedby={message ? messageId : undefined}
          aria-busy={loading || undefined}
          className={cn(
            "h-8 w-full min-w-0 appearance-none rounded-md border border-input bg-transparent px-2.5 py-1 pr-8 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-2 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:bg-input/50 disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-2 aria-invalid:ring-destructive/20 md:text-sm dark:bg-input/30 dark:disabled:bg-input/80 dark:aria-invalid:border-destructive/50 dark:aria-invalid:ring-destructive/40",
            isPlaceholderShown && "text-muted-foreground",
            className,
          )}
          {...props}
        >
          {placeholder && (
            // Deliberately not `disabled hidden`: this acts as a real,
            // always-reselectable "no value" choice (e.g. resetting a
            // filter back to "All statuses"), not a one-way required-field
            // placeholder that disappears once a real option is picked.
            <option value={PLACEHOLDER_VALUE}>{placeholder}</option>
          )}
          {options.map((option) => (
            <option key={option.value} value={option.value} disabled={option.disabled}>
              {option.label}
            </option>
          ))}
        </select>

        <span className="pointer-events-none absolute top-1/2 right-2.5 -translate-y-1/2 text-muted-foreground">
          {loading ? <Spinner className="size-4" /> : <ChevronDownIcon className="size-4" />}
        </span>
      </div>

      {message && (
        <p
          id={messageId}
          className={cn("text-xs", error ? "text-destructive" : "text-muted-foreground")}
        >
          {message}
        </p>
      )}
    </div>
  );
}
