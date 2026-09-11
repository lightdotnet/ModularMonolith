"use client";

import { useId } from "react";
import { InlineScript } from "@/components/shared/inline-script";

interface LocalDateTimeProps {
  /** An ISO string, epoch millis, or Date. Nullish renders nothing. */
  value: string | number | Date | null | undefined;
  className?: string;
}

/**
 * Renders a timestamp in the viewer's locale and time zone without a hydration mismatch.
 *
 * `toLocaleString()` with no arguments resolves against the runtime environment, so the server
 * (UTC) and the browser disagree. This follows the Next.js-recommended fix: the server emits its
 * best guess, an inline script rewrites it to the viewer's local value before hydration, and
 * `suppressHydrationWarning` tells React to keep the DOM. On soft navigations the Client Component
 * simply renders `toLocaleString()` in the browser and the (inert) script is ignored.
 *
 * See node_modules/next/dist/docs/01-app/02-guides/preventing-flash-before-hydration.md.
 */
export function LocalDateTime({ value, className }: LocalDateTimeProps) {
  const id = useId();

  if (value === null || value === undefined || value === "") {
    return null;
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return null;
  }

  const iso = date.toISOString();

  return (
    <>
      <time id={id} dateTime={iso} className={className} suppressHydrationWarning>
        {date.toLocaleString()}
      </time>
      <InlineScript
        html={`{var n=document.getElementById(${JSON.stringify(id)});if(n)n.textContent=new Date(${JSON.stringify(iso)}).toLocaleString()}`}
      />
    </>
  );
}
