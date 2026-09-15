"use client";

import { useState } from "react";
import { ImageOff } from "lucide-react";

/** Real preview of a product image URL — falls back to a placeholder glyph when there's no URL
 * at all, or the URL 404s/is invalid, instead of the browser's default broken-image icon. */
export function ProductImageThumbnail({ url }: { url: string | null | undefined }) {
  const [broken, setBroken] = useState(false);

  if (!url || broken) {
    return (
      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded border border-border bg-muted text-muted-foreground">
        <ImageOff className="h-4 w-4" />
      </div>
    );
  }

  return (
    // eslint-disable-next-line @next/next/no-img-element -- arbitrary external URLs, not a local/optimizable asset
    <img
      src={url}
      alt=""
      className="h-10 w-10 shrink-0 rounded border border-border object-cover"
      onError={() => setBroken(true)}
    />
  );
}
