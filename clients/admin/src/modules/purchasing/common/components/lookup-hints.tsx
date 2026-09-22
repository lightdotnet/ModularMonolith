import { Alert, AlertDescription } from "@/components/ui/alert";

/**
 * Inline hints for pickers/filters whose lookup is empty because the viewer lacks access, the call failed,
 * or more records exist than were loaded — so an empty control is never silently unexplained.
 */
export function LookupHints({ hints }: { hints: (string | false | null | undefined)[] }) {
  const items = hints.filter((hint): hint is string => !!hint);
  if (items.length === 0) return null;

  return (
    <Alert>
      <AlertDescription>
        <ul className="flex flex-col gap-1">
          {items.map((hint) => (
            <li key={hint}>{hint}</li>
          ))}
        </ul>
      </AlertDescription>
    </Alert>
  );
}

export const SUPPLIER_ACCESS_HINT =
  "You need Suppliers access (purchasing.suppliers.view) to pick or filter by supplier.";
export const SUPPLIER_FAILED_HINT = "Suppliers could not be loaded, so the supplier picker is empty. Refresh to try again.";
export const LOCATION_HINT =
  "Locations could not be loaded (you may need Location access, location.locations.view), so the location picker is empty.";

export function supplierTruncatedHint(limit: number): string {
  return `Only the first ${limit} suppliers are listed in the supplier picker. Use the Suppliers page to find others.`;
}
