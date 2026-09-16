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
import { searchProductsAction } from "@/modules/catalog/api/search-products-action";
import type { ProductDto } from "@/modules/catalog/types/product";

const SEARCH_DEBOUNCE_MS = 300;

function optionLabel(product: ProductDto) {
  return `${product.name} (${product.sku})`;
}

interface ProductSelectProps {
  value: string;
  onValueChange: (product: ProductDto) => void;
  placeholder?: string;
}

/**
 * On-demand active-product picker for adding an order line — never preloads the
 * full product list, debounces a search against `product` (`status: Active`).
 * Feature-owned copy of the async-search pattern in
 * `modules/organization/employees/components/user-select.tsx` (which itself
 * documents why this stays a small per-feature component rather than a shared
 * `components/ui/*` primitive) — kept local to Orders rather than shared with
 * Catalog since the two pickers aren't otherwise related.
 */
export function ProductSelect({ value, onValueChange, placeholder = "Select a product" }: ProductSelectProps) {
  const [open, setOpen] = React.useState(false);
  const [query, setQuery] = React.useState("");
  const [options, setOptions] = React.useState<ProductDto[]>([]);
  const [loading, setLoading] = React.useState(false);
  const [selectedLabel, setSelectedLabel] = React.useState<string | null>(null);

  const trimmedQuery = query.trim();

  React.useEffect(() => {
    let cancelled = false;
    const timeout = setTimeout(() => {
      (async () => {
        setLoading(true);
        const result = await searchProductsAction(trimmedQuery);
        if (cancelled) return;
        setOptions(result.data?.records ?? []);
        setLoading(false);
      })();
    }, SEARCH_DEBOUNCE_MS);

    return () => {
      cancelled = true;
      clearTimeout(timeout);
    };
  }, [trimmedQuery]);

  function handleSelect(product: ProductDto) {
    setSelectedLabel(optionLabel(product));
    onValueChange(product);
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
          <CommandInput placeholder="Search products..." value={query} onValueChange={setQuery} />
          <CommandList>
            {loading ? (
              <div className="flex items-center justify-center gap-2 py-6 text-sm text-muted-foreground">
                <Loader2Icon className="size-4 animate-spin" />
                Searching...
              </div>
            ) : (
              <>
                <CommandEmpty>No products found.</CommandEmpty>
                <CommandGroup>
                  {options.map((product) => (
                    <CommandItem key={product.id} value={product.id} onSelect={() => handleSelect(product)}>
                      <CheckIcon className={cn(product.id === value ? "opacity-100" : "opacity-0")} />
                      <span className="truncate">{optionLabel(product)}</span>
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
