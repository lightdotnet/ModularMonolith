"use client";

import * as React from "react";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/shared/utils";

export interface NumberInputProps
  extends Omit<React.ComponentProps<"input">, "type" | "value" | "onChange"> {
  /** Raw numeric string (e.g. "1200.5") — formatting here is display-only and never mutates this value. */
  value: string;
  onValueChange: (value: string) => void;
  /**
   * Grows the input's box width to fit exactly what's typed instead of staying at a
   * fixed/full-container width. Measures the actual rendered pixel width of the text via a
   * hidden mirror element rather than estimating from character count (`ch` units assume every
   * glyph is as wide as "0", which most fonts don't honor — the error compounds with length).
   * Opt-in so existing fixed-width `NumberInput` usages (fee/discount/line amounts) are
   * unaffected.
   */
  autoWidth?: boolean;
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
export function NumberInput({
  value,
  onValueChange,
  className,
  autoWidth,
  style,
  ...props
}: NumberInputProps) {
  const inputRef = React.useRef<HTMLInputElement>(null);
  const measureRef = React.useRef<HTMLSpanElement>(null);
  const pendingDigitCursorRef = React.useRef<number | null>(null);
  const [autoWidthPx, setAutoWidthPx] = React.useState(0);

  const displayValue = formatForDisplay(value);
  // Empty: size to the placeholder (e.g. the line's current unit price) instead of collapsing
  // to nothing — otherwise an unset value hides what it's a stand-in for.
  const measureText = displayValue || (typeof props.placeholder === "string" ? props.placeholder : "");

  React.useLayoutEffect(() => {
    const digitCount = pendingDigitCursorRef.current;
    pendingDigitCursorRef.current = null;
    if (digitCount == null || !inputRef.current) return;
    const pos = cursorPositionForDigitCount(displayValue, digitCount);
    inputRef.current.setSelectionRange(pos, pos);
  }, [displayValue]);

  // Measures the actual rendered pixel width instead of estimating from character count — the
  // mirror span below carries the same font/size classes as the input, so `scrollWidth` is
  // exactly what the text needs. Padding/border are read off the input itself (Tailwind's global
  // `box-sizing: border-box` means the final `width` must include them on top of that text width,
  // not just the text width alone) so this stays exact for whichever className each `autoWidth`
  // usage passes (`px-2.5` vs `px-1`, etc.) without hardcoding either.
  React.useLayoutEffect(() => {
    if (!autoWidth || !inputRef.current || !measureRef.current) return;
    const computed = getComputedStyle(inputRef.current);
    const overhead =
      parseFloat(computed.paddingLeft) +
      parseFloat(computed.paddingRight) +
      parseFloat(computed.borderLeftWidth) +
      parseFloat(computed.borderRightWidth);
    setAutoWidthPx(Math.ceil(measureRef.current.scrollWidth) + Math.ceil(overhead));
  }, [autoWidth, measureText, className]);

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
    <>
      {autoWidth && (
        // Invisible mirror used only to measure text width — deliberately mirrors just `Input`'s
        // font-affecting base classes (`text-base md:text-sm`), NOT the caller's `className`:
        // that can carry padding/border utilities (e.g. `px-1`), which would inflate this span's
        // own `scrollWidth` and get double-counted on top of the padding/border already added
        // via `overhead` below.
        <span
          ref={measureRef}
          aria-hidden
          className="pointer-events-none absolute -z-10 whitespace-pre text-base tabular-nums md:text-sm"
          style={{ visibility: "hidden", left: 0, top: 0 }}
        >
          {measureText || "0"}
        </span>
      )}
      <Input
        {...props}
        ref={inputRef}
        type="text"
        inputMode="decimal"
        className={cn(autoWidth && "w-auto min-w-0 tabular-nums", className)}
        style={autoWidth ? { width: `${autoWidthPx}px`, ...style } : style}
        value={displayValue}
        onChange={handleChange}
      />
    </>
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
