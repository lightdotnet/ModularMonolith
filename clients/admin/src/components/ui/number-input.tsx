"use client";

import * as React from "react";
import { Input } from "@/components/ui/input";

export interface NumberInputProps
  extends Omit<React.ComponentProps<"input">, "type" | "value" | "onChange"> {
  /** Raw numeric string (e.g. "1200.5") — formatting here is display-only and never mutates this value. */
  value: string;
  onValueChange: (value: string) => void;
}

/**
 * Number input that stays grouped-by-thousands (#0,000 / #0,000.5x) while typing, not just on
 * blur — the integer part is regrouped live on every keystroke while the fraction part (after
 * the decimal point) is left exactly as typed, so a trailing "0" (e.g. finishing "10.50") never
 * gets silently dropped by round-tripping through a parsed `Number`. Backed by a text input
 * (`inputMode="decimal"`) since a native `type="number"` can't render grouping separators at
 * all. Cursor position is preserved across reformatting by counting digits (not raw characters)
 * to its left before the edit and restoring the same digit-count position afterward, since
 * commas are the only characters ever inserted/removed by formatting.
 */
export function NumberInput({ value, onValueChange, className, ...props }: NumberInputProps) {
  const inputRef = React.useRef<HTMLInputElement>(null);
  const pendingDigitCursorRef = React.useRef<number | null>(null);

  const displayValue = formatForDisplay(value);

  React.useLayoutEffect(() => {
    const digitCount = pendingDigitCursorRef.current;
    pendingDigitCursorRef.current = null;
    if (digitCount == null || !inputRef.current) return;
    const pos = cursorPositionForDigitCount(displayValue, digitCount);
    inputRef.current.setSelectionRange(pos, pos);
  }, [displayValue]);

  function handleChange(event: React.ChangeEvent<HTMLInputElement>) {
    const input = event.target;
    const cursor = input.selectionStart ?? input.value.length;
    const digitsBeforeCursor = countDigits(input.value.slice(0, cursor));

    const raw = input.value.replace(/,/g, "");
    if (raw !== "" && raw !== "-" && !/^-?\d*\.?\d{0,2}$/.test(raw)) return;

    pendingDigitCursorRef.current = digitsBeforeCursor;
    onValueChange(raw);
  }

  return (
    <Input
      {...props}
      ref={inputRef}
      type="text"
      inputMode="decimal"
      className={className}
      value={displayValue}
      onChange={handleChange}
    />
  );
}

function countDigits(str: string): number {
  return (str.match(/\d/g) ?? []).length;
}

function cursorPositionForDigitCount(str: string, digitCount: number): number {
  if (digitCount <= 0) return 0;
  let seen = 0;
  for (let i = 0; i < str.length; i++) {
    if (/\d/.test(str[i])) {
      seen++;
      if (seen === digitCount) return i + 1;
    }
  }
  return str.length;
}

/** Groups only the integer part by thousands; the fraction part (if any) passes through verbatim. */
function formatForDisplay(raw: string): string {
  if (raw === "" || raw === "-") return raw;

  const negative = raw.startsWith("-");
  const unsigned = negative ? raw.slice(1) : raw;
  const dotIndex = unsigned.indexOf(".");
  const integerPart = dotIndex === -1 ? unsigned : unsigned.slice(0, dotIndex);
  const fractionPart = dotIndex === -1 ? null : unsigned.slice(dotIndex + 1);

  const groupedInteger = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, ",");

  return (negative ? "-" : "") + groupedInteger + (fractionPart === null ? "" : `.${fractionPart}`);
}
