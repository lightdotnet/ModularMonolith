"use client";

import { Check, Palette } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  ACCENT_COLORS,
  useAccentColor,
  type AccentColor,
} from "./accent-color-provider";

// Every preset maps to a Tailwind `-600` shade except "black", which has no
// shade scale — it's the flat `--color-black` token instead.
function swatchColor(value: AccentColor) {
  return value === "black" ? "var(--color-black)" : `var(--color-${value}-600)`;
}

export function AccentColorPicker() {
  const { accentColor, setAccentColor } = useAccentColor();

  return (
    <DropdownMenu modal={false}>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" aria-label="Change accent color">
          <Palette />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuLabel>Accent color</DropdownMenuLabel>
        <DropdownMenuSeparator />
        {ACCENT_COLORS.map(({ value, label }) => (
          <DropdownMenuItem key={value} onSelect={() => setAccentColor(value)}>
            <span
              className="size-3.5 shrink-0 rounded-full"
              style={{ backgroundColor: swatchColor(value) }}
              aria-hidden="true"
            />
            {label}
            {accentColor === value && <Check className="ml-auto size-4" />}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
